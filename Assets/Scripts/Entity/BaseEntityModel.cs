using Blue.Attack;
using Blue.Interface;

namespace Blue.Entity
{
    public abstract class BaseEntityModel : ILivingEntity
    {
        public Status Status { get; private set; }
        public bool IsDead => Status.IsDead;

        public BaseEntityModel(EntityData data)
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