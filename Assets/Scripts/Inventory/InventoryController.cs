using Blue.Input;
using UnityEngine;

namespace Blue.Inventory
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

            quickSlotHandler.OnQuickSlotUpdated += RefreshInventoryUI;

            view.Initialize(this.model, quick_slot_handler, input_handler);
        }

        private void OnDisable()
        {
            if (quickSlotHandler != null)
            {
                quickSlotHandler.OnQuickSlotUpdated -= RefreshInventoryUI;
            }
        }

        public void RefreshInventoryUI()
        {
            view.UpdateInventoryUI();
        }
    }
}