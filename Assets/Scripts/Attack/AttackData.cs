using Blue.Interface;
using UnityEngine;

namespace Blue.Attack
{
    public enum AttackType
    {
        Melee,
        Ranged,
        Magic,
        Explosion
    }

    public class AttackData
    {
        public MonoBehaviour Attacker { get; private set; }
        public IAttackable Damager { get; private set; }
        public int Power { get; private set; }
        public AttackType Type { get; private set; }
        public Vector3 HitPosition { get; private set; }

        public AttackData(MonoBehaviour attacker, IAttackable damager, int power, AttackType type, Vector3 hit_position)
        {
            Attacker = attacker;
            Damager = damager;
            Power = power;
            Type = type;
            HitPosition = hit_position;
        }
    }
}