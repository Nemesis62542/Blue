using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using Blue.Item;
using System;

namespace Blue.Inventory
{
    public class InventoryModel
    {
        private List<InventoryItem> inventoryItems = new List<InventoryItem>();

        public ReadOnlyCollection<InventoryItem> InventoryItems => inventoryItems.AsReadOnly();
        public Action<ItemData> OnPickUpItem { private get; set; }

        public void AddItem(ItemData item_data, int quantity = 1)
        {
            if (TryGetItem(item_data, out InventoryItem existing_item))
            {
                existing_item.ModifyQuantity(quantity);
            }
            else
            {
                InventoryItem new_item = new InventoryItem(item_data);
                new_item.ModifyQuantity(quantity);
                inventoryItems.Add(new_item);
            }

            OnPickUpItem?.Invoke(item_data);
        }

        public void RemoveItem(ItemData item_data, int quantity = 1)
        {
            InventoryItem existing_item = FindItem(item_data);

            if (existing_item != null)
            {
                existing_item.ModifyQuantity(-quantity);
                if (existing_item.Quantity <= 0)
                {
                    inventoryItems.Remove(existing_item);

                    Debug.Log($"アイテム削除: {item_data.Name}（すべて削除）");
                }
                else
                {
                    Debug.Log($"アイテムの個数減少: {item_data.Name} x{quantity}");
                }
            }
        }

        public bool TryGetItem(ItemData item_data, out InventoryItem item)
        {
            item = inventoryItems.Find(i => i.ItemData == item_data);
            return item != null;
        }

        public InventoryItem FindItem(ItemData item_data)
        {
            for (int i = 0; i < inventoryItems.Count; i++)
            {
                if (inventoryItems[i].ItemData == item_data)
                {
                    return inventoryItems[i];
                }
            }
            return null;
        }

        public void ShowInventory()
        {
            Debug.Log("インベントリの内容:");
            for (int i = 0; i < inventoryItems.Count; i++)
            {
                Debug.Log(inventoryItems[i].ToString());
            }
        }

        public Dictionary<ItemData, int> GetAllItems()
        {
            Dictionary<ItemData, int> item_dictionary = new Dictionary<ItemData, int>();

            foreach (InventoryItem item in inventoryItems)
            {
                item_dictionary[item.ItemData] = item.Quantity;
            }

            return item_dictionary;
        }
    }
}