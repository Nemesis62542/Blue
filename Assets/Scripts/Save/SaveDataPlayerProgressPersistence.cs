using System.Collections.Generic;
using Blue.Entity;
using Blue.Inventory;
using Blue.UI.QuickSlot;

namespace Blue.Save
{
    /// <summary>
    /// SaveDataConverter(ドメイン変換) + SaveDataRepository(I/O) で構成した永続化
    /// </summary>
    public class SaveDataPlayerProgressPersistence : IPlayerProgressPersistence
    {
        private readonly ISaveDataRepository repository;

        public SaveDataPlayerProgressPersistence(ISaveDataRepository repository = null)
        {
            this.repository = repository ?? new SaveManagerRepository();
        }

        public InventoryModel LoadPlayerInventory()
        {
            SaveData save_data = repository.LoadCurrent();
            return SaveDataConverter.ConvertFromSaveData(save_data.playerInventory);
        }

        public QuickSlotModel LoadQuickSlot()
        {
            SaveData save_data = repository.LoadCurrent();
            return SaveDataConverter.ConvertFromSaveData(save_data.quickSlot);
        }

        public Dictionary<EntityData, int> LoadCapturedEntities()
        {
            SaveData save_data = repository.LoadCurrent();
            return SaveDataConverter.ConvertFromSaveData(save_data.capturedEntity);
        }

        public void SavePlayerInventory(InventoryModel inventory)
        {
            SaveData save_data = repository.LoadCurrent();
            save_data.playerInventory = SaveDataConverter.ConvertToSaveData(inventory);
            repository.Save(save_data);
        }

        public void SaveQuickSlot(QuickSlotModel quickSlot)
        {
            SaveData save_data = repository.LoadCurrent();
            save_data.quickSlot = SaveDataConverter.ConvertToSaveData(quickSlot);
            repository.Save(save_data);
        }

        public void SaveCapturedEntities(Dictionary<EntityData, int> capturedEntities)
        {
            SaveData save_data = repository.LoadCurrent();
            save_data.capturedEntity = SaveDataConverter.ConvertToSaveData(capturedEntities);
            repository.Save(save_data);
        }
    }
}
