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

        [Header("Sub Upgrade")]
        [SerializeField] private List<SubUpgradeData> subUpgrades;
        [SerializeField] private UpgradeData subCapacityUpgrade;

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

            // サブアップグレード初期化
            if (subUpgrades != null && subUpgrades.Count > 0)
            {
                view.InitializeSubUpgrades(subUpgrades, subCapacityUpgrade, UnlockSubUpgrade, ToggleSubUpgradeEquip);
            }
            else
            {
                Debug.LogWarning("subUpgrades is null or empty - SubUpgrade feature disabled");
            }
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
            view.RefreshSubUpgradeDisplay();
        }

        private void UnlockSubUpgrade(SubUpgradeData subUpgrade)
        {
            model.UnlockSubUpgrade(subUpgrade);
            SaveDataConverter.SaveUpgrades(model.GetUpgradeSaveData());
            view.RefreshSubUpgradeDisplay();
        }

        private void ToggleSubUpgradeEquip(SubUpgradeData subUpgrade)
        {
            model.ToggleSubUpgradeEquip(subUpgrade, subCapacityUpgrade, subUpgrades);
            SaveDataConverter.SaveUpgrades(model.GetUpgradeSaveData());
            view.RefreshSubUpgradeDisplay();
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
