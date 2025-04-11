using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using NFPS.Item;
using NFPS.Input;
using NFPS.Inventory;
using NFPS.UI.Common;
using NFPS.UI.Inventory;

namespace NFPS.UI.QuickSlot
{
    public class QuickSlotSelectHandler : MonoBehaviour
    {
        [SerializeField] private UISelectableNavigator quickSlotNavigator;
        [SerializeField] private UISelectableNavigator inventoryNavigator;
        [SerializeField] private InventoryItemSelectHandler inventoryHandler;

        private QuickSlotHandler quickSlotHandler;
        private ItemData pendingItem;

        public void SetupInput(PlayerInputHandler inputHandler)
        {
            InputAction submit = inputHandler.GetSubmitAction();
            InputAction cancel = inputHandler.GetCancelAction();
            InputAction remove = inputHandler.GetRemoveAction();

            submit.performed += OnRegister;
            cancel.performed += OnCancel;
            remove.performed += OnRemove;

            Debug.Log("[QuickSlotSelectHandler] 入力アクション登録完了");
        }

        public void SetPendingItem(ItemData item)
        {
            pendingItem = item;
        }

        public void SetQuickSlotHandler(QuickSlotHandler handler)
        {
            quickSlotHandler = handler;
        }

        public void OnUIElementSelected()
        {
            GameObject selected = EventSystem.current.currentSelectedGameObject;
            if (selected == null || quickSlotHandler == null) return;

            int index = selected.transform.GetSiblingIndex();

            quickSlotHandler.Register(index, pendingItem);
            Debug.Log($"[QuickSlot] Slot {index} に {pendingItem.name} を登録");

            inventoryHandler.ClearPendingItem();
            inventoryHandler.SwitchToInventory();
        }

        private void OnRegister(InputAction.CallbackContext ctx)
        {
            OnUIElementSelected();
        }

        private void OnRemove(InputAction.CallbackContext ctx)
        {
            GameObject selected = EventSystem.current.currentSelectedGameObject;
            if (selected == null || quickSlotHandler == null) return;

            int index = selected.transform.GetSiblingIndex();
            quickSlotHandler.Unregister(index);

            inventoryHandler.SwitchToInventory();
        }

        private void OnCancel(InputAction.CallbackContext ctx)
        {
            Debug.Log("[QuickSlot] キャンセル → インベントリに戻る");
            inventoryHandler.SwitchToInventory();
        }
    }
}
