using System;
using System.Collections.Generic;
using UnityEngine;

namespace Blue.Aquarium
{
    /// <summary>
    /// 設置物の配置とセルの占有を管理する。シーンには依存しない
    /// </summary>
    public class AquariumLayoutModel
    {
        private readonly AquariumFloorData floor;
        private readonly HashSet<string> unlockedRooms = new HashSet<string>();
        private readonly List<PlacedPiece> pieces = new List<PlacedPiece>();
        private readonly List<PlacedDecor> decors = new List<PlacedDecor>();
        private readonly Dictionary<Vector2Int, PlacedPiece> occupancy = new Dictionary<Vector2Int, PlacedPiece>();
        private readonly Dictionary<string, PlacedPiece> pieceByInstance = new Dictionary<string, PlacedPiece>();
        private readonly Dictionary<string, PlacedDecor> decorByInstance = new Dictionary<string, PlacedDecor>();

        public AquariumFloorData Floor => floor;
        public IReadOnlyList<PlacedPiece> Pieces => pieces;
        public IReadOnlyList<PlacedDecor> Decors => decors;
        public IReadOnlyCollection<string> UnlockedRooms => unlockedRooms;

        public event Action<PlacedPiece> OnPiecePlaced;
        public event Action<PlacedPiece> OnPieceRemoved;
        public event Action<PlacedPiece> OnPieceMoved;
        public event Action<PlacedDecor> OnDecorPlaced;
        public event Action<PlacedDecor> OnDecorRemoved;
        public event Action<string> OnRoomUnlocked;

        public AquariumLayoutModel(AquariumFloorData floor_data)
        {
            floor = floor_data;

            if (floor == null) return;

            foreach (AquariumRoomDefinition room in floor.Rooms)
            {
                if (room.UnlockedFromStart)
                {
                    unlockedRooms.Add(room.RoomID);
                }
            }
        }

        // ---------------- 部屋 ----------------

        public bool IsRoomUnlocked(string room_id)
        {
            return unlockedRooms.Contains(room_id);
        }

        /// <summary>
        /// 部屋を解放して設置可能エリアを広げる
        /// </summary>
        public bool UnlockRoom(string room_id)
        {
            if (string.IsNullOrEmpty(room_id)) return false;
            if (floor == null || floor.FindRoom(room_id) == null) return false;
            if (!unlockedRooms.Add(room_id)) return false;

            MarkPathDirty();

            OnRoomUnlocked?.Invoke(room_id);
            return true;
        }

        /// <summary>
        /// 解放済みの部屋に含まれるセルか
        /// </summary>
        public bool IsCellPlaceable(Vector2Int cell)
        {
            if (floor == null) return false;

            foreach (AquariumRoomDefinition room in floor.Rooms)
            {
                if (!unlockedRooms.Contains(room.RoomID)) continue;
                if (room.Contains(cell)) return true;
            }

            return false;
        }

        // ---------------- 設置物 ----------------

        /// <summary>
        /// セルを占有している設置物を取得する。空きなら null
        /// </summary>
        public PlacedPiece GetPieceAt(Vector2Int cell)
        {
            return occupancy.TryGetValue(cell, out PlacedPiece piece) ? piece : null;
        }

        public PlacedPiece FindPiece(string instance_id)
        {
            if (string.IsNullOrEmpty(instance_id)) return null;

            return pieceByInstance.TryGetValue(instance_id, out PlacedPiece piece) ? piece : null;
        }

        /// <summary>
        /// 設置できるかを判定する
        /// </summary>
        /// <param name="ignore_instance_id">移動中の設置物。自分自身との重なりを無視する</param>
        public PlacementRejection CanPlace(GridPieceData piece_data, Vector2Int cell, int rotation_step, string ignore_instance_id = null)
        {
            if (piece_data == null) return PlacementRejection.InvalidPiece;

            foreach (Vector2Int occupied in AquariumGrid.EnumerateCells(cell, piece_data.Footprint, rotation_step))
            {
                if (!IsCellPlaceable(occupied))
                {
                    return PlacementRejection.OutsideUnlockedArea;
                }

                PlacedPiece existing = GetPieceAt(occupied);
                if (existing != null && existing.InstanceID != ignore_instance_id)
                {
                    return PlacementRejection.CellOccupied;
                }
            }

            return PlacementRejection.None;
        }

