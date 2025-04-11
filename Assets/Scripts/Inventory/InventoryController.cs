using NFPS.Input;
using UnityEngine;

namespace NFPS.Inventory
{
    public class InventoryController : MonoBehaviour
    {
        [SerializeField] private InventoryView view;

        private InventoryModel model;
        private QuickSlotHandler quickSlotHandler;

        public void Initialize(InventoryModel model, QuickSlotHandler quick_slot_handler, PlayerInputHandler input_handler)
        {
            this.model = model;
            quickSlotHandler = quick_slot_handler;

            quickSlotHandler.OnQuickSlotUpdated += RefreshQuickSlotUI;

            view.Initialize(model, quick_slot_handler, input_handler);
        }

        public void UpdateInventory()
        {
            view.UpdateInventoryUI();
        }

        private void OnDisable()
        {
            if (quickSlotHandler != null)
            {
                quickSlotHandler.OnQuickSlotUpdated -= RefreshQuickSlotUI;
            }

            view.RemoveEventAllToModel();
        }

        private void RefreshQuickSlotUI()
        {
            view.RefreshQuickSlotUI();
        }
    }
}