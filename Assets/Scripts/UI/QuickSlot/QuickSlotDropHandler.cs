using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Blue.Inventory;
using Blue.Item;
using Blue.UI.DragAndDrop;

namespace Blue.UI.QuickSlot
{
    /// <summary>
    /// クイックスロット専用のドロップハンドラ
    /// アイテムを参照のみ登録し、元のインベントリからは削除しない
    /// </summary>
    public class QuickSlotDropHandler : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler, IItemDropTarget
    {
        [SerializeField] private Image slotImage;
        private QuickSlotHandler quickSlotHandler;
        private int slotIndex;
        private Color defaultColor;

        public void Setup(QuickSlotHandler quick_slot_handler, int index)
        {
            quickSlotHandler = quick_slot_handler;
            slotIndex = index;
            defaultColor = slotImage.color;
        }

        public void OnDrop(PointerEventData event_data)
        {
            if (event_data.pointerDrag != null &&
                event_data.pointerDrag.TryGetComponent(out IDraggableItemSlot draggable))
            {
                ItemData item_data = draggable.GetItemData();
                int quantity = draggable.GetItemQuantity();

                if (CanAcceptItem(item_data, quantity))
                {
                    OnItemDropped(item_data, quantity, draggable.GetSourceContainer());
                }
            }

            slotImage.color = defaultColor;
        }

        public void OnPointerEnter(PointerEventData event_data)
        {
            if (quickSlotHandler != null && event_data.pointerDrag != null)
            {
                slotImage.color = new Color(0.8f, 0.8f, 0.8f, 1f);
            }
        }

        public void OnPointerExit(PointerEventData event_data)
        {
            slotImage.color = defaultColor;
        }

        // IItemDropTarget実装
        public IItemContainer GetTargetContainer()
        {
            // クイックスロットは独自の管理なのでnull
            return null;
        }

        public bool CanAcceptItem(ItemData item_data, int quantity)
        {
            // 使用可能なアイテムのみ受け入れ
            // 今後、ItemTypeで制限を追加する可能性あり
            return quickSlotHandler != null && item_data != null;
        }

        public void OnItemDropped(ItemData item_data, int quantity, IItemContainer source_container)
        {
            // クイックスロットは参照のみなので、元のコンテナからは削除しない
            quickSlotHandler?.Register(slotIndex, item_data);
        }
    }
}