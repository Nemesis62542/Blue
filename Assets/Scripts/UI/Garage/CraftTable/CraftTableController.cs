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
        private InventoryModel playerInventoryModel;

        public void Initialize()
        {
            // セーブデータから倉庫とプレイヤーインベントリを読み込み
            storageInventoryModel = SaveDataConverter.LoadStorageInventory();
            playerInventoryModel = SaveDataConverter.LoadPlayerInventory();

            model = new CraftTableModel(storageInventoryModel, playerInventoryModel);

            // インベントリ変更時に自動保存
            storageInventoryModel.OnValueChanged += OnStorageInventoryChanged;
            playerInventoryModel.OnValueChanged += OnPlayerInventoryChanged;

            view.Initialize(recipes, model, ConfirmCraftItem);
        }

        private void OnDestroy()
        {
            // イベント解除
            if (storageInventoryModel != null)
            {
                storageInventoryModel.OnValueChanged -= OnStorageInventoryChanged;
            }
            if (playerInventoryModel != null)
            {
                playerInventoryModel.OnValueChanged -= OnPlayerInventoryChanged;
            }
        }

        private void OnStorageInventoryChanged()
        {
            SaveDataConverter.SaveStorageInventory(storageInventoryModel);
        }

        private void OnPlayerInventoryChanged()
        {
            SaveDataConverter.SavePlayerInventory(playerInventoryModel);
        }

        public void ConfirmCraftItem(RecipeData recipe)
        {
            model.CraftItem(recipe);
            view.RefreshDisplay();
        }
    }
}