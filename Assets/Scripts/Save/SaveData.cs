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
        public QuickSlotSaveData quickSlot;
        public CapturedEntitySaveData capturedEntity;
        public long lastSaveTime; // Unix timestamp

        public SaveData()
        {
            playerInventory = new InventorySaveData();
            storageInventory = new InventorySaveData();
            quickSlot = new QuickSlotSaveData();
            capturedEntity = new CapturedEntitySaveData();
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
        public List<DynamicValuePair> dynamicValues = new List<DynamicValuePair>();

        public InventoryItemSaveData(string item_data_path, int qty, List<DynamicValuePair> dynamic_values)
        {
            itemDataPath = item_data_path;
            quantity = qty;
            dynamicValues = dynamic_values ?? new List<DynamicValuePair>();
        }
    }

    /// <summary>
    /// 動的な属性値のペア（JsonUtility用にDictionaryの代わり）
    /// </summary>
    [Serializable]
    public class DynamicValuePair
    {
        public string key;
        public int value;

        public DynamicValuePair(string k, int v)
        {
            key = k;
            value = v;
        }
    }

    /// <summary>
    /// クイックスロットのセーブデータ
    /// </summary>
    [Serializable]
    public class QuickSlotSaveData
    {
        public List<QuickSlotItemSaveData> slots = new List<QuickSlotItemSaveData>();
        public int currentSlotIndex = 0;
    }

    /// <summary>
    /// クイックスロットアイテムのセーブデータ
    /// </summary>
    [Serializable]
    public class QuickSlotItemSaveData
    {
        public string itemDataPath; // ItemDataのResourcesパスまたはGUID
        public int quantity;

        public QuickSlotItemSaveData(string item_data_path, int qty)
        {
            itemDataPath = item_data_path;
            quantity = qty;
        }
    }

    /// <summary>
    /// 捕獲した生物のセーブデータ
    /// </summary>
    [Serializable]
    public class CapturedEntitySaveData
    {
        public List<CapturedEntityItemSaveData> entities = new List<CapturedEntityItemSaveData>();
    }

    /// <summary>
    /// 捕獲した生物アイテムのセーブデータ
    /// </summary>
    [Serializable]
    public class CapturedEntityItemSaveData
    {
        public string entityDataPath; // EntityDataのResourcesパスまたはGUID
        public int quantity;

        public CapturedEntityItemSaveData(string entity_data_path, int qty)
        {
            entityDataPath = entity_data_path;
            quantity = qty;
        }
    }
}
