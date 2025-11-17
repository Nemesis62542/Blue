using System.Collections.Generic;
using Blue.Entity;
using Blue.Inventory;
using Blue.Item;
using Blue.UI.QuickSlot;
using UnityEngine;

namespace Blue.Save
{
    /// <summary>
    /// ゲームデータとセーブデータの相互変換を行うクラス
    /// </summary>
    public static class SaveDataConverter
    {
        /// <summary>
        /// InventoryModelをInventorySaveDataに変換
        /// </summary>
        public static InventorySaveData ConvertToSaveData(InventoryModel inventory)
        {
            InventorySaveData save_data = new InventorySaveData();

            foreach (InventoryItem item in inventory.InventoryItems)
            {
                string item_path = GetItemDataPath(item.ItemData);
                if (string.IsNullOrEmpty(item_path))
                {
                    Debug.LogWarning($"Failed to get path for ItemData: {item.ItemData.Name}");
                    continue;
                }

                // 動的な属性値を保存
                List<DynamicValuePair> dynamic_values = new List<DynamicValuePair>();
                foreach (ItemAttribute attribute in System.Enum.GetValues(typeof(ItemAttribute)))
                {
                    int value = item.GetDynamicValue(attribute);
                    if (value != 0)
                    {
                        dynamic_values.Add(new DynamicValuePair(attribute.ToString(), value));
                    }
                }

                InventoryItemSaveData item_save_data = new InventoryItemSaveData(
                    item_path,
                    item.Quantity,
                    dynamic_values
                );

                save_data.items.Add(item_save_data);
            }

            return save_data;
        }

        /// <summary>
        /// InventorySaveDataをInventoryModelに変換
        /// </summary>
        public static InventoryModel ConvertFromSaveData(InventorySaveData save_data)
        {
            InventoryModel inventory = new InventoryModel();

            if (save_data == null || save_data.items == null)
            {
                return inventory;
            }

            foreach (InventoryItemSaveData item_data in save_data.items)
            {
                ItemData item = LoadItemData(item_data.itemDataPath);
                if (item == null)
                {
                    Debug.LogWarning($"Failed to load ItemData at path: {item_data.itemDataPath}");
                    continue;
                }

                // アイテムを追加
                inventory.AddItem(item, item_data.quantity);

                // 動的な属性値を復元
                if (inventory.TryGetItem(item, out InventoryItem inventory_item) && item_data.dynamicValues != null)
                {
                    foreach (DynamicValuePair pair in item_data.dynamicValues)
                    {
                        if (System.Enum.TryParse(pair.key, out ItemAttribute attribute))
                        {
                            inventory_item.SetDynamicValue(attribute, pair.value);
                        }
                    }
                }
            }

            return inventory;
        }

        /// <summary>
        /// ItemDataのGUIDを取得
        /// </summary>
        private static string GetItemDataPath(ItemData item_data)
        {
            if (item_data == null) return null;

#if UNITY_EDITOR
            // エディタではGUIDを直接取得
            return item_data.ItemID;
#else
            // ランタイムではキャッシュから取得
            return ItemDataCache.GetGUID(item_data);
#endif
        }

        /// <summary>
        /// GUIDからItemDataを読み込み
        /// </summary>
        private static ItemData LoadItemData(string guid)
        {
            if (string.IsNullOrEmpty(guid)) return null;

#if UNITY_EDITOR
            // エディタではAssetDatabaseから直接読み込み
            string asset_path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            if (!string.IsNullOrEmpty(asset_path))
            {
                return UnityEditor.AssetDatabase.LoadAssetAtPath<ItemData>(asset_path);
            }
#else
            // ランタイムではキャッシュから取得
            ItemData item = ItemDataCache.GetItemByGUID(guid);
            if (item != null)
            {
                return item;
            }
#endif

            Debug.LogWarning($"ItemData not found for GUID: {guid}");
            return null;
        }

