using System.Collections.Generic;
using Blue.Entity;
using Blue.Inventory;
using Blue.UI.QuickSlot;

namespace Blue.Save
{
    /// <summary>
    /// SaveDataConverterを利用したプレイヤー進行データ永続化
    /// </summary>
    public class SaveDataPlayerProgressPersistence : IPlayerProgressPersistence
    {
        public InventoryModel LoadPlayerInventory()
        {
            return SaveDataConverter.LoadPlayerInventory();
        }

        public QuickSlotModel LoadQuickSlot()
        {
            return SaveDataConverter.LoadQuickSlot();
        }

        public Dictionary<EntityData, int> LoadCapturedEntities()
        {
            return SaveDataConverter.LoadCapturedEntities();
        }

        public void SavePlayerInventory(InventoryModel inventory)
        {
            SaveDataConverter.SavePlayerInventory(inventory);
        }

        public void SaveQuickSlot(QuickSlotModel quickSlot)
        {
            SaveDataConverter.SaveQuickSlot(quickSlot);
        }

        public void SaveCapturedEntities(Dictionary<EntityData, int> capturedEntities)
        {
            SaveDataConverter.SaveCapturedEntities(capturedEntities);
        }
    }
}
