using System;
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

        public event Action<float, float> OnHPChanged;

        public Status(EntityData data)
        {
            Name = data.Name;
            MaxHp = data.HP;
            AttackPower = data.AttackPower;
            Size = data.DisplaySize;
            HP = MaxHp;
        }

        public void Damage(int power)
        {
            HP = Mathf.Max(0, HP - power);
            OnHPChanged?.Invoke(HP, MaxHp);
        }

        public bool IsDead => HP <= 0;
    }
}