        /// <summary>
        /// プレイヤーインベントリを保存
        /// </summary>
        public static void SavePlayerInventory(InventoryModel inventory)
        {
            SaveData save_data = SaveManager.CurrentSaveData;
            save_data.playerInventory = ConvertToSaveData(inventory);
            SaveManager.Save();
        }

        /// <summary>
        /// 倉庫インベントリを保存
        /// </summary>
        public static void SaveStorageInventory(InventoryModel inventory)
        {
            SaveData save_data = SaveManager.CurrentSaveData;
            save_data.storageInventory = ConvertToSaveData(inventory);
            SaveManager.Save();
        }

        /// <summary>
        /// プレイヤーインベントリを読み込み
        /// </summary>
        public static InventoryModel LoadPlayerInventory()
        {
            SaveData save_data = SaveManager.CurrentSaveData;
            return ConvertFromSaveData(save_data.playerInventory);
        }

        /// <summary>
        /// 倉庫インベントリを読み込み
        /// </summary>
        public static InventoryModel LoadStorageInventory()
        {
            SaveData save_data = SaveManager.CurrentSaveData;
            return ConvertFromSaveData(save_data.storageInventory);
        }

        /// <summary>
        /// QuickSlotModelをQuickSlotSaveDataに変換
        /// </summary>
        public static QuickSlotSaveData ConvertToSaveData(QuickSlotModel quickSlot)
        {
            QuickSlotSaveData save_data = new QuickSlotSaveData();
            save_data.currentSlotIndex = quickSlot.CurrentSlotIndex;

            foreach (QuickSlotItem slot_item in quickSlot.QuickSlots)
            {
                if (slot_item != null && slot_item.ItemData != null)
                {
                    string item_path = GetItemDataPath(slot_item.ItemData);
                    if (!string.IsNullOrEmpty(item_path))
                    {
                        save_data.slots.Add(new QuickSlotItemSaveData(item_path, slot_item.Quantity));
                    }
                    else
                    {
                        save_data.slots.Add(null); // 空スロット
                    }
                }
                else
                {
                    save_data.slots.Add(null); // 空スロット
                }
            }

            return save_data;
        }

        /// <summary>
        /// QuickSlotSaveDataをQuickSlotModelに変換
        /// </summary>
        public static QuickSlotModel ConvertFromSaveData(QuickSlotSaveData save_data)
        {
            QuickSlotModel quickSlot = new QuickSlotModel();

            if (save_data == null || save_data.slots == null)
            {
                return quickSlot;
            }

            // スロットを復元
            for (int i = 0; i < save_data.slots.Count; i++)
            {
                QuickSlotItemSaveData slot_data = save_data.slots[i];
                if (slot_data != null && !string.IsNullOrEmpty(slot_data.itemDataPath))
                {
                    ItemData item = LoadItemData(slot_data.itemDataPath);
                    if (item != null)
                    {
                        quickSlot.AddItem(item, slot_data.quantity);
                    }
                }
            }

            // 現在選択中のスロットを復元
            if (save_data.currentSlotIndex >= 0 && save_data.currentSlotIndex < save_data.slots.Count)
            {
                quickSlot.SelectSlot(save_data.currentSlotIndex);
            }

            return quickSlot;
        }

        /// <summary>
        /// クイックスロットを保存
        /// </summary>
        public static void SaveQuickSlot(QuickSlotModel quickSlot)
        {
            SaveData save_data = SaveManager.CurrentSaveData;
            save_data.quickSlot = ConvertToSaveData(quickSlot);
            SaveManager.Save();
        }

        /// <summary>
        /// クイックスロットを読み込み
        /// </summary>
        public static QuickSlotModel LoadQuickSlot()
        {
            SaveData save_data = SaveManager.CurrentSaveData;
            return ConvertFromSaveData(save_data.quickSlot);
        }

