namespace Blue.World.Loading
{
    /// <summary>
    /// ロード全体の進捗。
    /// </summary>
    // ロード画面 UI へはこれだけを渡す。
    public struct StageLoadStatus
    {
        /// <summary>実行中のフェーズ名（UI 表示用）</summary>
        public string phaseLabel;

        /// <summary>実行中のフェーズ番号（0始まり）</summary>
        public int phaseIndex;

        /// <summary>フェーズ総数</summary>
        public int phaseCount;

        /// <summary>実行中フェーズの進捗 0-1</summary>
        public float phaseProgress;

        /// <summary>重み付けされた全体進捗 0-1</summary>
        public float totalProgress;
    }

    /// <summary>
    /// ステージロードの1工程。
    /// </summary>
    // 「地形タイル」「散布データ」「スポーンフィールド」「初期スポーン」などを
    // それぞれこの形で実装し、StageLoadSequence が合成して全体進捗を出す。
    public interface IStageLoadPhase
    {
        /// <summary>UI に出す工程名</summary>
        string Label { get; }

        /// <summary>全体進捗における重み</summary>
        // フェーズを均等配分するとバーが序盤で止まって終盤で飛ぶ。
        // 実測した所要時間を基準に決めること。
        float Weight { get; }

        /// <summary>このフェーズの進捗 0-1</summary>
        float Progress { get; }

        /// <summary>完了したか</summary>
        bool IsDone { get; }

        /// <summary>開始時に1度だけ呼ばれる</summary>
        void Begin();

        /// <summary>完了するまで毎フレーム呼ばれる</summary>
        void Tick();
    }
}
