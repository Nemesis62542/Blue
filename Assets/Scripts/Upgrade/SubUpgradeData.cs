using System.Collections.Generic;
using Blue.Recipe;
using UnityEngine;

namespace Blue.Upgrade
{
    /// <summary>
    /// サブアップグレードの種類
    /// </summary>
    public enum SubUpgradeType
    {
        Test    // テスト用（将来の追加用にenumで管理）
    }

    /// <summary>
    /// サブアップグレードのデータ定義
    /// メインアップグレードと異なり、レベルを持たず解放/装備の2段階で管理される
    /// </summary>
    [CreateAssetMenu(fileName = "SubUpgradeData", menuName = "Blue/ScriptableObject/SubUpgradeData")]
    public class SubUpgradeData : ScriptableObject
    {
        [SerializeField] private string upgradeName;
        [SerializeField] private string description;
        [SerializeField] private Sprite icon;
        [SerializeField] private SubUpgradeType subUpgradeType;
        [SerializeField] private List<RequireItemData> requiredResources;
        [SerializeField] private int capacityCost = 1;

        public string UpgradeName => upgradeName;
        public string Description => description;
        public Sprite Icon => icon;
        public SubUpgradeType SubUpgradeType => subUpgradeType;
        public List<RequireItemData> RequiredResources => requiredResources;
        public int CapacityCost => capacityCost;
    }
}
