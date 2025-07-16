using System;
using System.Collections.Generic;
using System.Linq;
using Blue.Entity;
using Blue.Inventory;
using UnityEngine;

namespace Blue.Player
{
    public class PlayerModel : BaseEntityModel
    {
        private InventoryModel inventory = new InventoryModel();
        private Dictionary<EntityData, int> capturedEntity = new Dictionary<EntityData, int>();
        private QuickSlotHandler quickSlotHandler;
        private int maxOxygen = 100;
        private int oxygen;
        private float depth;

        public PlayerModel(EntityData data, InventoryModel inventory = null, int? initialOxygen = null) : base(data)
        {
            this.inventory = inventory ?? new InventoryModel();
            quickSlotHandler = new QuickSlotHandler(this.inventory);
            oxygen = initialOxygen ?? maxOxygen;
        }

        public InventoryModel Inventory => inventory;
        public QuickSlotHandler QuickSlot => quickSlotHandler;
        public int MaxOxygen
        {
            get => maxOxygen;
            set => maxOxygen = value;
        }
        public int Oxygen
        {
            get => oxygen;
            private set => SetOxygen(value);
        }
        public float Depth => depth;

        public event Action<float, float> OnOxygenChanged;
        public event Action<float> OnDepthChanged;

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

        public void AddCapturedEntity(EntityData entity)
        {
            if (capturedEntity.ContainsKey(entity))
            {
                capturedEntity[entity]++;
            }
            else
            {
                capturedEntity.Add(entity, 1);
            }
        }
    }
}