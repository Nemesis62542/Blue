using System;
using UnityEngine;

namespace Blue.World
{
    /// <summary>
    /// ステージのタイル分割レイアウト。ワールド座標とタイル座標の変換を行う。
    /// </summary>
    // ベイク時（エディタ）とランタイム（ストリーミング）で必ずこの構造体を経由すること。
    // 双方で座標計算を書くとタイル境界がずれ、散布物の所有タイルが食い違う。
    [Serializable]
    public struct StageTileLayout
    {
        #region Serialized Fields

        [Tooltip("ステージ全体の一辺の長さ(m)")]
        [SerializeField] private float worldSize;

        [Tooltip("一辺あたりのタイル数。タイル総数はこの二乗になる")]
        [SerializeField] private int tilesPerAxis;

        [Tooltip("タイル1枚のハイトマップ解像度。2^n+1 である必要がある")]
        [SerializeField] private int heightmapResolution;

        [Tooltip("タイル1枚のアルファマップ(スプラット)解像度")]
        [SerializeField] private int alphamapResolution;

        [Tooltip("最深部のY座標(m)。水中前提なので通常は負の値")]
        [SerializeField] private float minHeight;

        [Tooltip("最浅部のY座標(m)")]
        [SerializeField] private float maxHeight;

        #endregion

        #region Properties

        public float WorldSize => worldSize;
        public int TilesPerAxis => tilesPerAxis;
        public int HeightmapResolution => heightmapResolution;
        public int AlphamapResolution => alphamapResolution;
        public float MinHeight => minHeight;
        public float MaxHeight => maxHeight;

        /// <summary>タイル1枚の一辺(m)</summary>
        public float TileSize => worldSize / tilesPerAxis;

        /// <summary>高さ方向のレンジ(m)。TerrainData.size.y に入る値</summary>
        public float HeightRange => maxHeight - minHeight;

        /// <summary>タイル総数</summary>
        public int TileCount => tilesPerAxis * tilesPerAxis;

        /// <summary>ステージ全体で見たときのハイトマップサンプル数（一辺）</summary>
        // 隣接タイルは境界の1列を共有するので、単純な res * tiles にはならない。
        public int GlobalHeightSamples => tilesPerAxis * (heightmapResolution - 1) + 1;

        /// <summary>
        /// ステージ左下(-X,-Z)の隅のワールド座標。ステージは原点中心に配置する。
        /// </summary>
        public Vector3 Origin => new Vector3(-worldSize * 0.5f, minHeight, -worldSize * 0.5f);

        /// <summary>TerrainData.size に設定する値</summary>
        public Vector3 TerrainSize => new Vector3(TileSize, HeightRange, TileSize);

        #endregion

        #region Constructors

        public StageTileLayout(float worldSize, int tilesPerAxis, int heightmapResolution,
                               int alphamapResolution, float minHeight, float maxHeight)
        {
            this.worldSize = worldSize;
            this.tilesPerAxis = tilesPerAxis;
            this.heightmapResolution = heightmapResolution;
            this.alphamapResolution = alphamapResolution;
            this.minHeight = minHeight;
            this.maxHeight = maxHeight;
        }

        /// <summary>2km / 8x8タイル(256m) / 1m per texel の既定構成</summary>
        // 水中はフォグで視界が短いため、タイルは小さめにしてストリーミング粒度を稼ぐ。
        public static StageTileLayout Default =>
            new StageTileLayout(2048f, 8, 257, 512, -300f, 20f);

        #endregion

        #region Tile Index

        /// <summary>タイル座標を一次元インデックスに変換する</summary>
        public int TileIndex(int tileX, int tileZ) => tileZ * tilesPerAxis + tileX;

        public int TileIndexToX(int index) => index % tilesPerAxis;

        public int TileIndexToZ(int index) => index / tilesPerAxis;

        public bool IsValidTile(int tileX, int tileZ) =>
            tileX >= 0 && tileX < tilesPerAxis && tileZ >= 0 && tileZ < tilesPerAxis;

        #endregion

        #region Coordinate Conversion

        /// <summary>タイルの原点（Terrain の transform.position に入る値）</summary>
        // Y は最深部なので、Terrain のローカル高さ 0 が minHeight に対応する。
        public Vector3 TileOrigin(int tileX, int tileZ)
        {
            Vector3 origin = Origin;
            return new Vector3(origin.x + tileX * TileSize, origin.y, origin.z + tileZ * TileSize);
        }

        public Bounds TileBounds(int tileX, int tileZ)
        {
            Vector3 size = TerrainSize;
            Vector3 min = TileOrigin(tileX, tileZ);
            return new Bounds(min + size * 0.5f, size);
        }

        /// <summary>
        /// ワールド座標が属するタイル座標を返す。範囲外の場合は端のタイルにクランプされる。
        /// </summary>
        // 散布物の「所有タイル」判定はこれで行う（インスタンスの基点が属するタイルが所有する）。
        public void WorldToTile(Vector3 worldPosition, out int tileX, out int tileZ)
        {
            Vector3 origin = Origin;
            float tileSize = TileSize;
            tileX = Mathf.Clamp(Mathf.FloorToInt((worldPosition.x - origin.x) / tileSize), 0, tilesPerAxis - 1);
            tileZ = Mathf.Clamp(Mathf.FloorToInt((worldPosition.z - origin.z) / tileSize), 0, tilesPerAxis - 1);
        }

        /// <summary>ワールド座標をステージ全体の正規化UV(0-1)に変換する</summary>
        public Vector2 WorldToStageUv(Vector3 worldPosition)
        {
            Vector3 origin = Origin;
            return new Vector2(
                (worldPosition.x - origin.x) / worldSize,
                (worldPosition.z - origin.z) / worldSize);
        }

        #endregion

        #region Validation

        /// <summary>設定が Unity の Terrain の制約を満たしているか検証する。</summary>
        public bool Validate(out string error)
        {
            if (worldSize <= 0f)
            {
                error = "worldSize は正の値である必要があります。";
                return false;
            }

            if (tilesPerAxis < 1)
            {
                error = "tilesPerAxis は1以上である必要があります。";
                return false;
            }

            if (!IsPowerOfTwoPlusOne(heightmapResolution))
            {
                error = $"heightmapResolution ({heightmapResolution}) は 2^n+1 である必要があります（33, 65, 129, 257, 513, 1025 など）。";
                return false;
            }

            if (!Mathf.IsPowerOfTwo(alphamapResolution) || alphamapResolution < 16)
            {
                error = $"alphamapResolution ({alphamapResolution}) は16以上の2の冪である必要があります。";
                return false;
            }

            if (HeightRange <= 0f)
            {
                error = $"maxHeight ({maxHeight}) は minHeight ({minHeight}) より大きい必要があります。";
                return false;
            }

            error = null;
            return true;
        }

        private static bool IsPowerOfTwoPlusOne(int value)
        {
            return value >= 33 && Mathf.IsPowerOfTwo(value - 1);
        }

        #endregion
    }
}