        /// <summary>
        /// 設置物を置く。置けなかった場合は null を返す
        /// </summary>
        public PlacedPiece TryPlace(GridPieceData piece_data, Vector2Int cell, int rotation_step, out PlacementRejection rejection, string instance_id = null)
        {
            rejection = CanPlace(piece_data, cell, rotation_step);
            if (rejection != PlacementRejection.None) return null;

            string id = string.IsNullOrEmpty(instance_id) ? Guid.NewGuid().ToString("N") : instance_id;
            PlacedPiece placed = new PlacedPiece(piece_data, id, cell, rotation_step);

            pieces.Add(placed);
            pieceByInstance[id] = placed;
            OccupyCells(placed);

            OnPiecePlaced?.Invoke(placed);
            return placed;
        }

        /// <summary>
        /// 設置済みの設置物を別のセルへ動かす
        /// </summary>
        public bool TryMove(string instance_id, Vector2Int cell, int rotation_step, out PlacementRejection rejection)
        {
            PlacedPiece placed = FindPiece(instance_id);
            if (placed == null)
            {
                rejection = PlacementRejection.InvalidPiece;
                return false;
            }

            rejection = CanPlace(placed.Piece, cell, rotation_step, instance_id);
            if (rejection != PlacementRejection.None) return false;

            ReleaseCells(placed);
            placed.MoveTo(cell, rotation_step);
            OccupyCells(placed);

            OnPieceMoved?.Invoke(placed);
            return true;
        }

        /// <summary>
        /// 設置物を撤去する。載っていた装飾も一緒に外れる
        /// </summary>
        public bool RemovePiece(string instance_id)
        {
            PlacedPiece placed = FindPiece(instance_id);
            if (placed == null) return false;

            ReleaseCells(placed);
            pieces.Remove(placed);
            pieceByInstance.Remove(instance_id);

            RemoveDecorsOn(instance_id);

            OnPieceRemoved?.Invoke(placed);
            return true;
        }

        private void OccupyCells(PlacedPiece placed)
        {
            MarkPathDirty();

            foreach (Vector2Int cell in placed.EnumerateCells())
            {
                occupancy[cell] = placed;
            }
        }

        private void ReleaseCells(PlacedPiece placed)
        {
            MarkPathDirty();

            foreach (Vector2Int cell in placed.EnumerateCells())
            {
                if (occupancy.TryGetValue(cell, out PlacedPiece current) && current == placed)
                {
                    occupancy.Remove(cell);
                }
            }
        }

        // ---------------- 装飾 ----------------

        public PlacedDecor FindDecor(string instance_id)
        {
            if (string.IsNullOrEmpty(instance_id)) return null;

            return decorByInstance.TryGetValue(instance_id, out PlacedDecor decor) ? decor : null;
        }

        /// <summary>
        /// 装飾を自由配置で置く
        /// </summary>
        public PlacedDecor PlaceDecor(DecorPieceData piece_data, Vector3 position, float yaw, string parent_instance_id = null, string instance_id = null)
        {
            if (piece_data == null) return null;

            // 親を指定するなら、その設置物が実在していなければならない
            if (!string.IsNullOrEmpty(parent_instance_id) && FindPiece(parent_instance_id) == null)
            {
                Debug.LogWarning($"装飾の親が見つかりません: {parent_instance_id}");
                return null;
            }

            string id = string.IsNullOrEmpty(instance_id) ? Guid.NewGuid().ToString("N") : instance_id;
            PlacedDecor decor = new PlacedDecor(piece_data, id, parent_instance_id, position, yaw);

            decors.Add(decor);
            decorByInstance[id] = decor;

            OnDecorPlaced?.Invoke(decor);
            return decor;
        }

        public bool RemoveDecor(string instance_id)
        {
            PlacedDecor decor = FindDecor(instance_id);
            if (decor == null) return false;

            decors.Remove(decor);
            decorByInstance.Remove(instance_id);

            OnDecorRemoved?.Invoke(decor);
            return true;
        }

        private void RemoveDecorsOn(string parent_instance_id)
        {
            for (int i = decors.Count - 1; i >= 0; i--)
            {
                if (decors[i].ParentInstanceID != parent_instance_id) continue;

                PlacedDecor decor = decors[i];
                decors.RemoveAt(i);
                decorByInstance.Remove(decor.InstanceID);

                OnDecorRemoved?.Invoke(decor);
            }
        }

