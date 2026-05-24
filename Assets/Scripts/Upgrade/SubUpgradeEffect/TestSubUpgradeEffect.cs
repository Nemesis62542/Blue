using Blue.Player;
using UnityEngine;

namespace Blue.Upgrade
{
    /// <summary>
    /// テスト用サブアップグレード効果
    /// 実際の効果は持たず、Debug.Log出力のみ
    /// </summary>
    public class TestSubUpgradeEffect : ISubUpgradeEffect
    {
        public SubUpgradeType Type => SubUpgradeType.Test;

        public void Apply(PlayerController player)
        {
            Debug.Log("テストサブアップグレード効果を適用しました");
        }

        public void Remove(PlayerController player)
        {
            Debug.Log("テストサブアップグレード効果を解除しました");
        }
    }
}
