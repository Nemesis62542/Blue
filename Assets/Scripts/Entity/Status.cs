using UnityEngine;

namespace Blue.Entity
{
    public class Status
    {
        public string Name { get; private set; }
        public int MaxHp { get; private set; }
        public int AttackPower { get; private set; }
        public float Size { get; private set; }
        public int HP { get; private set; }

        public Status(EntityData data)
        {
            Name = data.EntityName;
            MaxHp = data.HP;
            AttackPower = data.AttackPower;
            HP = MaxHp;
            Size = data.Size;
        }

        public void Damage(int power)
        {
            HP = Mathf.Max(0, HP - power);
        }

        public bool IsDead => HP <= 0;
    }
}