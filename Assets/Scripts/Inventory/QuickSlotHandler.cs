using System;
using System.Collections.Generic;
using NFPS.Item;
using UnityEngine;

namespace NFPS.Inventory
{
    public class QuickSlotHandler
    {
        private List<ItemData> quickSlots = new List<ItemData>(new ItemData[4]);
        private InventoryModel inventory;
        private int currentSlotIndex = 0;

        public IReadOnlyList<ItemData> QuickSlots => quickSlots.AsReadOnly();
        public ItemData CurrentEquippedItem => GetItem(currentSlotIndex);
        public InventoryItem CurrentInventoryItem => GetInventoryItem(currentSlotIndex);
        public int CurrentSlotIndex => currentSlotIndex;

        public event Action<int, ItemData> OnQuickSlotItemUsed;
        public event Action OnQuickSlotUpdated;

        public QuickSlotHandler(InventoryModel inventory)
        {
            this.inventory = inventory;
        }

        public void Register(int index, ItemData item)
        {
            if (IsValidSlot(index))
            {
                quickSlots[index] = item;
                OnQuickSlotUpdated?.Invoke();
            }
        }

        public void Unregister(int index)
        {
            if (IsValidSlot(index))
            {
                quickSlots[index] = null;
                OnQuickSlotItemUsed?.Invoke(index, null);
                OnQuickSlotUpdated?.Invoke();
            }
        }

        public ItemData GetItem(int index)
        {
            if (IsValidSlot(index)) return quickSlots[index];
            return null;
        }

        public void Use(int index)
        {
            ItemData item = GetItem(index);
            if (item == null) return;

            currentSlotIndex = index;

            switch (item.Type)
            {
                case ItemType.Consumable:
                    ApplyConsumableEffect(item);
                    inventory.RemoveItem(item, 1);
                    CleanupInvalidSlots();
                    break;

                case ItemType.Weapon:
                    EquipWeapon(item);
                    break;
            }

            OnQuickSlotItemUsed?.Invoke(index, item);
        }

        public InventoryItem GetInventoryItem(int index)
        {
            ItemData item = GetItem(index);
            return item == null ? null : inventory.FindItem(item);
        }

        private bool IsValidSlot(int index)
        {
            return index >= 0 && index < quickSlots.Count;
        }

        public void CleanupInvalidSlots()
        {
            for (int i = 0; i < quickSlots.Count; i++)
            {
                ItemData item = quickSlots[i];
                if (item != null && !inventory.TryGetItem(item, out _))
                {
                    Unregister(i);
                }
            }
        }

        public void SelectSlot(int index)
        {
            if (IsValidSlot(index) && GetItem(index) != null)
            {
                currentSlotIndex = index;
                OnQuickSlotItemUsed?.Invoke(index, GetItem(index));
            }
        }

        private void ApplyConsumableEffect(ItemData item)
        {
            Debug.Log($"使用: {item.ItemName} ({item.GetAttributeValue(ItemAttribute.HealingValue)} HP 回復)");
        }

        private void EquipWeapon(ItemData item)
        {
            Debug.Log($"装備: {item.ItemName}");
        }
    }
}