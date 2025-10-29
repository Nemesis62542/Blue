using System;
using System.Collections.Generic;
using Blue.Entity;
using Blue.Inventory;
using Blue.UI.QuickSlot;
using UnityEngine;

namespace Blue.Player
{
    public class PlayerModel : BaseEntityModel
    {
        private InventoryModel inventory = new InventoryModel();
        private Dictionary<EntityData, int> capturedEntities = new Dictionary<EntityData, int>();
        private QuickSlotModel quickSlotModel;
        private int maxOxygen = 100;
        private int oxygen;
        private float maxFuel = 100f;
        private float fuel;
        private float depth;

        public PlayerModel(EntityData data, InventoryModel inventory = null, QuickSlotModel quickSlot = null, int? initialOxygen = null) : base(data)
        {
            this.inventory = inventory ?? new InventoryModel();
            quickSlotModel = quickSlot ?? new QuickSlotModel();
            oxygen = initialOxygen ?? maxOxygen;
            fuel = maxFuel;
        }

        public InventoryModel Inventory => inventory;
        public QuickSlotModel QuickSlot => quickSlotModel;
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
        public float MaxFuel
        {
            get => maxFuel;
            set => maxFuel = value;
        }
        public float Fuel
        {
            get => fuel;
            private set => SetFuel(value);
        }
        public float Depth => depth;

        public event Action<float, float> OnOxygenChanged;
        public event Action<float, float> OnFuelChanged;
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

        public void ConsumeFuel(float amount)
        {
            SetFuel(fuel - amount);
        }

        public void RefillFuel(float amount)
        {
            SetFuel(fuel + amount);
        }

        private void SetFuel(float value)
        {
            float clamped = Mathf.Clamp(value, 0f, maxFuel);
            if (!Mathf.Approximately(fuel, clamped))
            {
                fuel = clamped;
                OnFuelChanged?.Invoke(fuel, maxFuel);
            }
        }

        public void AddCapturedEntity(EntityData entity)
        {
            if (capturedEntities.ContainsKey(entity))
            {
                capturedEntities[entity]++;
            }
            else
            {
                capturedEntities.Add(entity, 1);
            }
        }

        public PlayerTransferData CreateTransferData()
        {
            return new PlayerTransferData(capturedEntities);
        }
    }
}