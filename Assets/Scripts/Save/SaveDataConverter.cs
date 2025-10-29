using System.Collections.Generic;
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
                Dictionary<string, int> dynamic_values = new Dictionary<string, int>();
                foreach (ItemAttribute attribute in System.Enum.GetValues(typeof(ItemAttribute)))
                {
                    int value = item.GetDynamicValue(attribute);
                    if (value != 0)
                    {
                        dynamic_values[attribute.ToString()] = value;
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
                if (inventory.TryGetItem(item, out InventoryItem inventory_item))
                {
                    foreach (KeyValuePair<string, int> kvp in item_data.dynamicValues)
                    {
                        if (System.Enum.TryParse(kvp.Key, out ItemAttribute attribute))
                        {
                            inventory_item.SetDynamicValue(attribute, kvp.Value);
                        }
                    }
                }
            }

            return inventory;
        }

        /// <summary>
        /// ItemDataのパスを取得（AssetDatabaseを使用しない方法）
        /// </summary>
        private static string GetItemDataPath(ItemData item_data)
        {
            if (item_data == null) return null;

            // ScriptableObjectの名前をパスとして使用
            // 実際の実装では、ItemDataにユニークなIDを持たせるか、
            // Resourcesフォルダに配置してResources.Loadで読み込む方法を推奨
            return item_data.name;
        }

        /// <summary>
        /// パスからItemDataを読み込み
        /// </summary>
        private static ItemData LoadItemData(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;

            // Resourcesフォルダから読み込む場合
            // ItemData item = Resources.Load<ItemData>($"Items/{path}");

            // 現状はFind.objectOfTypeAllを使用（パフォーマンスが悪いため、本番では改善が必要）
            ItemData[] all_items = Resources.FindObjectsOfTypeAll<ItemData>();
            foreach (ItemData item in all_items)
            {
                if (item.name == path)
                {
                    return item;
                }
            }

            Debug.LogWarning($"ItemData not found: {path}");
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
    }
}
