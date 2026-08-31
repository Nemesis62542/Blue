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
        public UpgradeSaveData upgrades;
        public AquariumSaveData aquarium;
        public long lastSaveTime; // Unix timestamp

        public SaveData()
        {
            playerInventory = new InventorySaveData();
            storageInventory = new InventorySaveData();
            quickSlot = new QuickSlotSaveData();
            capturedEntity = new CapturedEntitySaveData();
            upgrades = new UpgradeSaveData();
            aquarium = new AquariumSaveData();
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

    /// <summary>
    /// アップグレードのセーブデータ
    /// </summary>
    [Serializable]
    public class UpgradeSaveData
    {
        public int oxygenLevel = 0;      // 酸素アップグレードレベル (0=未強化)
        public int depthLevel = 0;       // 深度アップグレードレベル (0=未強化)
        public int subCapacityLevel = 0; // サブアップグレード容量レベル (0=未強化)
        public List<SubUpgradeItemSaveData> subUpgrades = new List<SubUpgradeItemSaveData>();
    }

    /// <summary>
    /// 水族館のセーブデータ
    /// </summary>
    [Serializable]
    public class AquariumSaveData
    {
        public List<string> unlockedRoomIDs = new List<string>();
        public List<PlacedPieceSaveData> pieces = new List<PlacedPieceSaveData>();
        public List<PlacedDecorSaveData> decors = new List<PlacedDecorSaveData>();
        public List<ExhibitSaveData> tankExhibits = new List<ExhibitSaveData>();
        public List<ExhibitSaveData> pedestalExhibits = new List<ExhibitSaveData>();
    }

    /// <summary>
    /// グリッドに設置した設置物1つ分のセーブデータ
    /// </summary>
    [Serializable]
    public class PlacedPieceSaveData
    {
        public string pieceGUID;   // AquariumPieceDataのGUID
        public string instanceID;  // 展示内容と装飾から参照される識別子
        public int cellX;
        public int cellY;
        public int rotationStep;

        public PlacedPieceSaveData(string piece_guid, string instance_id, int cell_x, int cell_y, int rotation_step)
        {
            pieceGUID = piece_guid;
            instanceID = instance_id;
            cellX = cell_x;
            cellY = cell_y;
            rotationStep = rotation_step;
        }
    }

    /// <summary>
    /// 自由配置した装飾1つ分のセーブデータ
    /// </summary>
    [Serializable]
    public class PlacedDecorSaveData
    {
        public string pieceGUID;          // AquariumPieceDataのGUID
        public string instanceID;
        public string parentInstanceID;   // 載せる先の設置物。単独で置く場合は空
        public float positionX;
        public float positionY;
        public float positionZ;
        public float yaw;

        public PlacedDecorSaveData(string piece_guid, string instance_id, string parent_instance_id, float position_x, float position_y, float position_z, float yaw_angle)
        {
            pieceGUID = piece_guid;
            instanceID = instance_id;
            parentInstanceID = parent_instance_id;
            positionX = position_x;
            positionY = position_y;
            positionZ = position_z;
            yaw = yaw_angle;
        }
    }

    /// <summary>
    /// 設置物1つ分の展示内容のセーブデータ
    /// </summary>
    [Serializable]
    public class ExhibitSaveData
    {
        public string instanceID;                                // 展示先の設置物
        public List<string> contentGUIDs = new List<string>();    // EntityDataまたはItemDataのGUID

        public ExhibitSaveData(string instance_id, List<string> content_guids)
        {
            instanceID = instance_id;
            contentGUIDs = content_guids ?? new List<string>();
        }
    }

    /// <summary>
    /// サブアップグレードアイテムのセーブデータ
    /// </summary>
    [Serializable]
    public class SubUpgradeItemSaveData
    {
        public string subUpgradeId;  // SubUpgradeDataのアセットパス
        public bool isUnlocked;      // 解放済みか
        public bool isEquipped;      // 装備中か

        public SubUpgradeItemSaveData(string id, bool unlocked, bool equipped)
        {
            subUpgradeId = id;
            isUnlocked = unlocked;
            isEquipped = equipped;
        }
    }
}
