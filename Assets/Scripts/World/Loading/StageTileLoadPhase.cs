using UnityEngine;

namespace Blue.World.Loading
{
    /// <summary>
    /// 地形タイルを全枚数ロードするフェーズ。
    /// </summary>
    // 実測 (2km / 8x8 / 60fps): タイル1枚あたり2フレームで完了する。
    // ロード画面中は同時ロード数を上げられるので、64枚でも数秒に収まる想定。
    public class StageTileLoadPhase : IStageLoadPhase
    {
        private readonly StageLoader loader;

        public string Label { get; }

        public float Weight { get; }

        public float Progress => loader != null ? loader.LoadedFraction : 1f;

        public bool IsDone => loader == null || loader.IsFullyLoaded;

        /// <param name="weight">全体進捗における重み</param>
        // 64枚という離散ステップがあり進捗の粒度が良いので、現状ではここを
        // 支配的にしておくのが素直。散布・スポーンの実測が出たら調整する。
        public StageTileLoadPhase(StageLoader loader, float weight = 1f, string label = "地形を読み込み中")
        {
            this.loader = loader;
            this.Weight = weight;
            this.Label = label;
        }

        public void Begin()
        {
            if (loader == null)
            {
                Debug.LogError("[StageTileLoadPhase] StageLoader が null です。地形はロードされません。");
                return;
            }

            loader.SetMode(StageStreamingMode.PreloadAll);
        }

        public void Tick()
        {
            // StageLoader が自身の Update で進行するため、ここですることはない
        }
    }
}
