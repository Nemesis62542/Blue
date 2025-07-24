using System;
using System.Collections.Generic;
using Blue.Entity;
using UnityEngine;

namespace Blue.Object
{
    public class AquariumController : MonoBehaviour
    {
        [SerializeField] private List<DisplayData> displayData = new List<DisplayData>();

        private void Start()
        {
            //Initialize();
        }

        private void Initialize()
        {
            foreach (DisplayData data in displayData)
            {
                InstantiateDisplayEntity(data);
            }
        }

        private void InstantiateDisplayEntity(DisplayData data)
        {
            GameObject @object = data.Entity.School != null ? data.Entity.School.gameObject : data.Entity.Object;
            Instantiate(@object, data.SpawnPoint.position, Quaternion.identity, transform);
        }

        public DisplayData FirstEnptyDisplayData(bool isSchool = false)
        {
            foreach (DisplayData data in displayData)
            {
                if (data.Entity != null) continue;

                if (isSchool)
                {
                    if (data.IsSchool) return data;
                }
                else
                {
                    return data;
                }
            }

            return null;
        }

        public void SetDisplayEntity(DisplayData data, EntityData entity)
        {
            if (data.MaxDisplayableSize <= entity.DisplaySize) throw new Exception("生物の展示に失敗");

            if (entity.School != null)
            {
                if (data.IsSchool)
                {
                    data.Entity = entity;
                    InstantiateDisplayEntity(data);
                }
                else throw new Exception("生物の展示に失敗");
            }
            else
            {
                data.Entity = entity;
                InstantiateDisplayEntity(data);
            }
        }
    }

    [Serializable]
    public class DisplayData
    {
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private bool isSchool;
        [SerializeField] private int maxDisplayableSize;
        [SerializeField] private EntityData entity;

        public Transform SpawnPoint => spawnPoint;
        public bool IsSchool => isSchool;
        public int MaxDisplayableSize => maxDisplayableSize;

        public EntityData Entity { get => entity; set => entity = value; }
    }
}