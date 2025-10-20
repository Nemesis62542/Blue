using UnityEngine;
using Blue.Inventory;
using Blue.UI.Inventory;
using Blue.UI.DragAndDrop;
using System.Collections.Generic;
using Blue.Item;

namespace Blue.UI.QuickSlot
{
    public class QuickSlotView : MonoBehaviour
    {
        [SerializeField] private ItemSlot itemSlotPrefab;

        private QuickSlotModel quickSlotModel;
        private GenericItemDropHandler[] quickSlots;
        private List<ItemSlot> itemSlots = new List<ItemSlot>();
        private Queue<ItemSlot> itemSlotPool = new Queue<ItemSlot>();

        public void Initialize(QuickSlotModel quickSlotModel)
        {
            this.quickSlotModel = quickSlotModel;
            quickSlots = GetComponentsInChildren<GenericItemDropHandler>();

            // 各ドロップ枠にGenericItemDropHandlerをセットアップ
            foreach (GenericItemDropHandler handler in quickSlots)
            {
                handler.Setup(quickSlotModel);
            }
        }

        public void UpdateInventoryUI()
        {
            if (quickSlotModel == null) return;

            ReleaseAllItemSlots();

            for (int i = 0; i < quickSlots.Length; i++)
            {
                if (quickSlots[i] == null) continue;

                QuickSlotItem slot_item = quickSlotModel.GetQuickSlotItem(i);

                if (slot_item != null)
                {
                    AddItemToUI(slot_item.ItemData, slot_item.Quantity, quickSlots[i].transform);
                }
            }
        }

        private void AddItemToUI(ItemData item_data, int count, Transform parent)
        {
            ItemSlot item_slot = GetOrCreateItemSlot();
            item_slot.transform.SetParent(parent);
            item_slot.transform.localPosition = Vector3.zero;
            item_slot.transform.localRotation = Quaternion.identity;
            item_slot.transform.localScale = Vector3.one;
            item_slot.SetItem(item_data, count);

            if (item_slot.HoverArea.TryGetComponent(out ItemSlotDragHandler drag_handler))
            {
                drag_handler.Initialize(quickSlotModel);
            }
        }

        private ItemSlot GetOrCreateItemSlot()
        {
            ItemSlot slot;
            if (itemSlotPool.Count > 0)
            {
                slot = itemSlotPool.Dequeue();
                slot.gameObject.SetActive(true);
                return slot;
            }
            slot = Instantiate(itemSlotPrefab);
            itemSlots.Add(slot);
            return slot;
        }

        private void ReleaseAllItemSlots()
        {
            foreach (ItemSlot slot in itemSlots)
            {
                // ドラッグ中のスロットはプールに戻さない
                if (slot.HoverArea.TryGetComponent(out ItemSlotDragHandler drag_handler))
                {
                    if (drag_handler.IsDragging)
                    {
                        continue;
                    }
                }

                slot.gameObject.SetActive(false);
                itemSlotPool.Enqueue(slot);
            }
        }
    }
}
