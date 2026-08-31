using System.Collections.Generic;
using Blue.Entity;
using Blue.Item;
using Blue.Save;
using UnityEngine;

namespace Blue.Aquarium
{
    /// <summary>
    /// AquariumModelとセーブデータの相互変換を行うクラス
    /// </summary>
    public static class AquariumSaveConverter
    {
        /// <summary>
        /// AquariumModelをAquariumSaveDataに変換
        /// </summary>
        public static AquariumSaveData ConvertToSaveData(AquariumModel model)
        {
            AquariumSaveData save_data = new AquariumSaveData();

            if (model == null) return save_data;

            save_data.unlockedRoomIDs.AddRange(model.Layout.UnlockedRooms);

            foreach (PlacedPiece placed in model.Layout.Pieces)
            {
                string piece_guid = placed.Piece.PieceID;
                if (string.IsNullOrEmpty(piece_guid))
                {
                    Debug.LogWarning($"Failed to get GUID for AquariumPieceData: {placed.Piece.Name}");
                    continue;
                }

                save_data.pieces.Add(new PlacedPieceSaveData(
                    piece_guid,
                    placed.InstanceID,
                    placed.Cell.x,
                    placed.Cell.y,
                    placed.RotationStep
                ));
            }

            foreach (PlacedDecor decor in model.Layout.Decors)
            {
                string piece_guid = decor.Piece.PieceID;
                if (string.IsNullOrEmpty(piece_guid))
                {
                    Debug.LogWarning($"Failed to get GUID for DecorPieceData: {decor.Piece.Name}");
                    continue;
                }

                save_data.decors.Add(new PlacedDecorSaveData(
                    piece_guid,
                    decor.InstanceID,
                    decor.ParentInstanceID,
                    decor.Position.x,
                    decor.Position.y,
                    decor.Position.z,
                    decor.Yaw
                ));
            }

            foreach (string instance_id in model.Exhibits.EnumerateTankInstanceIDs())
            {
                List<string> guids = new List<string>();
                foreach (EntityData entity in model.Exhibits.GetEntities(instance_id))
                {
                    string guid = entity.EntityGUID;
                    if (!string.IsNullOrEmpty(guid))
                    {
                        guids.Add(guid);
                    }
                }

                if (guids.Count > 0)
                {
                    save_data.tankExhibits.Add(new ExhibitSaveData(instance_id, guids));
                }
            }

            foreach (string instance_id in model.Exhibits.EnumeratePedestalInstanceIDs())
            {
                List<string> guids = new List<string>();
                foreach (ItemData item in model.Exhibits.GetItems(instance_id))
                {
                    string guid = item.ItemID;
                    if (!string.IsNullOrEmpty(guid))
                    {
                        guids.Add(guid);
                    }
                }

                if (guids.Count > 0)
                {
                    save_data.pedestalExhibits.Add(new ExhibitSaveData(instance_id, guids));
                }
            }

            return save_data;
        }

        /// <summary>
        /// AquariumSaveDataをAquariumModelに変換
        /// </summary>
        public static AquariumModel ConvertFromSaveData(AquariumSaveData save_data, AquariumFloorData floor_data, IEntityStock stock = null)
        {
            AquariumModel model = new AquariumModel(floor_data) { Stock = stock };

            if (save_data == null) return model;

            // 設置の可否は解放済みの部屋で判定されるので、部屋の復元を先に済ませる
            if (save_data.unlockedRoomIDs != null)
            {
                foreach (string room_id in save_data.unlockedRoomIDs)
                {
                    model.Layout.UnlockRoom(room_id);
                }
            }

            RestorePieces(save_data, model);
            RestoreDecors(save_data, model);
            RestoreExhibits(save_data, model);

            return model;
        }

        private static void RestorePieces(AquariumSaveData save_data, AquariumModel model)
        {
            if (save_data.pieces == null) return;

            foreach (PlacedPieceSaveData piece_data in save_data.pieces)
            {
                GridPieceData piece = AquariumPieceCache.GetPieceByGUID<GridPieceData>(piece_data.pieceGUID);
                if (piece == null)
                {
                    Debug.LogWarning($"AquariumPieceData not found for GUID: {piece_data.pieceGUID}");
                    continue;
                }

                Vector2Int cell = new Vector2Int(piece_data.cellX, piece_data.cellY);
                model.Layout.TryPlace(piece, cell, piece_data.rotationStep, out PlacementRejection rejection, piece_data.instanceID);

                if (rejection != PlacementRejection.None)
                {
                    Debug.LogWarning($"設置物を復元できませんでした: {piece.Name} ({rejection})");
                }
            }
        }

        private static void RestoreDecors(AquariumSaveData save_data, AquariumModel model)
        {
            if (save_data.decors == null) return;

            foreach (PlacedDecorSaveData decor_data in save_data.decors)
            {
                DecorPieceData piece = AquariumPieceCache.GetPieceByGUID<DecorPieceData>(decor_data.pieceGUID);
                if (piece == null)
                {
                    Debug.LogWarning($"DecorPieceData not found for GUID: {decor_data.pieceGUID}");
                    continue;
                }

                Vector3 position = new Vector3(decor_data.positionX, decor_data.positionY, decor_data.positionZ);
                model.Layout.PlaceDecor(piece, position, decor_data.yaw, decor_data.parentInstanceID, decor_data.instanceID);
            }
        }

        private static void RestoreExhibits(AquariumSaveData save_data, AquariumModel model)
        {
            if (save_data.tankExhibits != null)
            {
                foreach (ExhibitSaveData exhibit in save_data.tankExhibits)
                {
                    foreach (string guid in exhibit.contentGUIDs)
                    {
                        EntityData entity = EntityDataCache.GetEntityByGUID(guid);
                        if (entity == null)
                        {
                            Debug.LogWarning($"EntityData not found for GUID: {guid}");
                            continue;
                        }

                        if (!model.TryExhibitEntity(exhibit.instanceID, entity, out ExhibitRejection rejection))
                        {
                            Debug.LogWarning($"展示を復元できませんでした: {entity.Name} ({rejection})");
                        }
                    }
                }
            }

            if (save_data.pedestalExhibits == null) return;

            foreach (ExhibitSaveData exhibit in save_data.pedestalExhibits)
            {
                foreach (string guid in exhibit.contentGUIDs)
                {
                    ItemData item = ItemDataCache.GetItemByGUID(guid);
                    if (item == null)
                    {
                        Debug.LogWarning($"ItemData not found for GUID: {guid}");
                        continue;
                    }

                    if (!model.TryExhibitItem(exhibit.instanceID, item, out ExhibitRejection rejection))
                    {
                        Debug.LogWarning($"展示を復元できませんでした: {item.Name} ({rejection})");
                    }
                }
            }
        }

        /// <summary>
        /// 水族館を保存
        /// </summary>
        public static void SaveAquarium(AquariumModel model)
        {
            SaveData save_data = SaveManager.CurrentSaveData;
            save_data.aquarium = ConvertToSaveData(model);
            SaveManager.Save();
        }

        /// <summary>
        /// 水族館を読み込み
        /// </summary>
        public static AquariumModel LoadAquarium(AquariumFloorData floor_data, IEntityStock stock = null)
        {
            SaveData save_data = SaveManager.CurrentSaveData;

            // 水族館の実装より前のセーブデータには項目そのものが無い
            save_data.aquarium ??= new AquariumSaveData();

            // 所持数は復元の前に渡す。所持を超えて保存されていた場合、
            // 読み込んだ時点で上限まで戻したい
            return ConvertFromSaveData(save_data.aquarium, floor_data, stock);
        }
    }
}
