using UnityEngine;
using System.Collections.Generic;
using Blue.Item;
using Blue.Input;
using Blue.UI.Common;
using Blue.UI.Inventory;
using Blue.UI.QuickSlot;

namespace Blue.Inventory
{
    public class InventoryView : MonoBehaviour
    {
        [SerializeField] private Transform itemSlotParent;
        [SerializeField] private ItemSlot itemSlotPrefab;
        [SerializeField] private Transform quickSlotParent;
        [SerializeField] private ItemSlot quickSlotPrefab;
        [SerializeField] private UISelectableNavigator navigator;
        [SerializeField] private InventoryItemSelectHandler itemSelectHandler;
        [SerializeField] private QuickSlotSelectHandler quickSlotSelectHandler;

        private Dictionary<ItemData, ItemSlot> itemSlots = new Dictionary<ItemData, ItemSlot>();
        private List<ItemSlot> quickSlotSlots = new List<ItemSlot>();
        private QuickSlotHandler quickSlotHandler;
        private InventoryModel model;

        public void Initialize(InventoryModel model, QuickSlotHandler quick_slot_handler, PlayerInputHandler input_handler)
        {
            this.model = model; 
            quickSlotHandler = quick_slot_handler;

            quickSlotHandler.OnQuickSlotChanged += UpdateQuickSlotUI;
            quickSlotHandler.OnQuickSlotUpdated += RefreshQuickSlotUI;

            quickSlotSelectHandler.SetQuickSlotHandler(quickSlotHandler);
            itemSelectHandler.SetupInput(input_handler);
            quickSlotSelectHandler.SetupInput(input_handler);
        }

        public void RemoveEventAllToModel()
        {
            if (quickSlotHandler == null) return;

            quickSlotHandler.OnQuickSlotChanged -= UpdateQuickSlotUI;
            quickSlotHandler.OnQuickSlotUpdated -= RefreshQuickSlotUI;
        }

        public void UpdateInventoryUI()
        {
            if (model == null) return;

            foreach (Transform child in itemSlotParent)
            {
                Destroy(child.gameObject);
            }
            itemSlots.Clear();

            foreach (KeyValuePair<ItemData, int> item in model.GetAllItems())
            {
                AddItemToUI(item.Key, item.Value);
            }
            navigator.InitializeSelection();
            RefreshQuickSlotUI();
        }

        private void AddItemToUI(ItemData item_data, int count)
        {
            ItemSlot new_item_slot = Instantiate(itemSlotPrefab, itemSlotParent);
            new_item_slot.SetItem(item_data, count);
            if (new_item_slot.HoverArea.TryGetComponent(out ItemSlotDragHandler drag_handler))
            {
                drag_handler.Initialize(item_data);
            }
            itemSlots[item_data] = new_item_slot;
        }

        public void UpdateQuickSlotUI(int slot_index, ItemData item)
        {
            RefreshQuickSlotUI();
        }

        public void RefreshQuickSlotUI()
        {
            if (quickSlotHandler == null) return;

            foreach (Transform child in quickSlotParent)
            {
                Destroy(child.gameObject);
            }
            quickSlotSlots.Clear();

            for (int i = 0; i < quickSlotHandler.QuickSlots.Count; i++)
            {
                ItemData item_data = quickSlotHandler.GetItem(i);
                ItemSlot quick_slot = Instantiate(quickSlotPrefab, quickSlotParent);
                quick_slot.SetItem(item_data, 1);

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
    }
}