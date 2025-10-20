using UnityEngine;
using UnityEngine.EventSystems;
using Blue.Item;
using Blue.UI.DragAndDrop;

namespace Blue.UI.Inventory
{
    public class ItemSlotDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDraggableItemSlot
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private ItemSlot itemSlot;

        private Transform originalParent;
        private Canvas canvas;
        private IItemContainer sourceContainer;
        private bool isDragging = false;

        public ItemData CurrentItem => itemSlot.CurrentItem;
        public bool IsDragging => isDragging;

        public void Initialize(IItemContainer container)
        {
            sourceContainer = container;
            canvas = GetComponentInParent<Canvas>();
        }

        // IDraggableItemSlot実装
        public ItemData GetItemData()
        {
            return itemSlot.CurrentItem;
        }

        public int GetItemQuantity()
        {
            return itemSlot.CurrentItemCount;
        }

        public IItemContainer GetSourceContainer()
        {
            return sourceContainer;
        }

        public void OnBeginDrag(PointerEventData event_data)
        {
            if (itemSlot.CurrentItem == null) return;

            isDragging = true;
            originalParent = itemSlot.transform.parent;
            itemSlot.transform.SetParent(canvas.transform);
            canvasGroup.blocksRaycasts = false;
            canvasGroup.alpha = 0.6f;
        }

        public void OnDrag(PointerEventData event_data)
        {
            Vector2 local_position;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                itemSlot.transform.parent as RectTransform,
                event_data.position,
                event_data.pressEventCamera,
                out local_position
            );

            itemSlot.transform.localPosition = new Vector3(local_position.x, local_position.y, 0f);
        }

        public void OnEndDrag(PointerEventData event_data)
        {
            EndDragInternal();
        }

        /// <summary>
        /// ドラッグを強制終了する（OnDropから呼ばれる）
        /// </summary>
        public void ForceEndDrag()
        {
            EndDragInternal();
        }

        private void EndDragInternal()
        {
            isDragging = false;

            if (originalParent != null)
            {
                itemSlot.transform.SetParent(originalParent);
                itemSlot.transform.localPosition = Vector3.zero;
                itemSlot.transform.localRotation = Quaternion.identity;
                itemSlot.transform.localScale = Vector3.one;
            }
            canvasGroup.blocksRaycasts = true;
            canvasGroup.alpha = 1f;
        }
    }
}
