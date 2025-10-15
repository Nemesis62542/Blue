using UnityEngine;
using System.Collections.Generic;
using Blue.Item;
using Blue.Input;
using Blue.UI.Common;
using Blue.UI.Inventory;
using Blue.UI.DragAndDrop;

namespace Blue.Inventory
{
    public class InventoryView : MonoBehaviour
    {
        [SerializeField] private Transform itemSlotParent;
        [SerializeField] private ItemSlot itemSlotPrefab;
        [SerializeField] private UISelectableNavigator navigator;
        [SerializeField] private InventoryItemSelectHandler itemSelectHandler;

        private List<ItemSlot> itemSlots = new List<ItemSlot>();
        private InventoryModel model;
        private IItemContainer container;
        private Queue<ItemSlot> itemSlotPool = new Queue<ItemSlot>();

        public void Initialize(InventoryModel model, PlayerInputHandler input_handler, IItemContainer container)
        {
            this.model = model;
            this.container = container;
            itemSelectHandler.SetupInput(input_handler);
        }

        public void UpdateInventoryUI()
        {
            if (model == null) return;

            ReleaseAllItemSlots();

            foreach (KeyValuePair<ItemData, int> item in model.GetAllItems())
            {
                AddItemToUI(item.Key, item.Value);
            }
            navigator.InitializeSelection();
        }

        private void AddItemToUI(ItemData item_data, int count)
        {
            ItemSlot new_item_slot = GetOrCreateItemSlot();
            new_item_slot.SetItem(item_data, count);
            if (new_item_slot.HoverArea.TryGetComponent(out ItemSlotDragHandler drag_handler))
            {
                drag_handler.Initialize(container);
            }
        }

        private ItemSlot GetOrCreateItemSlot()
        {
            ItemSlot slot;
            if (itemSlotPool.Count > 0)
            {
                slot = itemSlotPool.Dequeue();
                slot.gameObject.SetActive(true);
                return slot;
            }
            slot = Instantiate(itemSlotPrefab, itemSlotParent);
            itemSlots.Add(slot);
            return slot;
        }

        private void ReleaseAllItemSlots()
        {
            foreach (ItemSlot child in itemSlots)
            {
                child.gameObject.SetActive(false);
                itemSlotPool.Enqueue(child);
            }
        }
    }
}