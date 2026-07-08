using Blue.Attack;
using Blue.Entity;

namespace Blue.Interface
{
    public interface ILivingEntity
    {
        Status Status { get; }
        void Damage(AttackData attackData);
        void OnDead();

        // Threat size properties
        float Size { get; }                    // Threat size of this entity
        float ThreatSizeThreshold { get; }     // Threats are entities larger than this (-1 = fears nothing)
    }
}