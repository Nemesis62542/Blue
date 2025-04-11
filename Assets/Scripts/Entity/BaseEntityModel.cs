using NFPS.Attack;
using NFPS.Interface;

namespace NFPS.Entity
{
    public abstract class BaseEntityModel : ILivingEntity
    {
        public Status Status { get; private set; }
        public bool IsDead => Status.IsDead;

        public virtual void Initialize(EntityData data)
        {
            Status = new Status(data);
        }

        public virtual void Damage(AttackData attack_data)
        {
            Status.Damage(attack_data.Power);
        }

        public virtual void OnDead()
        {
        }
    }
}