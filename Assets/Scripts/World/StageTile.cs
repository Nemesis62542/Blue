using UnityEngine;

namespace Blue.World
{
    /// <summary>
    /// タイルシーン上の Terrain に付く識別子。
    /// ストリーミングローダーがロード済みタイルを把握し、隣接接続やアンロード判定を行うために使う。
    /// </summary>
    [RequireComponent(typeof(Terrain))]
    [DisallowMultipleComponent]
    public class StageTile : MonoBehaviour
    {
        [SerializeField] private string stageId;
        [SerializeField] private int tileX;
        [SerializeField] private int tileZ;
        [SerializeField] private int tileIndex;

        private Terrain cachedTerrain;

        public string StageId => stageId;
        public int TileX => tileX;
        public int TileZ => tileZ;
        public int TileIndex => tileIndex;

        public Terrain Terrain
        {
            get
            {
                if (cachedTerrain == null)
                {
                    cachedTerrain = GetComponent<Terrain>();
                }

                return cachedTerrain;
            }
        }

        /// <summary>ベイカーから設定する。ランタイムからは呼ばない。</summary>
        public void Setup(string stageId, int tileX, int tileZ, int tileIndex)
        {
            this.stageId = stageId;
            this.tileX = tileX;
            this.tileZ = tileZ;
            this.tileIndex = tileIndex;
        }
    }
}
