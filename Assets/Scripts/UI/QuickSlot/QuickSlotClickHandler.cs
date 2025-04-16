using UnityEngine;
using UnityEngine.EventSystems;
using Blue.Inventory;

namespace Blue.UI.QuickSlot
{
    public class QuickSlotClickHandler : MonoBehaviour, IPointerClickHandler
    {
        private QuickSlotHandler quickSlotHandler;
        private int slotIndex;

        public void Setup(QuickSlotHandler quick_slot_handler, int index)
        {
            quickSlotHandler = quick_slot_handler;
            slotIndex = index;
        }

        public void OnPointerClick(PointerEventData event_data)
        {
            if (event_data.button == PointerEventData.InputButton.Right)
            {
                quickSlotHandler?.Unregister(slotIndex);
            }
        }
    }
}
