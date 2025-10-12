using UnityEngine;
using Blue.Inventory;
using Blue.Input;

namespace Blue.UI.QuickSlot
{
    public class QuickSlotController : MonoBehaviour
    {
        [SerializeField] private QuickSlotView view;
        [SerializeField] private QuickSlotSelectHandler selectHandler;

        private QuickSlotHandler quickSlotHandler;

        public void Initialize(QuickSlotHandler quickSlotHandler, PlayerInputHandler inputHandler)
        {
            this.quickSlotHandler = quickSlotHandler;

            quickSlotHandler.OnQuickSlotUpdated += RefreshQuickSlotUI;

            //selectHandler.SetQuickSlotHandler(quickSlotHandler);
            //selectHandler.SetupInput(inputHandler);

            view.Initialize(quickSlotHandler);
        }

        private void OnDisable()
        {
            if (quickSlotHandler != null)
            {
                quickSlotHandler.OnQuickSlotUpdated -= RefreshQuickSlotUI;
            }
        }

        public void RefreshQuickSlotUI()
        {
            view.RefreshQuickSlotUI();
        }
    }
}
