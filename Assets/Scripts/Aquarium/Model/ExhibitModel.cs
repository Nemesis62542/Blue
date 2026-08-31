using System;
using System.Collections.Generic;
using Blue.Entity;
using Blue.Item;

namespace Blue.Aquarium
{
    /// <summary>
    /// どの設置物に何を展示しているかを保持する
    /// </summary>
    public class ExhibitModel
    {
        private static readonly IReadOnlyList<EntityData> EMPTY_ENTITIES = Array.Empty<EntityData>();
        private static readonly IReadOnlyList<ItemData> EMPTY_ITEMS = Array.Empty<ItemData>();

        private readonly Dictionary<string, List<EntityData>> tankContents = new Dictionary<string, List<EntityData>>();
        private readonly Dictionary<string, List<ItemData>> pedestalContents = new Dictionary<string, List<ItemData>>();

        /// <summary>
        /// 展示内容が変わった設置物の InstanceID を通知する
        /// </summary>
        public event Action<string> OnContentsChanged;

        /// <summary>
        /// 水槽の展示内容を取得する。何も入っていなければ空
        /// </summary>
        public IReadOnlyList<EntityData> GetEntities(string instance_id)
        {
            if (string.IsNullOrEmpty(instance_id)) return EMPTY_ENTITIES;

            return tankContents.TryGetValue(instance_id, out List<EntityData> contents) ? contents : EMPTY_ENTITIES;
        }

        /// <summary>
        /// 展示台の展示内容を取得する。何も飾っていなければ空
        /// </summary>
        public IReadOnlyList<ItemData> GetItems(string instance_id)
        {
            if (string.IsNullOrEmpty(instance_id)) return EMPTY_ITEMS;

            return pedestalContents.TryGetValue(instance_id, out List<ItemData> contents) ? contents : EMPTY_ITEMS;
        }

        public void AddEntity(string instance_id, EntityData entity)
        {
            if (string.IsNullOrEmpty(instance_id) || entity == null) return;

            if (!tankContents.TryGetValue(instance_id, out List<EntityData> contents))
            {
                contents = new List<EntityData>();
                tankContents[instance_id] = contents;
            }

            contents.Add(entity);
            OnContentsChanged?.Invoke(instance_id);
        }

        public bool RemoveEntity(string instance_id, EntityData entity)
        {
            if (string.IsNullOrEmpty(instance_id) || entity == null) return false;
            if (!tankContents.TryGetValue(instance_id, out List<EntityData> contents)) return false;
            if (!contents.Remove(entity)) return false;

            OnContentsChanged?.Invoke(instance_id);
            return true;
        }

        public void AddItem(string instance_id, ItemData item)
        {
            if (string.IsNullOrEmpty(instance_id) || item == null) return;

            if (!pedestalContents.TryGetValue(instance_id, out List<ItemData> contents))
            {
                contents = new List<ItemData>();
                pedestalContents[instance_id] = contents;
            }

            contents.Add(item);
            OnContentsChanged?.Invoke(instance_id);
        }

        public bool RemoveItem(string instance_id, ItemData item)
        {
            if (string.IsNullOrEmpty(instance_id) || item == null) return false;
            if (!pedestalContents.TryGetValue(instance_id, out List<ItemData> contents)) return false;
            if (!contents.Remove(item)) return false;

            OnContentsChanged?.Invoke(instance_id);
            return true;
        }

        /// <summary>
        /// 設置物1つ分の展示内容を空にする。撤去時に呼ぶ
        /// </summary>
        public void ClearPiece(string instance_id)
        {
            if (string.IsNullOrEmpty(instance_id)) return;

            bool removed = tankContents.Remove(instance_id);
            removed |= pedestalContents.Remove(instance_id);

            if (removed)
            {
                OnContentsChanged?.Invoke(instance_id);
            }
        }

        /// <summary>
        /// 展示中の設置物の InstanceID を列挙する
        /// </summary>
        public IEnumerable<string> EnumerateTankInstanceIDs() => tankContents.Keys;

        public IEnumerable<string> EnumeratePedestalInstanceIDs() => pedestalContents.Keys;

        public void Clear()
        {
            tankContents.Clear();
            pedestalContents.Clear();
        }
    }
}
