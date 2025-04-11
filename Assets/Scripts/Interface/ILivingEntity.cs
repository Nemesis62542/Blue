using NFPS.Attack;
using NFPS.Entity;

namespace NFPS.Interface
{
    public interface ILivingEntity
    {
        Status Status { get; }
        void Damage(AttackData attackData);
        void OnDead();
    }
}