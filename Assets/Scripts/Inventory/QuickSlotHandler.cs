using System;
using System.Collections.Generic;
using Blue.Item;
using UnityEngine;

namespace Blue.Inventory
{
    public class QuickSlotHandler
    {
        private List<QuickSlotItem> quickSlots = new List<QuickSlotItem>(new QuickSlotItem[4]);
        private InventoryModel inventory;
        private int currentSlotIndex = 0;

        public IReadOnlyList<QuickSlotItem> QuickSlots => quickSlots.AsReadOnly();
        public ItemData CurrentEquippedItem => GetItem(currentSlotIndex);
        public QuickSlotItem CurrentQuickSlotItem => GetQuickSlotItem(currentSlotIndex);
        public int CurrentSlotIndex => currentSlotIndex;

        public event Action<int, ItemData> OnQuickSlotChanged;
        public event Action OnQuickSlotUpdated;

        public QuickSlotHandler(InventoryModel inventory)
        {
            this.inventory = inventory;
        }

        public void Register(int index, ItemData item, int quantity = 1)
        {
            if (!IsValidSlot(index)) return;

            // 既に登録されているアイテムがあれば、インベントリに戻す
            if (quickSlots[index] != null)
            {
                inventory.AddItem(quickSlots[index].ItemData, quickSlots[index].Quantity);
            }

            // インベントリから指定個数を取り出す
            if (inventory.TryGetItem(item, out InventoryItem inventoryItem))
            {
                int actualQuantity = Mathf.Min(quantity, inventoryItem.Quantity);

                inventory.RemoveItem(item, actualQuantity);
                quickSlots[index] = new QuickSlotItem(item, actualQuantity);

                if (index == currentSlotIndex) OnQuickSlotChanged?.Invoke(index, item);
                OnQuickSlotUpdated?.Invoke();
            }
        }

        public void Unregister(int index)
        {
            if (!IsValidSlot(index) || quickSlots[index] == null) return;

            // クイックスロットからインベントリに戻す
            inventory.AddItem(quickSlots[index].ItemData, quickSlots[index].Quantity);
            quickSlots[index] = null;

            OnQuickSlotChanged?.Invoke(index, null);
            OnQuickSlotUpdated?.Invoke();
        }

        public ItemData GetItem(int index)
        {
            if (IsValidSlot(index) && quickSlots[index] != null)
            {
                return quickSlots[index].ItemData;
            }
            return null;
        }

        public QuickSlotItem GetQuickSlotItem(int index)
        {
            if (IsValidSlot(index))
            {
                return quickSlots[index];
            }
            return null;
        }

        public void Use(int index)
        {
            QuickSlotItem slot_item = GetQuickSlotItem(index);
            if (slot_item == null || slot_item.ItemData == null) return;

            currentSlotIndex = index;

            switch (slot_item.ItemData.Type)
            {
                case ItemType.Consumable:
                    ApplyConsumableEffect(slot_item.ItemData);

                    // クイックスロットから1個消費
                    slot_item.ModifyQuantity(-1);

                    // 個数が0になったらスロットをクリア
                    if (slot_item.Quantity <= 0)
                    {
                        quickSlots[index] = null;
                        OnQuickSlotChanged?.Invoke(index, null);
                    }

                    OnQuickSlotUpdated?.Invoke();
                    break;
            }
        }

        private bool IsValidSlot(int index)
        {
            return index >= 0 && index < quickSlots.Count;
        }

        public void CleanupInvalidSlots()
        {
            for (int i = 0; i < quickSlots.Count; i++)
            {
                QuickSlotItem slot_item = quickSlots[i];

                // 個数が0以下になったらクリア
                if (slot_item != null && slot_item.Quantity <= 0)
                {
                    quickSlots[i] = null;
                    OnQuickSlotChanged?.Invoke(i, null);
                    OnQuickSlotUpdated?.Invoke();
                }
            }
        }

        public void SelectSlot(int index)
        {
            if (IsValidSlot(index))
            {
                currentSlotIndex = index;
                Debug.Log($"クイックスロット{index}を選択");
                OnQuickSlotChanged?.Invoke(index, GetItem(index));
            }
        }

        private void ApplyConsumableEffect(ItemData item)
        {
            Debug.Log($"使用: {item.Name} ({item.GetAttributeValue(ItemAttribute.HealingValue)} HP 回復)");
        }

    }
}