using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Blue.Input
{
    public enum InputMapType
    {
        None,
        Player,
        Menu,
        Inventory
    }

    public class PlayerInputHandler
    {
        private PlayerInputActions inputActions;
        private Vector2 moveInput;
        private Vector2 lookInput;
        private bool jumpPressed;
        private InputMapType currentInputMap = InputMapType.None;

        public Vector2 MoveInput => moveInput;
        public Vector2 LookInput => lookInput;
        public bool JumpPressed => jumpPressed;
        public event Action OnAttackEvent;
        public event Action OnInventoryToggleEvent;
        public event Action OnPauseToggleEvent;
        public event Action<int> OnQuickSlotUseEvent;
        public event Action OnInteractPressEvent;
        public event Action OnInteractReleaseEvent;

        public InputAction GetSubmitAction() => inputActions.Inventory.Submit;
        public InputAction GetCancelAction() => inputActions.Inventory.Cancel;
        public InputAction GetRegisterAction() => inputActions.Inventory.Register;
        public InputAction GetRemoveAction() => inputActions.Inventory.Remove;

        public PlayerInputHandler()
        {
            inputActions = new PlayerInputActions();

            inputActions.Player.Move.performed += OnMove;
            inputActions.Player.Move.canceled += OnMove;
            inputActions.Player.Jump.performed += OnJump;
            inputActions.Player.Look.performed += OnLook;
            inputActions.Player.Look.canceled += OnLook;
            inputActions.Player.Interact.started += OnInteractPress;
            inputActions.Player.Interact.canceled += OnInteractRelease;
            inputActions.Player.Attack.performed += OnAttack;
            inputActions.Player.QuickSlot1.performed += OnQuickSlot1;
            inputActions.Player.QuickSlot2.performed += OnQuickSlot2;
            inputActions.Player.QuickSlot3.performed += OnQuickSlot3;
            inputActions.Player.QuickSlot4.performed += OnQuickSlot4;
            inputActions.Player.Inventory.performed += OnInventoryToggle;
            inputActions.Player.Pause.performed += OnPauseToggle;

            inputActions.Inventory.Close.performed += OnInventoryToggle;
            inputActions.Menu.Close.performed += OnPauseToggle;

            inputActions.Enable();
        }

        public void Dispose()
        {
            inputActions.Player.Move.performed -= OnMove;
            inputActions.Player.Move.canceled -= OnMove;
            inputActions.Player.Jump.performed -= OnJump;
            inputActions.Player.Look.performed -= OnLook;
            inputActions.Player.Look.canceled -= OnLook;
            inputActions.Player.Interact.started -= OnInteractPress;
            inputActions.Player.Interact.canceled -= OnInteractRelease;
            inputActions.Player.Attack.performed -= OnAttack;
            inputActions.Player.QuickSlot1.performed -= OnQuickSlot1;
            inputActions.Player.QuickSlot2.performed -= OnQuickSlot2;
            inputActions.Player.QuickSlot3.performed -= OnQuickSlot3;
            inputActions.Player.QuickSlot4.performed -= OnQuickSlot4;
            inputActions.Player.Inventory.performed -= OnInventoryToggle;
            inputActions.Player.Pause.performed -= OnPauseToggle;

            inputActions.Inventory.Close.performed -= OnInventoryToggle;
            inputActions.Menu.Close.performed -= OnPauseToggle;

            inputActions.Disable();
        }

        private void OnMove(InputAction.CallbackContext context)
        {
            moveInput = context.ReadValue<Vector2>();
        }

        private void OnJump(InputAction.CallbackContext context)
        {
            jumpPressed = true;
        }

        private void OnLook(InputAction.CallbackContext context)
        {
            lookInput = context.ReadValue<Vector2>();
        }

        private void OnInteractPress(InputAction.CallbackContext context)
        {
            OnInteractPressEvent?.Invoke();
        }

        private void OnInteractRelease(InputAction.CallbackContext context)
        {
            OnInteractReleaseEvent?.Invoke();
        }

        private void OnAttack(InputAction.CallbackContext context)
        {
            OnAttackEvent?.Invoke();
        }

        private void OnInventoryToggle(InputAction.CallbackContext context)
        {
            OnInventoryToggleEvent?.Invoke();
        }

        private void OnPauseToggle(InputAction.CallbackContext context)
        {
            OnPauseToggleEvent?.Invoke();
        }

        private void OnQuickSlot1(InputAction.CallbackContext context)
        {
            OnQuickSlotUseEvent?.Invoke(0);
        }

        private void OnQuickSlot2(InputAction.CallbackContext context)
        {
            OnQuickSlotUseEvent?.Invoke(1);
        }

        private void OnQuickSlot3(InputAction.CallbackContext context)
        {
            OnQuickSlotUseEvent?.Invoke(2);
        }

        private void OnQuickSlot4(InputAction.CallbackContext context)
        {
            OnQuickSlotUseEvent?.Invoke(3);
        }

        public void ResetJumpFlag()
        {
            jumpPressed = false;
        }

        public void DisableInput()
        {
            inputActions.Disable();
        }

        public void EnableInput()
        {
            inputActions.Enable();
        }

        public void SetInputMap(InputMapType type)
        {
            if (currentInputMap == type) return;

            inputActions.Disable();

            switch (type)
            {
                case InputMapType.Player:
                    inputActions.Player.Enable();
                    break;

                case InputMapType.Menu:
                    inputActions.Menu.Enable();
                    break;

                case InputMapType.Inventory:
                    inputActions.Inventory.Enable();
                    break;

                case InputMapType.None:
                    break;
            }

            currentInputMap = type;
            Debug.Log($"[Input] InputMap 切り替え → {type}");
        }

        public InputMapType GetCurrentInputMap()
        {
            return currentInputMap;
        }
    }
}