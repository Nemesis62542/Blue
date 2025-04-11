using UnityEngine;
using UnityEngine.EventSystems;
using NFPS.Item;

namespace NFPS.UI.Inventory
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
            if (itemData == null) return;
            itemSlot.transform.position = event_data.position;
        }

        public void OnEndDrag(PointerEventData event_data)
        {
            itemSlot.transform.SetParent(originalParent);
            canvasGroup.blocksRaycasts = true;
            canvasGroup.alpha = 1f;
        }
    }
}
