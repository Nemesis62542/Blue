using System;
using UnityEngine;

namespace Blue.World
{
    /// <summary>
    /// タイル1枚分のベイク結果。
    /// </summary>
    [Serializable]
    public class StageTileEntry
    {
        public int tileIndex;
        public int tileX;
        public int tileZ;

        [Tooltip("加算ロードするタイルシーンのパス")]
        public string scenePath;

        [Tooltip("このタイルの Terrain が占める範囲。ストリーミングの距離判定に使う")]
        public Bounds bounds;

        [Tooltip("実際にベイクされた高さの範囲(m)。ミニマップの色域決定などに使う")]
        public float minBakedHeight;
        public float maxBakedHeight;
    }

    /// <summary>
    /// ステージのベイク結果一覧。ストリーミングローダーはこれだけを見ればよく、
    /// シーンやアセットを走査する必要がない。
    ///
    /// これは生成物なので、手で編集しない（再ベイクで上書きされる）。
    /// </summary>
    public class StageTileManifest : ScriptableObject
    {
        [SerializeField] private string stageId;
        [SerializeField] private StageTileLayout layout;
        [SerializeField] private StageTileEntry[] tiles = Array.Empty<StageTileEntry>();
        [SerializeField] private string bakedAtUtc;

        public string StageId => stageId;
        public StageTileLayout Layout => layout;
        public StageTileEntry[] Tiles => tiles;
        public string BakedAtUtc => bakedAtUtc;

        public StageTileEntry Find(int tileX, int tileZ)
        {
            if (!layout.IsValidTile(tileX, tileZ))
            {
                return null;
            }

            int index = layout.TileIndex(tileX, tileZ);
            foreach (StageTileEntry tile in tiles)
            {
                if (tile.tileIndex == index)
                {
                    return tile;
                }
            }

            return null;
        }

        /// <summary>ベイカーから設定する。ランタイムからは呼ばない。</summary>
        public void SetContents(string stageId, StageTileLayout layout, StageTileEntry[] tiles)
        {
            this.stageId = stageId;
            this.layout = layout;
            this.tiles = tiles ?? Array.Empty<StageTileEntry>();
            this.bakedAtUtc = DateTime.UtcNow.ToString("o");
        }
    }
}
