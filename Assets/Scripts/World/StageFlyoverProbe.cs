using System.IO;
using UnityEngine;

namespace Blue.World
{
    /// <summary>
    /// ステージを一定速度で直線横断させる計測用ハーネス。
    ///
    /// Unity Profiler は直近2000フレームしか保持しないため、通常の遊泳速度で 2km を
    /// 横断すると録画範囲に収まらない。高速で横断させて1キャプチャに収める。
    /// 高速移動はストリーミングの最悪ケースでもあるので、テストとしても妥当。
    ///
    /// 使い方:
    ///   1. このコンポーネントを空の GameObject に付け、StageLoader の target に指定する
    ///   2. Play して横断させる（Profiler は Record 状態にしておく）
    ///   3. 終了時に ProfilerCaptures/ へ CSV が出るので、
    ///      profiler-capture-dumper の出力とフレーム番号で突き合わせる
    /// </summary>
    public class StageFlyoverProbe : MonoBehaviour
    {
        #region Serialized Fields

        [SerializeField] private StageLoader loader;

        [Header("Path")]
        [Tooltip("横断速度(m/s)。2048m を 60fps/2000フレーム(約33秒)に収めるには 62 以上が必要")]
        [SerializeField] private float speed = 80f;

        [Tooltip("横断する高さ(m)")]
        [SerializeField] private float altitude = -60f;

        [Tooltip("ステージ端から内側に寄せる距離(m)")]
        [SerializeField] private float margin = 64f;

        [Tooltip("対角線で横断する。false なら X 軸に平行に横断する")]
        [SerializeField] private bool diagonal = true;

        [Header("Control")]
        [SerializeField] private bool runOnStart = true;

        [Tooltip("到達時に CSV を書き出す")]
        [SerializeField] private bool writeReportOnFinish = true;

        #endregion

        #region Fields

        private Vector3 startPosition;
        private Vector3 endPosition;
        private bool running;
        private float startTime;
        private int startFrame;

        #endregion

        #region Properties

        public bool IsRunning => running;

        #endregion

        #region Unity

        private void Start()
        {
            if (runOnStart)
            {
                Run();
            }
        }

        private void Update()
        {
            if (!running)
            {
                return;
            }

            Vector3 direction = endPosition - startPosition;
            float totalDistance = direction.magnitude;
            float travelled = (Time.time - startTime) * speed;

            if (travelled >= totalDistance)
            {
                transform.position = endPosition;
                Finish();
                return;
            }

            transform.position = startPosition + direction.normalized * travelled;
        }

        #endregion

        #region Control

        /// <summary>
        /// 横断を開始する。
        /// </summary>
        public void Run()
        {
            if (loader == null || loader.Manifest == null)
            {
                Debug.LogError("[StageFlyoverProbe] StageLoader またはそのマニフェストが未設定です。", this);
                return;
            }

            StageTileLayout layout = loader.Manifest.Layout;
            float half = layout.WorldSize * 0.5f - margin;

            startPosition = new Vector3(-half, altitude, diagonal ? -half : 0f);
            endPosition = new Vector3(half, altitude, diagonal ? half : 0f);

            transform.position = startPosition;
            startTime = Time.time;
            startFrame = Time.frameCount;
            running = true;

            loader.ClearRecords();

            float distance = Vector3.Distance(startPosition, endPosition);
            Debug.Log(
                $"[StageFlyoverProbe] 横断を開始します。\n" +
                $"  距離: {distance:F0}m / 速度: {speed:F0}m/s → 所要 約{distance / speed:F1}秒",
                this);
        }

        private void Finish()
        {
            running = false;

            float elapsed = Time.time - startTime;
            int frames = Time.frameCount - startFrame;

            Debug.Log(
                $"[StageFlyoverProbe] 横断が完了しました。\n" +
                $"  所要: {elapsed:F1}秒 / {frames} フレーム (平均 {frames / Mathf.Max(elapsed, 0.001f):F1} fps)\n" +
                $"  ロード記録: {loader.Records.Count} 件 / 現在ロード中のタイル: {loader.LoadedTileCount} 枚",
                this);

            if (frames > 2000)
            {
                Debug.LogWarning(
                    $"[StageFlyoverProbe] {frames} フレームかかっており、Profiler のリングバッファ上限(2000)を超えています。" +
                    "キャプチャには後半しか残りません。speed を上げるか区間を分けてください。",
                    this);
            }

            if (writeReportOnFinish)
            {
                WriteReport();
            }
        }

        /// <summary>
        /// ロード実測を ProfilerCaptures/ に CSV で書き出す。
        /// </summary>
        public void WriteReport()
        {
            string directory = Path.Combine(Application.dataPath, "..", "ProfilerCaptures");
            Directory.CreateDirectory(directory);

            string fileName = $"stage_flyover_{System.DateTime.Now:yyyyMMdd_HHmmss}.csv";
            string path = Path.Combine(directory, fileName);

            File.WriteAllText(path, loader.BuildReportCsv());
            Debug.Log($"[StageFlyoverProbe] ロード実測を書き出しました: {path}", this);
        }

        #endregion
    }
}
