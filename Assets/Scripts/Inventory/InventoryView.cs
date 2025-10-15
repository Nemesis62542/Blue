using UnityEngine;
using System.Collections.Generic;
using Blue.Item;
using Blue.Input;
using Blue.UI.Common;
using Blue.UI.Inventory;
using Blue.UI.DragAndDrop;

namespace Blue.Inventory
{
    public class InventoryView : MonoBehaviour
    {
        [SerializeField] private Transform itemSlotParent;
        [SerializeField] private ItemSlot itemSlotPrefab;
        [SerializeField] private UISelectableNavigator navigator;
        [SerializeField] private InventoryItemSelectHandler itemSelectHandler;

        private List<ItemSlot> itemSlots = new List<ItemSlot>();
        private InventoryModel model;
        private IItemContainer container;
        private Queue<ItemSlot> itemSlotPool = new Queue<ItemSlot>();

        public void Initialize(InventoryModel model, PlayerInputHandler input_handler, IItemContainer container)
        {
            this.model = model;
            this.container = container;
            itemSelectHandler.SetupInput(input_handler);
        }

        public void UpdateInventoryUI()
        {
            if (model == null) return;

            // 他のインベントリのプールに入っているスロットを回収
            CleanupOrphanedSlots();

            ReleaseAllItemSlots();

            foreach (KeyValuePair<ItemData, int> item in model.GetAllItems())
            {
                AddItemToUI(item.Key, item.Value);
            }
            navigator.InitializeSelection();
        }

        /// <summary>
        /// 他のインベントリの親に属しているスロットを自分のプールに戻す
        /// </summary>
        private void CleanupOrphanedSlots()
        {
            for (int i = itemSlots.Count - 1; i >= 0; i--)
            {
                ItemSlot slot = itemSlots[i];
                if (slot == null)
                {
                    itemSlots.RemoveAt(i);
                    continue;
                }

                // 親が自分のitemSlotParentでない場合は、正しい親に戻す
                if (slot.transform.parent != itemSlotParent)
                {
                    slot.transform.SetParent(itemSlotParent);
                    slot.transform.localPosition = Vector3.zero;
                    slot.transform.localRotation = Quaternion.identity;
                    slot.transform.localScale = Vector3.one;
                }
            }
        }

        private void AddItemToUI(ItemData item_data, int count)
        {
            ItemSlot new_item_slot = GetOrCreateItemSlot();
            new_item_slot.SetItem(item_data, count);
            if (new_item_slot.HoverArea.TryGetComponent(out ItemSlotDragHandler drag_handler))
            {
                drag_handler.Initialize(container);
            }
        }

        private ItemSlot GetOrCreateItemSlot()
        {
            ItemSlot slot;
            if (itemSlotPool.Count > 0)
            {
                slot = itemSlotPool.Dequeue();
                slot.gameObject.SetActive(true);

                // プールから取り出す際にTransformをリセット
                slot.transform.SetParent(itemSlotParent);
                slot.transform.localPosition = Vector3.zero;
                slot.transform.localRotation = Quaternion.identity;
                slot.transform.localScale = Vector3.one;
                return slot;
            }
            slot = Instantiate(itemSlotPrefab, itemSlotParent);
            itemSlots.Add(slot);
            return slot;
        }

        private void ReleaseAllItemSlots()
        {
            foreach (ItemSlot child in itemSlots)
            {
                // ドラッグ中のスロットはプールに戻さない
                if (child.HoverArea.TryGetComponent(out ItemSlotDragHandler drag_handler))
                {
                    if (drag_handler.IsDragging)
                    {
                        continue;
                    }
                }

                child.gameObject.SetActive(false);
                itemSlotPool.Enqueue(child);
            }
        }
    }
}