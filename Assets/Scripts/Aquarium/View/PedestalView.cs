using System.Collections.Generic;
using Blue.Item;
using UnityEngine;

namespace Blue.Aquarium
{
    /// <summary>
    /// 収集アイテムの展示台1つ分。飾っているアイテムのモデルを並べる
    /// </summary>
    public class PedestalView : AquariumPieceView
    {
        [SerializeField] private Transform[] slots; // アイテムを置く位置。PedestalPieceData の slotCount と数を合わせる

        private readonly List<GameObject> displayed = new List<GameObject>();

        private PedestalPieceData Pedestal => Placed?.Piece as PedestalPieceData;

        public override void Bind(PlacedPiece placed)
        {
            base.Bind(placed);

            PedestalPieceData pedestal = Pedestal;
            if (pedestal != null && slots != null && slots.Length < pedestal.SlotCount)
            {
                Debug.LogWarning($"展示台のスロットが足りません: {pedestal.Name} (必要 {pedestal.SlotCount} / 実際 {slots.Length})", this);
            }
        }

        /// <summary>
        /// 展示内容をモデルに合わせる
        /// </summary>
        // 動きのない置物なので、水槽と違って作り直しても見た目に影響しない
        public void RefreshContents(IReadOnlyList<ItemData> items)
        {
            ClearContents();

            if (items == null || slots == null) return;

            int count = Mathf.Min(items.Count, slots.Length);
            for (int i = 0; i < count; i++)
            {
                ItemData item = items[i];
                if (item == null || slots[i] == null) continue;

                if (item.Model == null)
                {
                    Debug.LogWarning($"展示に使うモデルが設定されていません: {item.Name}", this);
                    continue;
                }

                GameObject instance = Instantiate(item.Model, slots[i].position, slots[i].rotation, slots[i]);
                displayed.Add(instance);
            }
        }

        public override void ClearContents()
        {
            for (int i = 0; i < displayed.Count; i++)
            {
                if (displayed[i] != null) Destroy(displayed[i]);
            }

            displayed.Clear();
        }
    }
}
