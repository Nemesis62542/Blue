using System.Collections.Generic;
using Blue.Player;
using Blue.Save;
using UnityEngine;

namespace Blue.Upgrade
{
    /// <summary>
    /// サブアップグレード効果の管理クラス
    /// </summary>
    public class SubUpgradeEffectManager
    {
        private Dictionary<SubUpgradeType, ISubUpgradeEffect> effects;
        private HashSet<SubUpgradeType> activeEffects;

        public SubUpgradeEffectManager()
        {
            effects = new Dictionary<SubUpgradeType, ISubUpgradeEffect>();
            activeEffects = new HashSet<SubUpgradeType>();

            RegisterEffect(new TestSubUpgradeEffect());
        }

        private void RegisterEffect(ISubUpgradeEffect effect)
        {
            effects[effect.Type] = effect;
        }

        /// <summary>
        /// 装備中のサブアップグレード効果を適用
        /// </summary>
        public void ApplyEquippedEffects(PlayerController player, UpgradeSaveData saveData, List<SubUpgradeData> allSubUpgrades)
        {
            RemoveAllEffects(player);

            if (saveData?.subUpgrades == null) return;

            foreach (SubUpgradeItemSaveData sub in saveData.subUpgrades)
            {
                if (sub.isEquipped)
                {
                    SubUpgradeData data = FindSubUpgradeData(sub.subUpgradeId, allSubUpgrades);
                    if (data != null && effects.TryGetValue(data.SubUpgradeType, out ISubUpgradeEffect effect))
                    {
                        effect.Apply(player);
                        activeEffects.Add(data.SubUpgradeType);
                        Debug.Log($"サブアップグレード効果を適用: {data.UpgradeName}");
                    }
                }
            }
        }

        /// <summary>
        /// すべての効果を解除
        /// </summary>
        public void RemoveAllEffects(PlayerController player)
        {
            foreach (SubUpgradeType type in activeEffects)
            {
                if (effects.TryGetValue(type, out ISubUpgradeEffect effect))
                {
                    effect.Remove(player);
                }
            }
            activeEffects.Clear();
        }

        private SubUpgradeData FindSubUpgradeData(string id, List<SubUpgradeData> allSubUpgrades)
        {
            if (allSubUpgrades == null) return null;
            return allSubUpgrades.Find(s => s.name == id);
        }
    }
}
