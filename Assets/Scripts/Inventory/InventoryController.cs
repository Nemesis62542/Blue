using Blue.Input;
using Blue.Item;
using Blue.UI.DragAndDrop;
using UnityEngine;

namespace Blue.Inventory
{
    public class InventoryController : MonoBehaviour, IItemContainer
    {
        [SerializeField] private InventoryView view;

        private InventoryModel model;

        public void Initialize(InventoryModel model, PlayerInputHandler input_handler)
        {
            this.model = model;
            this.model.OnValueChanged += RefreshInventoryUI;
            view?.Initialize(this.model, input_handler, this);
        }

        public void RefreshInventoryUI()
        {
            view.UpdateInventoryUI();
        }

        public bool RemoveItem(ItemData item_data, int quantity)
        {
            if (model.TryGetItem(item_data, out InventoryItem item) && item.Quantity >= quantity)
            {
                model.RemoveItem(item_data, quantity);
                return true;
            }
            return false;
        }

        public bool AddItem(ItemData item_data, int quantity)
        {
            model.AddItem(item_data, quantity);
            return true;
        }

        public void UpdateView()
        {
            RefreshInventoryUI();
        }
    }
}