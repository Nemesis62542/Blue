using System;
using System.Collections.Generic;

namespace Blue.Save
{
    /// <summary>
    /// ゲーム全体のセーブデータ
    /// </summary>
    [Serializable]
    public class SaveData
    {
        public InventorySaveData playerInventory;
        public InventorySaveData storageInventory;
        public long lastSaveTime; // Unix timestamp

        public SaveData()
        {
            playerInventory = new InventorySaveData();
            storageInventory = new InventorySaveData();
            lastSaveTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }
    }

    /// <summary>
    /// インベントリのセーブデータ
    /// </summary>
    [Serializable]
    public class InventorySaveData
    {
        public List<InventoryItemSaveData> items = new List<InventoryItemSaveData>();
    }

    /// <summary>
    /// インベントリアイテムのセーブデータ
    /// </summary>
    [Serializable]
    public class InventoryItemSaveData
    {
        public string itemDataPath; // ItemDataのResourcesパスまたはGUID
        public int quantity;
        public Dictionary<string, int> dynamicValues = new Dictionary<string, int>();

        public InventoryItemSaveData(string item_data_path, int qty, Dictionary<string, int> dynamic_values)
        {
            itemDataPath = item_data_path;
            quantity = qty;
            dynamicValues = dynamic_values ?? new Dictionary<string, int>();
        }
    }
}
