using Blue.Inventory;
using Blue.Item;
using Blue.Recipe;
using UnityEngine;

namespace Blue.UI.Garage.CraftTable
{
    public class CraftTableModel
    {
        private InventoryModel storageInventory;
        private InventoryModel playerInventory;

        public CraftTableModel(InventoryModel storage_inventory, InventoryModel player_inventory)
        {
            storageInventory = storage_inventory;
            playerInventory = player_inventory;
        }

        public void CraftItem(RecipeData recipe)
        {
            if (!HasAllRequiredResources(recipe))
            {
                Debug.Log("材料が不足しているため、アイテムを作成できません");
                return;
            }

            ConsumeResources(recipe);

            storageInventory.AddItem(recipe.ResultItem, recipe.ResultCount);
            Debug.Log($"{recipe.ResultItem.Name}を{recipe.ResultCount}個作成");
        }

        private bool HasAllRequiredResources(RecipeData recipe)
        {
            foreach(RequireItemData require in recipe.RequireResources)
            {
                if(!CheckEnoughResource(require.Item, require.Count)) return false;
            }
            return true;
        }

        private void ConsumeResources(RecipeData recipe)
        {
            // 倉庫のインベントリ→プレイヤーのインベントリの順でアイテムを検索し、削除する
            // 片方のインベントリからだけでは足りない場合、不足分をもう片方のインベントリから探す
            foreach(RequireItemData require in recipe.RequireResources)
            {
                int remaining_count = require.Count;

                // まず倉庫から消費
                if (storageInventory.TryGetItem(require.Item, out InventoryItem storage_item))
                {
                    int consume_from_storage = Mathf.Min(storage_item.Quantity, remaining_count);
                    storageInventory.RemoveItem(require.Item, consume_from_storage);
                    remaining_count -= consume_from_storage;
                }

                // 倉庫で足りない分はプレイヤーインベントリから消費
                if (remaining_count > 0)
                {
                    playerInventory.RemoveItem(require.Item, remaining_count);
                }
            }
        }

        public bool CheckEnoughResource(ItemData item, int count)
        {
            // 倉庫とプレイヤーインベントリの合計数をチェック
            int storage_count = 0;
            int player_count = 0;

            if (storageInventory.TryGetItem(item, out InventoryItem storage_item))
            {
                storage_count = storage_item.Quantity;
            }

            if (playerInventory.TryGetItem(item, out InventoryItem player_item))
            {
                player_count = player_item.Quantity;
            }

            return (storage_count + player_count) >= count;
        }
    }
}