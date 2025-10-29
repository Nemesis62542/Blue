using System.Collections.Generic;
using Blue.Inventory;
using Blue.Recipe;
using Blue.Save;
using UnityEngine;

namespace Blue.UI.Garage.CraftTable
{
    public class CraftTableController : MonoBehaviour
    {
        [SerializeField] private CraftTableView view;
        [SerializeField] private List<RecipeData> recipes;

        private CraftTableModel model;
        private InventoryModel storageInventoryModel;

        public void Initialize()
        {
            // セーブデータから倉庫インベントリを読み込み
            storageInventoryModel = SaveDataConverter.LoadStorageInventory();

            model = new CraftTableModel(storageInventoryModel);

            // インベントリ変更時に自動保存
            storageInventoryModel.OnValueChanged += OnInventoryChanged;

            view.Initialize(recipes, model, ConfirmCraftItem);
        }

        private void OnDestroy()
        {
            // イベント解除
            if (storageInventoryModel != null)
            {
                storageInventoryModel.OnValueChanged -= OnInventoryChanged;
            }
        }

        private void OnInventoryChanged()
        {
            SaveDataConverter.SaveStorageInventory(storageInventoryModel);
        }

        public void ConfirmCraftItem(RecipeData recipe)
        {
            model.CraftItem(recipe);
            view.RefreshDisplay();
        }
    }
}