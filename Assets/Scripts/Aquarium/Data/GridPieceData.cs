using UnityEngine;

namespace Blue.Aquarium
{
    /// <summary>
    /// グリッドのセルを占有して置く設置物の定義
    /// </summary>
    public abstract class GridPieceData : AquariumPieceData
    {
        [Header("グリッド")]
        [SerializeField] private Vector2Int footprint = Vector2Int.one; // 回転前の占有セル数(X,Z)
        [SerializeField] private bool walkable;                        // 上を歩けるか（通路は true）

        public Vector2Int Footprint => footprint;
        public bool Walkable => walkable;

        public override PiecePlacement Placement => PiecePlacement.Grid;
    }
}
