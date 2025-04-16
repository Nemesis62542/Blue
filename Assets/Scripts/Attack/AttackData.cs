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
        public int Power { get; private set; }
        public AttackType Type { get; private set; }
        public Vector3 HitPosition { get; private set; }

        public AttackData(MonoBehaviour attacker, int power, AttackType type, Vector3 hit_position)
        {
            Attacker = attacker;
            Power = power;
            Type = type;
            HitPosition = hit_position;
        }
    }
}