using System;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Blue.World.Loading
{
    /// <summary>
    /// ステージロードのフェーズを組み立てて実行する入口。
    /// </summary>
    // ロード画面 UI はこのコンポーネントの ProgressChanged を購読するだけでよい。
    // 現状は地形タイルのフェーズのみ。散布データ・スポーンフィールド・初期スポーンは
    // 実装され次第 BuildSequence に足していく。
    public class StageLoadRunner : MonoBehaviour
    {
        #region Serialized Fields

        [SerializeField] private StageLoader loader;

        [Header("Weights")]
        [Tooltip("地形タイルフェーズの重み。完了ログに出る実測秒数を見て調整する")]
        [SerializeField] private float tilePhaseWeight = 1f;

        [Header("Control")]
        [SerializeField] private bool runOnStart = true;

        [Tooltip("1フェーズがこの秒数を超えたら異常として中断する")]
        [SerializeField] private float phaseTimeoutSeconds = 120f;

        #endregion

        #region Fields

        private CancellationTokenSource cancellation;
        private StageLoadSequence sequence;

        #endregion

        #region Properties

        public StageLoadStatus Status { get; private set; }

        public bool IsLoading => sequence != null && sequence.IsRunning;

        /// <summary>進捗が更新されるたびに呼ばれる。UI はこれを購読する</summary>
        public event Action<StageLoadStatus> ProgressChanged;

        /// <summary>全フェーズ完了時に呼ばれる</summary>
        public event Action Completed;

        /// <summary>失敗時に呼ばれる（タイムアウトなど）</summary>
        public event Action<Exception> Failed;

        #endregion

        #region Unity

        private void Start()
        {
            if (runOnStart)
            {
                Load().Forget();
            }
        }

        private void OnDestroy()
        {
            cancellation?.Cancel();
            cancellation?.Dispose();
            cancellation = null;
        }

        #endregion

        #region Run

        /// <summary>
        /// ロードを開始する。
        /// </summary>
        public async UniTask Load()
        {
            if (IsLoading)
            {
                Debug.LogWarning("[StageLoadRunner] 既にロード中です。", this);
                return;
            }

            cancellation?.Cancel();
            cancellation?.Dispose();
            cancellation = new CancellationTokenSource();

            sequence = BuildSequence();

            Progress<StageLoadStatus> progress = new Progress<StageLoadStatus>(status =>
            {
                Status = status;
                ProgressChanged?.Invoke(status);
            });

            float startTime = Time.realtimeSinceStartup;

            try
            {
                await sequence.RunAsync(progress, cancellation.Token);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[StageLoadRunner] ロードに失敗しました: {exception.Message}", this);
                Failed?.Invoke(exception);
                return;
            }

            LogReports(Time.realtimeSinceStartup - startTime);
            Completed?.Invoke();
        }

        /// <summary>
        /// フェーズを組み立てる。
        /// </summary>
        // 工程が増えたらここに足す。
        private StageLoadSequence BuildSequence()
        {
            return new StageLoadSequence(phaseTimeoutSeconds)
                .Add(new StageTileLoadPhase(loader, tilePhaseWeight));

            // 今後追加する予定:
            //   .Add(new ScatterLoadPhase(...))
            //   .Add(new SpawnFieldLoadPhase(...))
            //   .Add(new InitialSpawnPhase(...))
        }

        /// <summary>
        /// フェーズごとの実測値をログに出す。
        /// </summary>
        // Weight を実測から決めるための材料。
        private void LogReports(float totalSeconds)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"[StageLoadRunner] ロード完了: {totalSeconds:F2} 秒");

            foreach (StageLoadPhaseReport report in sequence.Reports)
            {
                float share = totalSeconds > 0f ? report.seconds / totalSeconds : 0f;
                builder.AppendLine(
                    $"  {report.label,-24} {report.seconds,6:F2}秒 " +
                    $"(実測比 {share:P0} / 設定 weight {report.weight:F2}) " +
                    $"メモリ {report.reservedMemoryDelta / 1048576f,+8:F1} MB");
            }

            builder.AppendLine("  実測比と weight が乖離していると、進捗バーが途中で止まったように見えます。");
            builder.Append("  メモリ増分はエディタでは Profiler 自身の膨張に埋もれます。確定値は Development Build で。");
            Debug.Log(builder.ToString(), this);
        }

        #endregion
    }
}