        /// <summary>
        /// 捕獲した生物のDictionaryをCapturedEntitySaveDataに変換
        /// </summary>
        public static CapturedEntitySaveData ConvertToSaveData(Dictionary<EntityData, int> capturedEntities)
        {
            CapturedEntitySaveData save_data = new CapturedEntitySaveData();

            if (capturedEntities == null)
            {
                return save_data;
            }

            foreach (KeyValuePair<EntityData, int> pair in capturedEntities)
            {
                string entity_path = GetEntityDataPath(pair.Key);
                if (string.IsNullOrEmpty(entity_path))
                {
                    Debug.LogWarning($"Failed to get path for EntityData: {pair.Key.Name}");
                    continue;
                }

                CapturedEntityItemSaveData item_save_data = new CapturedEntityItemSaveData(
                    entity_path,
                    pair.Value
                );

                save_data.entities.Add(item_save_data);
            }

            return save_data;
        }

        /// <summary>
        /// CapturedEntitySaveDataを捕獲した生物のDictionaryに変換
        /// </summary>
        public static Dictionary<EntityData, int> ConvertFromSaveData(CapturedEntitySaveData save_data)
        {
            Dictionary<EntityData, int> captured_entities = new Dictionary<EntityData, int>();

            if (save_data == null || save_data.entities == null)
            {
                return captured_entities;
            }

            foreach (CapturedEntityItemSaveData entity_data in save_data.entities)
            {
                EntityData entity = LoadEntityData(entity_data.entityDataPath);
                if (entity == null)
                {
                    Debug.LogWarning($"Failed to load EntityData at path: {entity_data.entityDataPath}");
                    continue;
                }

                captured_entities[entity] = entity_data.quantity;
            }

            return captured_entities;
        }

        /// <summary>
        /// EntityDataのGUIDを取得
        /// </summary>
        private static string GetEntityDataPath(EntityData entity_data)
        {
            if (entity_data == null) return null;

#if UNITY_EDITOR
            // エディタではAssetDatabaseからGUIDを取得
            string asset_path = UnityEditor.AssetDatabase.GetAssetPath(entity_data);
            if (!string.IsNullOrEmpty(asset_path))
            {
                return UnityEditor.AssetDatabase.AssetPathToGUID(asset_path);
            }
#else
            // ランタイムではキャッシュから取得（将来的に実装）
            // TODO: EntityDataCacheの実装が必要
            Debug.LogWarning("Runtime entity loading not yet implemented. Need EntityDataCache.");
#endif

            return null;
        }

        /// <summary>
        /// GUIDからEntityDataを読み込み
        /// </summary>
        private static EntityData LoadEntityData(string guid)
        {
            if (string.IsNullOrEmpty(guid)) return null;

#if UNITY_EDITOR
            // エディタではAssetDatabaseから直接読み込み
            string asset_path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            if (!string.IsNullOrEmpty(asset_path))
            {
                return UnityEditor.AssetDatabase.LoadAssetAtPath<EntityData>(asset_path);
            }
#else
            // ランタイムではキャッシュから取得（将来的に実装）
            // TODO: EntityDataCacheの実装が必要
            Debug.LogWarning("Runtime entity loading not yet implemented. Need EntityDataCache.");
#endif

            Debug.LogWarning($"EntityData not found for GUID: {guid}");
            return null;
        }

        /// <summary>
        /// 捕獲した生物を保存
        /// </summary>
        public static void SaveCapturedEntities(Dictionary<EntityData, int> capturedEntities)
        {
            SaveData save_data = SaveManager.CurrentSaveData;
            save_data.capturedEntity = ConvertToSaveData(capturedEntities);
            SaveManager.Save();
        }

        /// <summary>
        /// 捕獲した生物を読み込み
        /// </summary>
        public static Dictionary<EntityData, int> LoadCapturedEntities()
        {
            SaveData save_data = SaveManager.CurrentSaveData;
            return ConvertFromSaveData(save_data.capturedEntity);
        }

        /// <summary>
        /// 捕獲した生物のリストを読み込み
        /// </summary>
        public static List<EntityData> LoadCapturedEntitiesList()
        {
            Dictionary<EntityData, int> captured_entities = LoadCapturedEntities();
            return new List<EntityData>(captured_entities.Keys);
        }
    }
}
