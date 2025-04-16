using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Blue.Item;
using Blue.UI.Common;
using Blue.Input;
using Blue.UI.QuickSlot;

namespace Blue.UI.Inventory
{
    public class InventoryItemSelectHandler : MonoBehaviour
    {
        private enum SelectionPhase
        {
            Inventory,
            QuickSlot
        }

        [SerializeField] private UISelectableNavigator quickSlotNavigator;
        [SerializeField] private UISelectableNavigator inventoryNavigator;
        [SerializeField] private QuickSlotSelectHandler quickSlotSelectHandler;

        private SelectionPhase phase = SelectionPhase.Inventory;
        private ItemData selectedItemData;

        public void SetupInput(PlayerInputHandler inputHandler)
        {
            InputAction submit = inputHandler.GetSubmitAction();
            InputAction cancel = inputHandler.GetCancelAction();
            InputAction register = inputHandler.GetRegisterAction();

            submit.performed += OnSubmit;
            cancel.performed += OnCancel;
            register.performed += OnRegister;

            Debug.Log("[ItemSelectHandler] 入力アクション登録完了");
        }

        private void OnSubmit(InputAction.CallbackContext ctx)
        {
            GameObject selected = EventSystem.current.currentSelectedGameObject;
            if (selected == null) return;

            if (phase == SelectionPhase.Inventory)
            {
                OnUIElementSelected(selected);
            }
        }

        private void OnRegister(InputAction.CallbackContext ctx)
        {
            GameObject selected = EventSystem.current.currentSelectedGameObject;
            if (phase == SelectionPhase.QuickSlot && selected != null)
            {
                quickSlotSelectHandler.OnUIElementSelected();
            }
        }

        private void OnCancel(InputAction.CallbackContext ctx)
        {
            TryCancel();
        }

        private void OnUIElementSelected(GameObject selected)
        {
            if (selected.TryGetComponent(out ItemSlot slot) && slot.CurrentItem != null)
            {
                selectedItemData = slot.CurrentItem;
                SwitchToQuickSlot();
            }
        }

        private void SwitchToQuickSlot()
        {
            phase = SelectionPhase.QuickSlot;
            quickSlotSelectHandler.SetPendingItem(selectedItemData);
            inventoryNavigator.ClearSelection();
            quickSlotNavigator.InitializeSelection();
        }

        public void SwitchToInventory()
        {
            phase = SelectionPhase.Inventory;
            quickSlotNavigator.ClearSelection();
            inventoryNavigator.InitializeSelection();
        }

        public void ClearPendingItem()
        {
            selectedItemData = null;
        }

        public void TryCancel()
        {
            if (phase == SelectionPhase.QuickSlot)
            {
                Debug.Log("キャンセル：インベントリに戻る");
                SwitchToInventory();
            }
        }
    }
}