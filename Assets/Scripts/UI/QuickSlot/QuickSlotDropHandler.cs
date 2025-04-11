using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using NFPS.Inventory;
using NFPS.UI.Inventory;

namespace NFPS.UI.QuickSlot
{
    public class QuickSlotDropHandler : MonoBehaviour, IDropHandler
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
                event_data.pointerDrag.TryGetComponent(out ItemSlotDragHandler dragged_item_slot))
            {
                quickSlotHandler?.Register(slotIndex, dragged_item_slot.CurrentItem);
            }

            slotImage.color = defaultColor;
        }

        public void OnPointerEnter(PointerEventData event_data)
        {
            if (quickSlotHandler != null)
            {
                slotImage.color = new Color(0.8f, 0.8f, 0.8f, 1f);
            }
        }

        public void OnPointerExit(PointerEventData event_data)
        {
            slotImage.color = defaultColor;
        }
    }
}