        // ---------------- 通路 ----------------

        // 入口から歩いて辿り着ける通路セル。設置や部屋の解放で変わるので、
        // 変化を知らせて必要になった時点で引き直す
        private readonly HashSet<Vector2Int> connectedPath = new HashSet<Vector2Int>();
        private readonly Queue<Vector2Int> pathFrontier = new Queue<Vector2Int>();
        private bool pathDirty = true;

        private static readonly Vector2Int[] Neighbours =
        {
            Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right,
        };

        /// <summary>
        /// そのセルを歩けるか
        /// </summary>
        // 空きセルは通路にしない。床の上をどこでも歩けることにすると、
        // 全ての水槽が自動的に通路に面してしまい、導線を引く意味が無くなる
        public bool IsWalkable(Vector2Int cell)
        {
            PlacedPiece piece = GetPieceAt(cell);

            return piece != null && piece.Piece.Walkable;
        }

        /// <summary>
        /// 入口から歩いて辿り着けるセルか
        /// </summary>
        public bool IsPathConnected(Vector2Int cell)
        {
            EnsurePath();

            return connectedPath.Contains(cell);
        }

        /// <summary>
        /// その設置物が、入口から繋がった通路に面しているか
        /// </summary>
        // 見に行けない場所に置かれた水槽を知らせるための判定。
        // 設置そのものは断らない（導線の失敗は作り直せばよい）
        public bool IsFacingPath(PlacedPiece placed)
        {
            if (placed == null) return false;

            return IsFacingPath(placed.Cell, placed.Piece.Footprint, placed.RotationStep);
        }

        /// <summary>
        /// そこに置いたとして、入口から繋がった通路に面するか
        /// </summary>
        // 置く前に知りたいので、設置済みかどうかに関わらず占有セルから判定する
        public bool IsFacingPath(Vector2Int cell, Vector2Int footprint, int rotation_step)
        {
            EnsurePath();

            foreach (Vector2Int occupied in AquariumGrid.EnumerateCells(cell, footprint, rotation_step))
            {
                foreach (Vector2Int offset in Neighbours)
                {
                    if (connectedPath.Contains(occupied + offset)) return true;
                }
            }

            return false;
        }

        private void EnsurePath()
        {
            if (!pathDirty) return;

            pathDirty = false;
            connectedPath.Clear();
            pathFrontier.Clear();

            if (floor == null) return;

            // 入口そのものは通路が置かれていなくても起点として扱う。
            // 入口に通路を置き忘れただけで全てが未接続になると、原因が分かりにくい
            foreach (AquariumRoomDefinition room in floor.Rooms)
            {
                if (!unlockedRooms.Contains(room.RoomID)) continue;

                foreach (Vector2Int entrance in room.Entrances)
                {
                    if (connectedPath.Add(entrance)) pathFrontier.Enqueue(entrance);
                }
            }

            while (pathFrontier.Count > 0)
            {
                Vector2Int cell = pathFrontier.Dequeue();

                foreach (Vector2Int offset in Neighbours)
                {
                    Vector2Int next = cell + offset;

                    if (connectedPath.Contains(next)) continue;
                    if (!IsCellPlaceable(next) || !IsWalkable(next)) continue;

                    connectedPath.Add(next);
                    pathFrontier.Enqueue(next);
                }
            }
        }

        private void MarkPathDirty()
        {
            pathDirty = true;
        }

        /// <summary>
        /// 全ての配置を破棄する
        /// </summary>
        public void Clear()
        {
            for (int i = pieces.Count - 1; i >= 0; i--)
            {
                RemovePiece(pieces[i].InstanceID);
            }

            for (int i = decors.Count - 1; i >= 0; i--)
            {
                RemoveDecor(decors[i].InstanceID);
            }
        }
    }

    /// <summary>
    /// 設置を断った理由
    /// </summary>
    public enum PlacementRejection
    {
        None,                // 設置できる
        InvalidPiece,        // 設置物が不正、または存在しない
        OutsideUnlockedArea, // 解放済みの部屋の外
        CellOccupied,        // 他の設置物と重なる
    }
}
