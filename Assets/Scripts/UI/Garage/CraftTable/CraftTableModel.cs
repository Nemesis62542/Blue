using Blue.Inventory;
using Blue.Item;
using Blue.Recipe;
using UnityEngine;

namespace Blue.UI.Garage.CraftTable
{
    public class CraftTableModel
    {
        private InventoryModel strageInventory;

        public CraftTableModel(InventoryModel strage_inventory)
        {
            strageInventory = strage_inventory;
        }

        public void CraftItem(RecipeData recipe)
        {
            if (!HasAllRequiredResources(recipe))
            {
                Debug.Log("材料が不足しているため、アイテムを作成できません");
                return;
            }

            ConsumeResources(recipe);

            strageInventory.AddItem(recipe.ResultItem, recipe.ResultCount);
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
            //現在、仮の実装で単一のインベントリからアイテムを探して削除している
            //今後、倉庫のインベントリ→プレイヤーのインベントリの順でアイテムを検索し、削除する方式にする予定
            //必要アイテムは足りているが片方のインベントリからだけでは足りない場合、不足分をもう片方のインベントリから探す処理も実装する
            foreach(RequireItemData require in recipe.RequireResources)
            {
                strageInventory.RemoveItem(require.Item, require.Count);
            }
        }

        public bool CheckEnoughResource(ItemData item, int count)
        {
            return  strageInventory.TryGetItem(item, out InventoryItem inventory_item) &&
                    inventory_item.Quantity >= count;
        }
    }
}