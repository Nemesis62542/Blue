using System;
using Blue.Inventory;
using Blue.Item;
using Blue.Recipe;
using Blue.Save;
using Blue.Upgrade;
using UnityEngine;

namespace Blue.UI.Garage.Customize
{
    public class CustomizeScreenModel
    {
        private InventoryModel storageInventory;
        private InventoryModel playerInventory;
        private UpgradeSaveData upgradeSaveData;

        public int OxygenLevel => upgradeSaveData.oxygenLevel;
        public int DepthLevel => upgradeSaveData.depthLevel;

        public event Action OnUpgradeApplied;

        public CustomizeScreenModel(
            InventoryModel storageInventory,
            InventoryModel playerInventory,
            UpgradeSaveData upgradeSaveData)
        {
            this.storageInventory = storageInventory;
            this.playerInventory = playerInventory;
            this.upgradeSaveData = upgradeSaveData;
        }

        public UpgradeSaveData GetUpgradeSaveData()
        {
            return upgradeSaveData;
        }

        public bool CanUpgrade(UpgradeData upgradeData)
        {
            int currentLevel = GetCurrentLevel(upgradeData.UpgradeType);
            if (currentLevel >= upgradeData.MaxLevel) return false;

            UpgradeLevelData nextLevel = upgradeData.GetLevelData(currentLevel);
            return HasAllRequiredResources(nextLevel);
        }

        public void ApplyUpgrade(UpgradeData upgradeData)
        {
            int currentLevel = GetCurrentLevel(upgradeData.UpgradeType);
            if (currentLevel >= upgradeData.MaxLevel) return;

            UpgradeLevelData levelData = upgradeData.GetLevelData(currentLevel);
            if (!HasAllRequiredResources(levelData)) return;

            ConsumeResources(levelData);
            IncrementLevel(upgradeData.UpgradeType);
            OnUpgradeApplied?.Invoke();
        }

        public int GetCurrentLevel(UpgradeType type)
        {
            return type switch
            {
                UpgradeType.Oxygen => upgradeSaveData.oxygenLevel,
                UpgradeType.Depth => upgradeSaveData.depthLevel,
                _ => 0
            };
        }

        public bool IsMaxLevel(UpgradeData upgradeData)
        {
            int currentLevel = GetCurrentLevel(upgradeData.UpgradeType);
            return currentLevel >= upgradeData.MaxLevel;
        }

        private void IncrementLevel(UpgradeType type)
        {
            switch (type)
            {
                case UpgradeType.Oxygen:
                    upgradeSaveData.oxygenLevel++;
                    break;
                case UpgradeType.Depth:
                    upgradeSaveData.depthLevel++;
                    break;
            }
        }

        public bool HasAllRequiredResources(UpgradeLevelData levelData)
        {
            if (levelData == null || levelData.RequiredResources == null) return false;

            foreach (RequireItemData require in levelData.RequiredResources)
            {
                if (!CheckEnoughResource(require.Item, require.Count)) return false;
            }
            return true;
        }

        private void ConsumeResources(UpgradeLevelData levelData)
        {
            foreach (RequireItemData require in levelData.RequiredResources)
            {
                int remainingCount = require.Count;

                if (storageInventory.TryGetItem(require.Item, out InventoryItem storageItem))
                {
                    int consumeFromStorage = Mathf.Min(storageItem.Quantity, remainingCount);
                    storageInventory.RemoveItem(require.Item, consumeFromStorage);
                    remainingCount -= consumeFromStorage;
                }

                if (remainingCount > 0)
                {
                    playerInventory.RemoveItem(require.Item, remainingCount);
                }
            }
        }

        public bool CheckEnoughResource(ItemData item, int count)
        {
            return GetItemCount(item) >= count;
        }

        public int GetItemCount(ItemData item)
        {
            int storageCount = 0;
            int playerCount = 0;

            if (storageInventory.TryGetItem(item, out InventoryItem storageItem))
            {
                storageCount = storageItem.Quantity;
            }

            if (playerInventory.TryGetItem(item, out InventoryItem playerItem))
            {
                playerCount = playerItem.Quantity;
            }

            return storageCount + playerCount;
        }
    }
}
