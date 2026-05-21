using System.Collections.Generic;
using Blue.Recipe;
using UnityEngine;

namespace Blue.Upgrade
{
    public enum UpgradeType
    {
        Oxygen,     // 酸素容量
        Depth       // 最大深度
    }

    [CreateAssetMenu(fileName = "UpgradeData", menuName = "Blue/ScriptableObject/UpgradeData")]
    public class UpgradeData : ScriptableObject
    {
        [SerializeField] private string upgradeName;
        [SerializeField] private string description;
        [SerializeField] private Sprite icon;
        [SerializeField] private UpgradeType upgradeType;
        [SerializeField] private int baseValue;
        [SerializeField] private List<UpgradeLevelData> levels;

        public string UpgradeName => upgradeName;
        public string Description => description;
        public Sprite Icon => icon;
        public UpgradeType UpgradeType => upgradeType;
        public int BaseValue => baseValue;
        public List<UpgradeLevelData> Levels => levels;
        public int MaxLevel => levels.Count;

        public UpgradeLevelData GetLevelData(int level)
        {
            if (level < 0 || level >= levels.Count) return null;
            return levels[level];
        }

        /// <summary>
        /// 指定レベルでの効果値を取得（レベル0は初期値）
        /// </summary>
        public int GetEffectValue(int level)
        {
            if (level <= 0) return baseValue;
            if (level > levels.Count) level = levels.Count;
            return levels[level - 1].EffectValue;
        }
    }

    [System.Serializable]
    public class UpgradeLevelData
    {
        [SerializeField] private int effectValue;
        [SerializeField] private List<RequireItemData> requiredResources;

        public int EffectValue => effectValue;
        public List<RequireItemData> RequiredResources => requiredResources;
    }
}
