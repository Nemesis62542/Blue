using UnityEngine;
using UnityEngine.EventSystems;
using Blue.Item;

namespace Blue.UI.Inventory
{
    public class ItemSlotDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private ItemSlot itemSlot;

        private Transform originalParent;
        private Canvas canvas;
        private ItemData itemData;

        public ItemData CurrentItem => itemData;

        public void Initialize(ItemData item)
        {
            itemData = item;
        }

        private void Awake()
        {
            canvas = GetComponentInParent<Canvas>();
        }

        public void OnBeginDrag(PointerEventData event_data)
        {
            if (itemData == null) return;

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
            itemSlot.transform.SetParent(originalParent);
            canvasGroup.blocksRaycasts = true;
            canvasGroup.alpha = 1f;
        }
    }
}
