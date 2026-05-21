using System.Collections.Generic;
using Blue.Inventory;
using Blue.Save;
using Blue.UI.Screen;
using Blue.Upgrade;
using UnityEngine;

namespace Blue.UI.Garage.Customize
{
    public class CustomizeScreenController : MonoBehaviour, IScreenController
    {
        [SerializeField] private CustomizeScreenView view;
        [SerializeField] private List<UpgradeData> upgrades;

        private CustomizeScreenModel model;
        private InventoryModel storageInventoryModel;
        private InventoryModel playerInventoryModel;
        private UpgradeSaveData upgradeSaveData;

        public void Initialize()
        {
            // セーブデータから読み込み
            storageInventoryModel = SaveDataConverter.LoadStorageInventory();
            playerInventoryModel = SaveDataConverter.LoadPlayerInventory();
            upgradeSaveData = SaveDataConverter.LoadUpgrades();

            model = new CustomizeScreenModel(
                storageInventoryModel,
                playerInventoryModel,
                upgradeSaveData);

            // インベントリ変更時に自動保存
            storageInventoryModel.OnValueChanged += OnStorageInventoryChanged;
            playerInventoryModel.OnValueChanged += OnPlayerInventoryChanged;

            view.Initialize(upgrades, model, ConfirmUpgrade);
        }

        private void OnDestroy()
        {
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

        public void ConfirmUpgrade(UpgradeData upgrade)
        {
            model.ApplyUpgrade(upgrade);
            SaveDataConverter.SaveUpgrades(model.GetUpgradeSaveData());
            view.RefreshDisplay();
        }

        public void OnScreenEnter()
        {
            Initialize();
        }

        public void OnScreenExit()
        {
            view.ShowScreenOffPanel();
        }

        public void OnScreenChanged(ScreenState state)
        {
            if (state == ScreenState.Customize) OnScreenEnter();
            else OnScreenExit();
        }
    }
}
