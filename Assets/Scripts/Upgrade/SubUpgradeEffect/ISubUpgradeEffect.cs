using Blue.Player;

namespace Blue.Upgrade
{
    /// <summary>
    /// サブアップグレード効果のインターフェース
    /// </summary>
    public interface ISubUpgradeEffect
    {
        SubUpgradeType Type { get; }
        void Apply(PlayerController player);
        void Remove(PlayerController player);
    }
}
