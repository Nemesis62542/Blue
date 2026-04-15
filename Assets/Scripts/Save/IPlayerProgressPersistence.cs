using System.Collections.Generic;
using Blue.Entity;
using Blue.Inventory;
using Blue.UI.QuickSlot;

namespace Blue.Save
{
    /// <summary>
    /// プレイヤー進行データの保存・読み込みを抽象化するインターフェース
    /// </summary>
    public interface IPlayerProgressPersistence
    {
        InventoryModel LoadPlayerInventory();
        QuickSlotModel LoadQuickSlot();
        Dictionary<EntityData, int> LoadCapturedEntities();

        void SavePlayerInventory(InventoryModel inventory);
        void SaveQuickSlot(QuickSlotModel quickSlot);
        void SaveCapturedEntities(Dictionary<EntityData, int> capturedEntities);
    }
}
