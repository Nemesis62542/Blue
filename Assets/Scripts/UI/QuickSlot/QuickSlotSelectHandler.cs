using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Blue.Item;
using Blue.Input;
using Blue.UI.Common;
using Blue.UI.Inventory;

namespace Blue.UI.QuickSlot
{
    public class QuickSlotSelectHandler : MonoBehaviour
    {
        [SerializeField] private UISelectableNavigator quickSlotNavigator;
        [SerializeField] private UISelectableNavigator inventoryNavigator;
        [SerializeField] private InventoryItemSelectHandler inventoryHandler;

        private QuickSlotModel quickSlotModel;
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

        public void SetQuickSlotModel(QuickSlotModel model)
        {
            quickSlotModel = model;
        }

        public void OnUIElementSelected()
        {
            GameObject selected = EventSystem.current.currentSelectedGameObject;
            if (selected == null || quickSlotModel == null) return;

            int index = selected.transform.GetSiblingIndex();

            quickSlotModel.AddItem(pendingItem, 1);
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
            if (selected == null || quickSlotModel == null) return;

            int index = selected.transform.GetSiblingIndex();
            ItemData item = quickSlotModel.GetItem(index);
            if (item != null)
            {
                quickSlotModel.RemoveItem(item, quickSlotModel.GetQuickSlotItem(index).Quantity);
            }

            inventoryHandler.SwitchToInventory();
        }

        private void OnCancel(InputAction.CallbackContext ctx)
        {
            Debug.Log("[QuickSlot] キャンセル → インベントリに戻る");
            inventoryHandler.SwitchToInventory();
        }
    }
}
