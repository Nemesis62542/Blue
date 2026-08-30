using System.Collections.Generic;
using UnityEngine;

namespace Blue.Aquarium
{
    /// <summary>
    /// グリッドに設置済みの設置物1つ分
    /// </summary>
    public class PlacedPiece
    {
        private readonly GridPieceData piece;
        private readonly string instanceID;

        public GridPieceData Piece => piece;

        /// <summary>
        /// 設置物1つを指す識別子。展示内容の紐付け先になる
        /// </summary>
        public string InstanceID => instanceID;

        public Vector2Int Cell { get; private set; }
        public int RotationStep { get; private set; }

        public PlacedPiece(GridPieceData piece_data, string instance_id, Vector2Int cell, int rotation_step)
        {
            piece = piece_data;
            instanceID = instance_id;
            Cell = cell;
            RotationStep = AquariumGrid.NormalizeStep(rotation_step);
        }

        /// <summary>
        /// 占有しているセルを列挙する
        /// </summary>
        public IEnumerable<Vector2Int> EnumerateCells()
        {
            return AquariumGrid.EnumerateCells(Cell, piece.Footprint, RotationStep);
        }

        /// <summary>
        /// シーンに置くときの位置
        /// </summary>
        public Vector3 GetWorldPosition()
        {
            return AquariumGrid.CellToWorld(Cell, piece.Footprint, RotationStep);
        }

        /// <summary>
        /// シーンに置くときの向き
        /// </summary>
        public Quaternion GetWorldRotation()
        {
            return AquariumGrid.StepToRotation(RotationStep);
        }

        /// <summary>
        /// 設置位置を変更する
        /// </summary>
        public void MoveTo(Vector2Int cell, int rotation_step)
        {
            Cell = cell;
            RotationStep = AquariumGrid.NormalizeStep(rotation_step);
        }
    }
}
