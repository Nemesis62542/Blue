using Blue.Attack;
using Blue.Entity;

namespace Blue.Interface
{
    public interface ILivingEntity
    {
        Status Status { get; }
        void Damage(AttackData attackData);
        void OnDead();
    }
}