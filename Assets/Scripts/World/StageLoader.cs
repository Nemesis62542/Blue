using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Blue.World
{
    /// <summary>
    /// タイルの読み込み状態。
    /// </summary>
    public enum StageTileState
    {
        Unloaded,
        Loading,
        Loaded,
        Unloading,
    }

    /// <summary>
    /// タイルの供給方式。
    /// </summary>
    public enum StageStreamingMode
    {
        /// <summary>プレイヤーとの距離でロード/アンロードする</summary>
        DistanceStreaming,

        /// <summary>ステージ遷移時に全タイルをロードし、以後アンロードしない</summary>
        // 地形は状態を持たないうえ、常駐できるだけのメモリがあるならゲーム中に
        // ロードを走らせる理由がない。全タイルが載っていれば Terrain の LOD 接続も
        // 常に成立し、ロード順による継ぎ目も起きない。
        PreloadAll,
    }

    /// <summary>
    /// 1タイルのロードにかかった実測値。
    /// </summary>
    // Unity Profiler だけでは「どのタイルのせいでスパイクしたか」が分からない。
    // シーン統合のコストは AsyncOperation の完了フレームにエンジン内部で発生し、
    // こちらのコードで Profiler.BeginSample を掛けても囲めないため。
    // completedFrame をキャプチャのフレーム番号と突き合わせて特定する。
    public struct StageTileLoadRecord
    {
        public int tileIndex;
        public int requestedFrame;
        public int completedFrame;

        /// <summary>要求から完了までの実時間(ms)。大半は非同期側なので、これ自体はヒッチ量ではない</summary>
        public float loadMs;

        /// <summary>要求から完了までにかかったフレーム数</summary>
        public int frameSpan;
    }

    /// <summary>
    /// マニフェストを元に、タイルシーンを加算ロード/アンロードする。
    /// </summary>
    // 水中はフォグで視界が短いため、2km ステージでも常時ロードは数枚で足りる。
    //
    // allowSceneActivation は使わない。当初はアクティベーションを1枚ずつに直列化して
    // ヒッチを平すつもりで allowSceneActivation = false による順番待ちを使っていたが、
    // これは機能しない。Unity の非同期オペレーションはキューで処理され、activation を
    // 保留したオペレーションは後続をブロックするため、2枚目以降が progress 0.9 にすら
    // 到達せずロードが永久に止まる（Hierarchy に "(now loading)" が残り続ける）。
    // 同時実行数を絞ることで直列化する方式に変更している。
    public class StageLoader : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Source")]
        [SerializeField] private StageTileManifest manifest;

        [Tooltip("DistanceStreaming のとき、この Transform の周囲のタイルをロードする。未設定なら MainCamera を探す")]
        [SerializeField] private Transform target;

        [Header("Mode")]
        [SerializeField] private StageStreamingMode mode = StageStreamingMode.DistanceStreaming;

        [Tooltip("PreloadAll のときの同時ロード数。ロード画面中なのでフレーム予算を気にしなくてよい")]
        [SerializeField] private int preloadMaxConcurrentLoads = 4;

        [Header("Radius")]
        [Tooltip("この距離以内のタイルをロードする(m)")]
        [SerializeField] private float loadRadius = 400f;

        [Tooltip("ロード半径にこれを足した距離を超えたらアンロードする(m)。境界での往復を防ぐ")]
        [SerializeField] private float unloadPadding = 160f;

        [Header("Budget")]
        [Tooltip("同時に走らせるロード数。1なら常に1枚ずつ統合されるのでヒッチが重ならない。\n" +
                 "増やすと追従は速くなるが、同一フレームで複数タイルが統合されて山が重なる")]
        [SerializeField] private int maxConcurrentLoads = 1;

        [Tooltip("評価の間隔(秒)。毎フレーム距離計算する必要はない")]
        [SerializeField] private float evaluateInterval = 0.25f;

        [Header("Debug")]
        [Tooltip("ロード/アンロードの実測を記録する。計測時のみ有効にする")]
        [SerializeField] private bool recordDiagnostics;

        [SerializeField] private bool drawGizmos;

        #endregion

        #region Fields

        private readonly Dictionary<int, StageTileState> states = new Dictionary<int, StageTileState>();
        private readonly List<PendingLoad> pendingLoads = new List<PendingLoad>();
        private readonly List<AsyncOperation> pendingUnloads = new List<AsyncOperation>();
        private readonly List<StageTileLoadRecord> records = new List<StageTileLoadRecord>();
        private readonly List<StageTileEntry> loadCandidates = new List<StageTileEntry>();

        private float nextEvaluateTime;
        private bool hasPendingUnloadCleanup;
        private bool warnedMissingTarget;

        private sealed class PendingLoad
        {
            public int tileIndex;
            public AsyncOperation operation;
            public int requestedFrame;
            public float requestedTime;
        }

        #endregion

        #region Properties

        public StageTileManifest Manifest => manifest;

        public IReadOnlyList<StageTileLoadRecord> Records => records;

        public int LoadedTileCount
        {
            get
            {
                int count = 0;
                foreach (KeyValuePair<int, StageTileState> pair in states)
                {
                    if (pair.Value == StageTileState.Loaded)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public int PendingLoadCount => pendingLoads.Count;

        public float UnloadRadius => loadRadius + unloadPadding;

        public StageStreamingMode Mode => mode;

        public int TotalTileCount => manifest != null ? manifest.Tiles.Length : 0;

        /// <summary>全タイルに対するロード済みの割合 0-1。ロード画面の進捗に使う</summary>
        public float LoadedFraction
        {
            get
            {
                int total = TotalTileCount;
                return total > 0 ? (float)LoadedTileCount / total : 1f;
            }
        }

        /// <summary>PreloadAll で全タイルが載りきったか</summary>
        public bool IsFullyLoaded => TotalTileCount > 0 && LoadedTileCount >= TotalTileCount;

        #endregion

        #region Unity

        private void Awake()
        {
            if (target == null && Camera.main != null)
            {
                target = Camera.main.transform;
            }
        }

        private void Update()
        {
            // PreloadAll は距離を見ないので target は不要
            bool needsTarget = mode == StageStreamingMode.DistanceStreaming;

            if (manifest == null || (needsTarget && target == null))
            {
                if (!warnedMissingTarget)
                {
                    warnedMissingTarget = true;
                    Debug.LogWarning(
                        manifest == null
                            ? "[StageLoader] manifest が未設定のため何もロードされません。"
                            : "[StageLoader] target が未設定で MainCamera も見つからないため何もロードされません。",
                        this);
                }

                return;
            }

            PumpLoads();
            PumpUnloads();

            if (Time.unscaledTime >= nextEvaluateTime)
            {
                nextEvaluateTime = Time.unscaledTime + evaluateInterval;
                Evaluate();
            }
        }

        #endregion

        #region Evaluation

        /// <summary>
        /// 対象との距離から、ロードすべきタイルとアンロードすべきタイルを決める。
        /// </summary>
        private void Evaluate()
        {
            loadCandidates.Clear();

            bool preload = mode == StageStreamingMode.PreloadAll;
            int budget = preload ? preloadMaxConcurrentLoads : maxConcurrentLoads;

            if (preload)
            {
                // 全タイルを積む。アンロードは行わない
                foreach (StageTileEntry entry in manifest.Tiles)
                {
                    if (GetState(entry.tileIndex) == StageTileState.Unloaded)
                    {
                        loadCandidates.Add(entry);
                    }
                }
            }
            else
            {
                Vector3 position = target.position;
                float unloadRadius = UnloadRadius;

                foreach (StageTileEntry entry in manifest.Tiles)
                {
                    float distance = DistanceToTileXZ(entry.bounds, position);
                    StageTileState state = GetState(entry.tileIndex);

                    if (distance <= loadRadius)
                    {
                        if (state == StageTileState.Unloaded)
                        {
                            loadCandidates.Add(entry);
                        }
                    }
                    else if (distance > unloadRadius && state == StageTileState.Loaded)
                    {
                        BeginUnload(entry);
                    }
                }
            }

            if (loadCandidates.Count == 0)
            {
                return;
            }

            // 近いタイルから埋める。遠くのタイルのロードで足元が空くのを防ぐ。
            // PreloadAll でも、プレイヤー位置が分かるならその周囲から埋めた方が
            // 途中でロード画面を抜けても破綻しにくい
            if (target != null)
            {
                Vector3 position = target.position;
                loadCandidates.Sort((a, b) =>
                    DistanceToTileXZ(a.bounds, position).CompareTo(DistanceToTileXZ(b.bounds, position)));
            }

            foreach (StageTileEntry entry in loadCandidates)
            {
                if (pendingLoads.Count >= budget)
                {
                    break;
                }

                BeginLoad(entry);
            }
        }

        /// <summary>
        /// タイルの XZ 矩形までの距離を返す。
        /// </summary>
        // タイルは高さ方向にステージ全体を覆うので Y は見ない。
        private static float DistanceToTileXZ(Bounds bounds, Vector3 position)
        {
            float dx = Mathf.Max(0f, Mathf.Max(bounds.min.x - position.x, position.x - bounds.max.x));
            float dz = Mathf.Max(0f, Mathf.Max(bounds.min.z - position.z, position.z - bounds.max.z));
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        #endregion

        #region Load

        private void BeginLoad(StageTileEntry entry)
        {
            AsyncOperation operation = SceneManager.LoadSceneAsync(entry.scenePath, LoadSceneMode.Additive);
            if (operation == null)
            {
                Debug.LogError(
                    $"[StageLoader] タイルシーンをロードできません: {entry.scenePath}\n" +
                    "Build Settings に登録されていない可能性があります（Blue > World > Register Stage Scenes）。",
                    this);
                states[entry.tileIndex] = StageTileState.Unloaded;
                return;
            }

            pendingLoads.Add(new PendingLoad
            {
                tileIndex = entry.tileIndex,
                operation = operation,
                requestedFrame = Time.frameCount,
                requestedTime = Time.realtimeSinceStartup,
            });

            states[entry.tileIndex] = StageTileState.Loading;
        }

        /// <summary>
        /// 進行中のロードの完了を拾う。
        /// </summary>
        private void PumpLoads()
        {
            for (int i = pendingLoads.Count - 1; i >= 0; i--)
            {
                PendingLoad pending = pendingLoads[i];

                if (!pending.operation.isDone)
                {
                    continue;
                }

                states[pending.tileIndex] = StageTileState.Loaded;
                pendingLoads.RemoveAt(i);

                // 枠が空いたらすぐ次を積む。評価間隔を待つと初期充填が
                // maxConcurrentLoads / evaluateInterval 枚/秒に律速されてしまう
                nextEvaluateTime = 0f;

                if (recordDiagnostics)
                {
                    records.Add(new StageTileLoadRecord
                    {
                        tileIndex = pending.tileIndex,
                        requestedFrame = pending.requestedFrame,
                        completedFrame = Time.frameCount,
                        loadMs = (Time.realtimeSinceStartup - pending.requestedTime) * 1000f,
                        frameSpan = Time.frameCount - pending.requestedFrame,
                    });
                }
            }
        }

        #endregion

        #region Unload

        private void BeginUnload(StageTileEntry entry)
        {
            Scene scene = SceneManager.GetSceneByPath(entry.scenePath);
            if (!scene.isLoaded)
            {
                states[entry.tileIndex] = StageTileState.Unloaded;
                return;
            }

            AsyncOperation operation = SceneManager.UnloadSceneAsync(scene);
            if (operation == null)
            {
                states[entry.tileIndex] = StageTileState.Unloaded;
                return;
            }

            states[entry.tileIndex] = StageTileState.Unloading;
            pendingUnloads.Add(operation);
            hasPendingUnloadCleanup = true;
        }

        private void PumpUnloads()
        {
            for (int i = pendingUnloads.Count - 1; i >= 0; i--)
            {
                if (!pendingUnloads[i].isDone)
                {
                    continue;
                }

                pendingUnloads.RemoveAt(i);
            }

            if (pendingUnloads.Count > 0 || !hasPendingUnloadCleanup)
            {
                return;
            }

            // アンロードが片付いた時点で Unloading のまま残っているものを整理する。
            // 毎フレーム全タイルを走査しないよう、実際にアンロードした後だけ実行する
            hasPendingUnloadCleanup = false;

            foreach (StageTileEntry entry in manifest.Tiles)
            {
                if (GetState(entry.tileIndex) == StageTileState.Unloading)
                {
                    states[entry.tileIndex] = StageTileState.Unloaded;
                }
            }
        }

        #endregion

        #region Public API

        public StageTileState GetState(int tileIndex)
        {
            return states.TryGetValue(tileIndex, out StageTileState state) ? state : StageTileState.Unloaded;
        }

        public bool IsTileLoaded(int tileIndex) => GetState(tileIndex) == StageTileState.Loaded;

        /// <summary>
        /// 供給方式を切り替える。
        /// </summary>
        // ロード画面で PreloadAll に、大きすぎるステージでは DistanceStreaming に、
        // といった使い分けを想定する。
        public void SetMode(StageStreamingMode value)
        {
            mode = value;
            nextEvaluateTime = 0f;
        }

        /// <summary>
        /// 計測結果を CSV 文字列にする。
        /// </summary>
        // profiler キャプチャのフレーム番号と突き合わせて使う。
        public string BuildReportCsv()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("tileIndex,requestedFrame,completedFrame,frameSpan,loadMs");

            foreach (StageTileLoadRecord record in records)
            {
                builder.AppendLine(
                    $"{record.tileIndex},{record.requestedFrame},{record.completedFrame}," +
                    $"{record.frameSpan},{record.loadMs:F2}");
            }

            return builder.ToString();
        }

        public void ClearRecords() => records.Clear();

        #endregion

        #region Gizmos

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!drawGizmos || manifest == null)
            {
                return;
            }

            Vector3 position = target != null ? target.position : transform.position;

            Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.6f);
            Gizmos.DrawWireSphere(position, loadRadius);
            Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.4f);
            Gizmos.DrawWireSphere(position, UnloadRadius);

            foreach (StageTileEntry entry in manifest.Tiles)
            {
                StageTileState state = GetState(entry.tileIndex);
                Gizmos.color = state switch
                {
                    StageTileState.Loaded => new Color(0.2f, 1f, 0.3f, 0.35f),
                    StageTileState.Loading => new Color(1f, 1f, 0.2f, 0.35f),
                    StageTileState.Unloading => new Color(1f, 0.3f, 0.2f, 0.35f),
                    _ => new Color(0.4f, 0.4f, 0.4f, 0.12f),
                };

                Gizmos.DrawWireCube(entry.bounds.center, entry.bounds.size);
            }
        }
#endif

        #endregion
    }
}
