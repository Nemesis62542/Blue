using NFPS.Attack;

namespace NFPS.Interface
{
    public interface IAttackable
    {
        void Damage(AttackData attackData);
    }
}