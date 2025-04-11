using NFPS.Entity;
using NFPS.Inventory;
using UnityEngine;

namespace NFPS.Player
{
    public class PlayerModel : BaseEntityModel
    {
        private InventoryModel inventory = new InventoryModel();
        private QuickSlotHandler quickSlotHandler;

        public InventoryModel Inventory => inventory;
        public QuickSlotHandler QuickSlot => quickSlotHandler;

        public override void Initialize(EntityData data)
        {
            base.Initialize(data);

            quickSlotHandler = new QuickSlotHandler(inventory);
        }
    }
}