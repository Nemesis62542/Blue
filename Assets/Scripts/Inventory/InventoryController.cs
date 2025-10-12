using Blue.Input;
using UnityEngine;

namespace Blue.Inventory
{
    public class InventoryController : MonoBehaviour
    {
        [SerializeField] private InventoryView view;

        private InventoryModel model;

        public void Initialize(InventoryModel model, PlayerInputHandler input_handler)
        {
            this.model = model;
            view.Initialize(this.model, input_handler);
        }

        public void RefreshInventoryUI()
        {
            view.UpdateInventoryUI();
        }
    }
}