using UnityEngine;
using System.Collections.Generic;
using Blue.Item;
using Blue.Inventory;
using Blue.UI.Inventory;

namespace Blue.UI.QuickSlot
{
    public class QuickSlotView : MonoBehaviour
    {
        [SerializeField] private Transform quickSlotParent;
        [SerializeField] private ItemSlot quickSlotPrefab;

        private List<ItemSlot> quickSlotSlots = new List<ItemSlot>();
        private Queue<ItemSlot> quickSlotPool = new Queue<ItemSlot>();
        private QuickSlotHandler quickSlotHandler;

        public void Initialize(QuickSlotHandler quickSlotHandler)
        {
            this.quickSlotHandler = quickSlotHandler;
        }

        public void RefreshQuickSlotUI()
        {
            if (quickSlotHandler == null) return;

            ReleaseAllQuickSlots();

            int slot_count = quickSlotHandler.QuickSlots.Count;
            for (int i = 0; i < slot_count; i++)
            {
                ItemData item_data = quickSlotHandler.GetItem(i);
                ItemSlot quick_slot = GetOrCreateQuickSlot();

                int quantity = 0;
                if (item_data != null)
                {
                    InventoryItem inventory_item = quickSlotHandler.GetInventoryItem(i);
                    quantity = inventory_item?.Quantity ?? 0;
                }
                quick_slot.SetItem(item_data, quantity);

                if (quick_slot.HoverArea.TryGetComponent(out QuickSlotDropHandler drop_handler))
                {
                    drop_handler.Setup(quickSlotHandler, i);
                }

                if (quick_slot.HoverArea.TryGetComponent(out QuickSlotClickHandler click_handler))
                {
                    click_handler.Setup(quickSlotHandler, i);
                }

                quickSlotSlots.Add(quick_slot);
            }
        }

        private ItemSlot GetOrCreateQuickSlot()
        {
            if (quickSlotPool.Count > 0)
            {
                ItemSlot slot = quickSlotPool.Dequeue();
                slot.gameObject.SetActive(true);
                return slot;
            }
            return Instantiate(quickSlotPrefab, quickSlotParent);
        }

        private void ReleaseAllQuickSlots()
        {
            foreach (Transform child in quickSlotParent)
            {
                child.gameObject.SetActive(false);
                quickSlotPool.Enqueue(child.GetComponent<ItemSlot>());
            }
            quickSlotSlots.Clear();
        }
    }
}
