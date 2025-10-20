using UnityEngine;
using Blue.Input;

namespace Blue.UI.QuickSlot
{
    public class QuickSlotController : MonoBehaviour
    {
        [SerializeField] private QuickSlotView view;

        private QuickSlotModel quickSlotModel;

        public void Initialize(QuickSlotModel quick__slot_model)
        {
            quickSlotModel = quick__slot_model;

            quick__slot_model.OnQuickSlotUpdated += RefreshQuickSlotUI;

            view.Initialize(quick__slot_model);
        }

        private void OnDisable()
        {
            if (quickSlotModel != null)
            {
                quickSlotModel.OnQuickSlotUpdated -= RefreshQuickSlotUI;
            }
        }

        public void RefreshQuickSlotUI()
        {
            view.UpdateInventoryUI();
        }
    }
}
