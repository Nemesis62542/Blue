using System;
using Blue.Entity;
using Blue.Inventory;
using UnityEngine;

namespace Blue.Player
{
    public class PlayerModel : BaseEntityModel
    {
        private InventoryModel inventory = new InventoryModel();
        private QuickSlotHandler quickSlotHandler;
        private int maxOxygen = 100;
        private int oxygen;
        private float depth;

        public InventoryModel Inventory => inventory;
        public QuickSlotHandler QuickSlot => quickSlotHandler;
        public int MaxOxygen
        {
            get => maxOxygen;
            set => maxOxygen = value;
        }
        public int Oxygen => oxygen;
        public float Depth => depth;

        public event Action<float, float> OnOxygenChanged;
        public event Action<float> OnDepthChanged;

        public override void Initialize(EntityData data)
        {
            base.Initialize(data);

            quickSlotHandler = new QuickSlotHandler(inventory);
            oxygen = maxOxygen;
        }

        public void ConsumeOxygen(int amount)
        {
            SetOxygen(oxygen - amount);
        }

        public void RefillOxygen(int amount)
        {
            SetOxygen(oxygen + amount);
        }

        public void SetDepth(float depth)
        {
            this.depth = depth;
            OnDepthChanged?.Invoke(depth);
        }

        private void SetOxygen(int value)
        {
            int clamped = Mathf.Clamp(value, 0, maxOxygen);
            if (oxygen != clamped)
            {
                oxygen = clamped;
                OnOxygenChanged?.Invoke(oxygen, maxOxygen);
            }
        }
    }
}