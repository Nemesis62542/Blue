using Blue.Attack;
using Blue.Interface;

namespace Blue.Entity
{
    public abstract class BaseEntityModel : ILivingEntity
    {
        private EntityData data;

        public Status Status { get; private set; }
        public EntityData Data => data;
        public bool IsDead => Status.IsDead;

        public BaseEntityModel(EntityData data)
        {
            this.data = data;
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