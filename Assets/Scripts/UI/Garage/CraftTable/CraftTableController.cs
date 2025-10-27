using System.Collections.Generic;
using Blue.Inventory;
using Blue.Item;
using Blue.Recipe;
using UnityEngine;

namespace Blue.UI.Garage.CraftTable
{
    public class CraftTableController : MonoBehaviour
    {
        [SerializeField] private CraftTableView view;
        [SerializeField] private List<RecipeData> recipes;
        [SerializeField] private ItemData item;

        private CraftTableModel model;

        public void Initialize()
        {
            //仮の実装で一旦Newする
            InventoryModel strage_inventory = new InventoryModel();
            //デバッグ用にアイテムを追加
            strage_inventory.AddItem(item);

            model = new CraftTableModel(strage_inventory);
            
            view.Initialize(recipes, model, ConfirmCraftItem);
        }

        public void ConfirmCraftItem(RecipeData recipe)
        {
            model.CraftItem(recipe);
            view.RefreshDisplay();
        }
    }
}