using System.Collections.Generic;
using System.Linq;
using Blue.Entity;
using UnityEngine;

namespace Blue.Object
{
    public class AquariumController : MonoBehaviour
    {
        [SerializeField] private HabitationArea habitation;
        [SerializeField] private List<DisplaySlot> slots = new List<DisplaySlot>();

        public HabitationArea Habitation => habitation;

        /// <summary>
        /// 指定された生物を配置できる空きスロットがあるか
        /// </summary>
        public bool HasAvailableSlot(EntityData entity)
        {
            return FindAvailableSlot(entity) != null;
        }

        /// <summary>
        /// 生物を水槽に追加
        /// </summary>
        public bool TryAddEntity(EntityData entity)
        {
            DisplaySlot slot = FindAvailableSlot(entity);
            if (slot == null)
            {
                Debug.LogWarning($"{entity.Name} を配置できる空きスロットがありません");
                return false;
            }

            slot.PlaceEntity(entity, transform);
            return true;
        }

        /// <summary>
        /// 生物を配置可能な空きスロットを検索
        /// </summary>
        private DisplaySlot FindAvailableSlot(EntityData entity)
        {
            return slots.FirstOrDefault(s => s.CanPlace(entity));
        }

        /// <summary>
        /// すべてのスロットをクリア
        /// </summary>
        public void ClearAllSlots()
        {
            foreach (DisplaySlot slot in slots)
            {
                slot.Clear();
            }
        }

        /// <summary>
        /// 指定された生物を水槽から削除
        /// </summary>
        public bool RemoveEntity(EntityData entity)
        {
            DisplaySlot slot = slots.FirstOrDefault(s => s.CurrentEntity == entity);
            if (slot == null) return false;

            slot.Clear();
            return true;
        }
    }
}