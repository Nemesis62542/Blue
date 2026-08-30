using System.Collections.Generic;
using Blue.Entity;
using Blue.Item;

namespace Blue.Aquarium
{
    /// <summary>
    /// レイアウトと展示内容を束ねる水族館の正本。シーンには依存しない
    /// </summary>
    public class AquariumModel
    {
        private readonly AquariumLayoutModel layout;
        private readonly ExhibitModel exhibits;

        public AquariumLayoutModel Layout => layout;
        public ExhibitModel Exhibits => exhibits;

        public AquariumModel(AquariumFloorData floor_data)
        {
            layout = new AquariumLayoutModel(floor_data);
            exhibits = new ExhibitModel();

            // 撤去された設置物の展示内容が残らないようにする
            layout.OnPieceRemoved += piece => exhibits.ClearPiece(piece.InstanceID);
        }

        // ---------------- 生物の展示 ----------------

        /// <summary>
        /// 指定した水槽に生物を入れられるかを判定する
        /// </summary>
        public ExhibitRejection CanExhibitEntity(string instance_id, EntityData entity)
        {
            PlacedPiece placed = layout.FindPiece(instance_id);
            if (placed == null) return ExhibitRejection.PieceNotFound;

            if (placed.Piece is not TankPieceData tank) return ExhibitRejection.NotATank;

            return ExhibitRule.EvaluateEntity(tank, exhibits.GetEntities(instance_id), entity);
        }

        /// <summary>
        /// 指定した水槽に生物を入れる
        /// </summary>
        public bool TryExhibitEntity(string instance_id, EntityData entity, out ExhibitRejection rejection)
        {
            rejection = CanExhibitEntity(instance_id, entity);
            if (rejection != ExhibitRejection.None) return false;

            exhibits.AddEntity(instance_id, entity);
            return true;
        }

        public bool RemoveExhibitedEntity(string instance_id, EntityData entity)
        {
            return exhibits.RemoveEntity(instance_id, entity);
        }

        // ---------------- アイテムの展示 ----------------

        /// <summary>
        /// 指定した展示台にアイテムを飾れるかを判定する
        /// </summary>
        public ExhibitRejection CanExhibitItem(string instance_id, ItemData item)
        {
            PlacedPiece placed = layout.FindPiece(instance_id);
            if (placed == null) return ExhibitRejection.PieceNotFound;

            if (placed.Piece is not PedestalPieceData pedestal) return ExhibitRejection.NotAPedestal;

            return ExhibitRule.EvaluateItem(pedestal, exhibits.GetItems(instance_id), item);
        }

        /// <summary>
        /// 指定した展示台にアイテムを飾る
        /// </summary>
        public bool TryExhibitItem(string instance_id, ItemData item, out ExhibitRejection rejection)
        {
            rejection = CanExhibitItem(instance_id, item);
            if (rejection != ExhibitRejection.None) return false;

            exhibits.AddItem(instance_id, item);
            return true;
        }

        public bool RemoveExhibitedItem(string instance_id, ItemData item)
        {
            return exhibits.RemoveItem(instance_id, item);
        }

        // ---------------- 検索 ----------------

        /// <summary>
        /// 指定の生物を受け入れられる水槽を全て挙げる。展示先を選ぶUIで使う
        /// </summary>
        public List<PlacedPiece> FindTanksAccepting(EntityData entity)
        {
            List<PlacedPiece> result = new List<PlacedPiece>();

            foreach (PlacedPiece placed in layout.Pieces)
            {
                if (placed.Piece is not TankPieceData) continue;
                if (CanExhibitEntity(placed.InstanceID, entity) != ExhibitRejection.None) continue;

                result.Add(placed);
            }

            return result;
        }

        /// <summary>
        /// その生物がどこかに展示済みか
        /// </summary>
        public bool IsEntityExhibited(EntityData entity)
        {
            if (entity == null) return false;

            foreach (string instance_id in exhibits.EnumerateTankInstanceIDs())
            {
                IReadOnlyList<EntityData> contents = exhibits.GetEntities(instance_id);
                for (int i = 0; i < contents.Count; i++)
                {
                    if (contents[i] == entity) return true;
                }
            }

            return false;
        }

        /// <summary>
        /// レイアウトと展示内容を全て破棄する
        /// </summary>
        public void Clear()
        {
            layout.Clear();
            exhibits.Clear();
        }
    }
}
