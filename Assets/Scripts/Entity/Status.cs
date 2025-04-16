using UnityEngine;

namespace Blue.Entity
{
    public class Status
    {
        public string Name { get; private set; }
        public int MaxHp { get; private set; }
        public int AttackPower { get; private set; }

        private int hp;
        public int HP => hp;

        public Status(EntityData data)
        {
            Name = data.EntityName;
            MaxHp = data.HP;
            AttackPower = data.AttackPower;
            hp = MaxHp;
        }

        public void Damage(int power)
        {
            hp = Mathf.Max(0, hp - power);
        }

        public bool IsDead => hp <= 0;
    }
}