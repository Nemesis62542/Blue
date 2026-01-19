using System;
using Blue.Entity;
using UnityEngine;

namespace Blue.Object
{
    [Serializable]
    public class DisplaySlot
    {
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private bool isSchoolSlot;
        [SerializeField] private int maxDisplayableSize;

        private EntityData currentEntity;
        private GameObject instantiatedObject;

        public Transform SpawnPoint => spawnPoint;
        public bool IsEmpty => currentEntity == null;
        public bool IsSchoolSlot => isSchoolSlot;
        public int MaxDisplayableSize => maxDisplayableSize;
        public EntityData CurrentEntity => currentEntity;

        /// <summary>
        /// このスロットに生物を配置できるか判定
        /// </summary>
        public bool CanPlace(EntityData entity)
        {
            if (!IsEmpty) return false;
            if (maxDisplayableSize < entity.DisplaySize) return false;

            // 群れ型生物はスクール用スロットのみ
            bool needsSchoolSlot = entity.School != null;
            if (needsSchoolSlot && !isSchoolSlot) return false;

            return true;
        }

        /// <summary>
        /// 生物を配置
        /// </summary>
        public void PlaceEntity(EntityData entity, Transform parentTransform)
        {
            if (!CanPlace(entity))
            {
                Debug.LogError($"このスロットに {entity.Name} を配置できません");
                return;
            }

            currentEntity = entity;

            // Schoolがある場合はSchoolのGameObjectを、ない場合は通常のObjectを使用
            GameObject prefab = entity.School != null ? entity.School.gameObject : entity.Object;

            if (prefab != null && spawnPoint != null)
            {
                instantiatedObject = UnityEngine.Object.Instantiate(
                    prefab,
                    spawnPoint.position,
                    Quaternion.identity,
                    parentTransform
                );
            }
            else
            {
                Debug.LogError($"プレハブまたはスポーンポイントが設定されていません: {entity.Name}");
            }
        }

        /// <summary>
        /// 配置された生物をクリア
        /// </summary>
        public void Clear()
        {
            if (instantiatedObject != null)
            {
                UnityEngine.Object.Destroy(instantiatedObject);
                instantiatedObject = null;
            }
            currentEntity = null;
        }
    }
}
