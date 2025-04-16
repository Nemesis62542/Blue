using UnityEngine;
using System.Collections.Generic;
using Blue.Item;

namespace Blue.Inventory
{
    public class InventoryItem
    {
        public ItemData ItemData { get; private set; }
        private Dictionary<ItemAttribute, int> dynamicValues;
        private int quantity;

        public int Quantity => quantity;

        public InventoryItem(ItemData item_data)
        {
            ItemData = item_data;
            dynamicValues = new Dictionary<ItemAttribute, int>();

            foreach (ItemAttribute attribute in System.Enum.GetValues(typeof(ItemAttribute)))
            {
                int value = item_data.GetAttributeValue(attribute);
                if (value != 0)
                {
                    dynamicValues[attribute] = value;
                }
            }
        }

        public int GetDynamicValue(ItemAttribute attribute)
        {
            return dynamicValues.TryGetValue(attribute, out int value) ? value : 0;
        }

        public void SetDynamicValue(ItemAttribute attribute, int new_value)
        {
            if (dynamicValues.ContainsKey(attribute))
            {
                dynamicValues[attribute] = new_value;
            }
        }

        public void ModifyQuantity(int amount)
        {
            quantity = System.Math.Max(0, quantity + amount);
        }

        public override string ToString()
        {
            System.Text.StringBuilder result = new System.Text.StringBuilder();
            result.Append($"[{ItemData.ItemName}]");

            foreach (KeyValuePair<ItemAttribute, int> kvp in dynamicValues)
            {
                result.Append($" {kvp.Key}: {kvp.Value}");
            }

            return result.ToString();
        }
    }
}