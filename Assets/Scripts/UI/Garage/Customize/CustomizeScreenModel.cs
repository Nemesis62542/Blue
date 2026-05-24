using System;
using System.Collections.Generic;
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
                UpgradeType.SubCapacity => upgradeSaveData.subCapacityLevel,
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
                case UpgradeType.SubCapacity:
                    upgradeSaveData.subCapacityLevel++;
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

        #region サブアップグレード関連

        /// <summary>
        /// 現在のサブアップグレード最大容量を取得
        /// </summary>
        public int GetMaxSubUpgradeCapacity(UpgradeData capacityUpgrade)
        {
            if (capacityUpgrade == null) return 1;
            return capacityUpgrade.GetEffectValue(upgradeSaveData.subCapacityLevel);
        }

        /// <summary>
        /// 現在装備中のサブアップグレードの合計容量コストを取得
        /// </summary>
        public int GetCurrentCapacityUsed(List<SubUpgradeData> allSubUpgrades)
        {
            int total = 0;
            foreach (SubUpgradeItemSaveData sub in upgradeSaveData.subUpgrades)
            {
                if (sub.isEquipped)
                {
                    SubUpgradeData data = FindSubUpgradeData(sub.subUpgradeId, allSubUpgrades);
                    if (data != null)
                    {
                        total += data.CapacityCost;
                    }
                }
            }
            return total;
        }

        /// <summary>
        /// サブアップグレードを解放可能か判定
        /// </summary>
        public bool CanUnlockSubUpgrade(SubUpgradeData subUpgrade)
        {
            if (subUpgrade == null) return false;
            if (IsSubUpgradeUnlocked(subUpgrade)) return false;
            return HasAllRequiredResourcesForSubUpgrade(subUpgrade.RequiredResources);
        }

        /// <summary>
        /// サブアップグレードを解放
        /// </summary>
        public void UnlockSubUpgrade(SubUpgradeData subUpgrade)
        {
            if (!CanUnlockSubUpgrade(subUpgrade)) return;

            ConsumeResourcesForSubUpgrade(subUpgrade.RequiredResources);

            string id = subUpgrade.name;
            SubUpgradeItemSaveData existing = upgradeSaveData.subUpgrades.Find(s => s.subUpgradeId == id);
            if (existing != null)
            {
                existing.isUnlocked = true;
            }
            else
            {
                upgradeSaveData.subUpgrades.Add(new SubUpgradeItemSaveData(id, true, false));
            }

            OnUpgradeApplied?.Invoke();
        }

        /// <summary>
        /// サブアップグレードが解放済みか
        /// </summary>
        public bool IsSubUpgradeUnlocked(SubUpgradeData subUpgrade)
        {
            if (subUpgrade == null) return false;
            string id = subUpgrade.name;
            SubUpgradeItemSaveData data = upgradeSaveData.subUpgrades.Find(s => s.subUpgradeId == id);
            return data?.isUnlocked ?? false;
        }

        /// <summary>
        /// サブアップグレードを装備可能か判定
        /// </summary>
        public bool CanEquipSubUpgrade(SubUpgradeData subUpgrade, UpgradeData capacityUpgrade, List<SubUpgradeData> allSubUpgrades)
        {
            if (subUpgrade == null) return false;
            if (!IsSubUpgradeUnlocked(subUpgrade)) return false;
            if (IsSubUpgradeEquipped(subUpgrade)) return false;

            int maxCapacity = GetMaxSubUpgradeCapacity(capacityUpgrade);
            int currentUsed = GetCurrentCapacityUsed(allSubUpgrades);
            return (currentUsed + subUpgrade.CapacityCost) <= maxCapacity;
        }

        /// <summary>
        /// サブアップグレードを装備/解除
        /// </summary>
        public void ToggleSubUpgradeEquip(SubUpgradeData subUpgrade, UpgradeData capacityUpgrade, List<SubUpgradeData> allSubUpgrades)
        {
            if (subUpgrade == null) return;

            string id = subUpgrade.name;
            SubUpgradeItemSaveData data = upgradeSaveData.subUpgrades.Find(s => s.subUpgradeId == id);
            if (data == null || !data.isUnlocked) return;

            if (data.isEquipped)
            {
                data.isEquipped = false;
            }
            else if (CanEquipSubUpgrade(subUpgrade, capacityUpgrade, allSubUpgrades))
            {
                data.isEquipped = true;
            }

            OnUpgradeApplied?.Invoke();
        }

        /// <summary>
        /// サブアップグレードが装備中か
        /// </summary>
        public bool IsSubUpgradeEquipped(SubUpgradeData subUpgrade)
        {
            if (subUpgrade == null) return false;
            string id = subUpgrade.name;
            SubUpgradeItemSaveData data = upgradeSaveData.subUpgrades.Find(s => s.subUpgradeId == id);
            return data?.isEquipped ?? false;
        }

        /// <summary>
        /// 装備中のサブアップグレードのリストを取得
        /// </summary>
        public List<SubUpgradeData> GetEquippedSubUpgrades(List<SubUpgradeData> allSubUpgrades)
        {
            List<SubUpgradeData> equipped = new List<SubUpgradeData>();
            foreach (SubUpgradeItemSaveData sub in upgradeSaveData.subUpgrades)
            {
                if (sub.isEquipped)
                {
                    SubUpgradeData data = FindSubUpgradeData(sub.subUpgradeId, allSubUpgrades);
                    if (data != null)
                    {
                        equipped.Add(data);
                    }
                }
            }
            return equipped;
        }

        private SubUpgradeData FindSubUpgradeData(string id, List<SubUpgradeData> allSubUpgrades)
        {
            if (allSubUpgrades == null) return null;
            return allSubUpgrades.Find(s => s.name == id);
        }

        private bool HasAllRequiredResourcesForSubUpgrade(List<RequireItemData> requires)
        {
            if (requires == null) return true;
            foreach (RequireItemData require in requires)
            {
                if (!CheckEnoughResource(require.Item, require.Count)) return false;
            }
            return true;
        }

        private void ConsumeResourcesForSubUpgrade(List<RequireItemData> requires)
        {
            if (requires == null) return;
            foreach (RequireItemData require in requires)
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

        #endregion
    }
}
