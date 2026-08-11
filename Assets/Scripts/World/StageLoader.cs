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
    /// 1タイルのロードにかかった実測値。ヒッチの原因追跡に使う。
    ///
    /// Unity Profiler だけでは「どのタイルのせいでスパイクしたか」が分からない。
    /// シーン統合のコストは AsyncOperation の完了フレームにエンジン内部で発生するため、
    /// こちらのコードで Profiler.BeginSample を掛けても囲めないため。
    /// completedFrame をキャプチャのフレーム番号と突き合わせて特定する。
    /// </summary>
    public struct StageTileLoadRecord
    {
        public int tileIndex;
        public int requestedFrame;
        public int completedFrame;
        public float queuedMs;
        public float loadMs;
        public float activateMs;
    }

    /// <summary>
    /// マニフェストを元に、対象(プレイヤー)の周囲のタイルシーンを加算ロード/アンロードする。
    ///
    /// 水中はフォグで視界が短いため、2km ステージでも常時ロードは数枚で足りる。
    ///
    /// 【シーンのアクティベーションを直列化している理由】
    /// 複数タイルが同一フレームで統合されるとヒッチが重なる。allowSceneActivation を
    /// 落として順番待ちさせ、常に1枚ずつ統合させることで山を平す。
    /// </summary>
    public class StageLoader : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Source")]
        [SerializeField] private StageTileManifest manifest;

        [Tooltip("この Transform の周囲のタイルをロードする。未設定なら MainCamera を探す")]
        [SerializeField] private Transform target;

        [Header("Radius")]
        [Tooltip("この距離以内のタイルをロードする(m)")]
        [SerializeField] private float loadRadius = 400f;

        [Tooltip("ロード半径にこれを足した距離を超えたらアンロードする(m)。境界での往復を防ぐ")]
        [SerializeField] private float unloadPadding = 160f;

        [Header("Budget")]
        [Tooltip("同時に走らせるロード数。1にすると最も滑らかだが追従が遅れる")]
        [SerializeField] private int maxConcurrentLoads = 2;

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
        private bool activationInFlight;
        private bool hasPendingUnloadCleanup;
        private bool warnedMissingTarget;

        private sealed class PendingLoad
        {
            public int tileIndex;
            public AsyncOperation operation;
            public int requestedFrame;
            public float requestedTime;
            public float readyTime;
            public float activationStartTime;
            public bool activationStarted;
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
            if (manifest == null || target == null)
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
            Vector3 position = target.position;
            float unloadRadius = UnloadRadius;

            loadCandidates.Clear();

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

            if (loadCandidates.Count == 0)
            {
                return;
            }

            // 近いタイルから埋める。遠くのタイルのロードで足元が空くのを防ぐ
            loadCandidates.Sort((a, b) =>
                DistanceToTileXZ(a.bounds, position).CompareTo(DistanceToTileXZ(b.bounds, position)));

            foreach (StageTileEntry entry in loadCandidates)
            {
                if (pendingLoads.Count >= maxConcurrentLoads)
                {
                    break;
                }

                BeginLoad(entry);
            }
        }

        /// <summary>
        /// タイルの XZ 矩形までの距離。タイルは高さ方向にステージ全体を覆うので Y は見ない。
        /// </summary>
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

            // 統合は1枚ずつ行う。順番が来るまで待たせる
            operation.allowSceneActivation = false;

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
        /// 進行中のロードを進める。アクティベーションは常に1枚だけ許可する。
        /// </summary>
        private void PumpLoads()
        {
            for (int i = pendingLoads.Count - 1; i >= 0; i--)
            {
                PendingLoad pending = pendingLoads[i];

                // allowSceneActivation = false のとき progress は 0.9 で頭打ちになる
                bool isReady = pending.operation.progress >= 0.9f;

                if (isReady && pending.readyTime <= 0f)
                {
                    pending.readyTime = Time.realtimeSinceStartup;
                }

                if (!pending.activationStarted)
                {
                    if (isReady && !activationInFlight)
                    {
                        pending.activationStarted = true;
                        pending.activationStartTime = Time.realtimeSinceStartup;
                        pending.operation.allowSceneActivation = true;
                        activationInFlight = true;
                    }

                    continue;
                }

                if (!pending.operation.isDone)
                {
                    continue;
                }

                states[pending.tileIndex] = StageTileState.Loaded;
                pendingLoads.RemoveAt(i);
                activationInFlight = false;

                if (recordDiagnostics)
                {
                    float now = Time.realtimeSinceStartup;
                    records.Add(new StageTileLoadRecord
                    {
                        tileIndex = pending.tileIndex,
                        requestedFrame = pending.requestedFrame,
                        completedFrame = Time.frameCount,
                        queuedMs = (pending.activationStartTime - pending.readyTime) * 1000f,
                        loadMs = (pending.readyTime - pending.requestedTime) * 1000f,
                        activateMs = (now - pending.activationStartTime) * 1000f,
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
        /// 計測結果を CSV 文字列にする。profiler キャプチャのフレーム番号と突き合わせて使う。
        /// </summary>
        public string BuildReportCsv()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("tileIndex,requestedFrame,completedFrame,loadMs,queuedMs,activateMs");

            foreach (StageTileLoadRecord record in records)
            {
                builder.AppendLine(
                    $"{record.tileIndex},{record.requestedFrame},{record.completedFrame}," +
                    $"{record.loadMs:F2},{record.queuedMs:F2},{record.activateMs:F2}");
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
