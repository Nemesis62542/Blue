using System;
using System.Collections.Generic;
using Blue.Item;
using Blue.Inventory;
using Blue.UI.DragAndDrop;

namespace Blue.UI.QuickSlot
{
    public class QuickSlotModel : IItemContainer
    {
        private List<QuickSlotItem> quickSlots = new List<QuickSlotItem>(new QuickSlotItem[4]);
        private int currentSlotIndex = 0;

        public IReadOnlyList<QuickSlotItem> QuickSlots => quickSlots.AsReadOnly();
        public ItemData CurrentEquippedItem => GetItem(currentSlotIndex);
        public QuickSlotItem CurrentQuickSlotItem => GetQuickSlotItem(currentSlotIndex);
        public int CurrentSlotIndex => currentSlotIndex;

        public event Action<int, ItemData> OnQuickSlotChanged;
        public event Action OnQuickSlotUpdated;

        // IItemContainer実装
        public bool AddItem(ItemData item_data, int quantity)
        {
            if (item_data == null || quantity <= 0) return false;

            // 空いているスロットを探して追加
            for (int i = 0; i < quickSlots.Count; i++)
            {
                if (quickSlots[i] == null)
                {
                    quickSlots[i] = new QuickSlotItem(item_data, quantity);
                    OnQuickSlotChanged?.Invoke(i, item_data);
                    OnQuickSlotUpdated?.Invoke();
                    return true;
                }
            }

            // 空きスロットがない場合は失敗
            return false;
        }

        public bool RemoveItem(ItemData item_data, int quantity)
        {
            if (item_data == null || quantity <= 0) return false;

            // 該当アイテムを探して削除
            for (int i = 0; i < quickSlots.Count; i++)
            {
                if (quickSlots[i]?.ItemData == item_data)
                {
                    if (quickSlots[i].Quantity >= quantity)
                    {
                        quickSlots[i].ModifyQuantity(-quantity);

                        if (quickSlots[i].Quantity <= 0)
                        {
                            quickSlots[i] = null;
                            OnQuickSlotChanged?.Invoke(i, null);
                        }

                        OnQuickSlotUpdated?.Invoke();
                        return true;
                    }
                }
            }

            return false;
        }

        public void UpdateView()
        {
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

                    slot_item.ModifyQuantity(-1);

                    if (slot_item.Quantity <= 0)
                    {
                        quickSlots[index] = null;
                        OnQuickSlotChanged?.Invoke(index, null);
                    }

                    OnQuickSlotUpdated?.Invoke();
                    break;
            }
        }

        public void SelectSlot(int index)
        {
            if (IsValidSlot(index))
            {
                currentSlotIndex = index;
                OnQuickSlotChanged?.Invoke(index, GetItem(index));
            }
        }

        public void CleanupInvalidSlots()
        {
            for (int i = 0; i < quickSlots.Count; i++)
            {
                QuickSlotItem slot_item = quickSlots[i];

                if (slot_item != null && slot_item.Quantity <= 0)
                {
                    quickSlots[i] = null;
                    OnQuickSlotChanged?.Invoke(i, null);
                    OnQuickSlotUpdated?.Invoke();
                }
            }
        }

        private bool IsValidSlot(int index)
        {
            return index >= 0 && index < quickSlots.Count;
        }

        private void ApplyConsumableEffect(ItemData item)
        {
            UnityEngine.Debug.Log($"使用: {item.Name} ({item.GetAttributeValue(ItemAttribute.HealingValue)} HP 回復)");
        }
    }
}