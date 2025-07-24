using Blue.Attack;
using Blue.Entity;
using Blue.Game;
using Blue.Input;
using Blue.Interface;
using Blue.Inventory;
using Blue.Item;
using Blue.Object;
using Blue.UI;
using Blue.UI.Screen;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Blue.Player
{
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerController : BaseEntityController<PlayerModel, PlayerView>, IInventoryHolder, IAttackable, ILivingEntity
    {
        [SerializeField] private Rigidbody rb;
        [SerializeField] private Transform camTransform;
        [SerializeField] private UIController uiController;
        [SerializeField] private ScannerController scannerController;
        [SerializeField] private PlayerStatusView playerStatusView;
        [Header("プレイヤーの情報")]
        [SerializeField] private InventoryController inventoryController;
        [Header("プレイヤーの操作に関する値")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float jumpStrength = 5f;
        [SerializeField] private float maxLookUpAngle = 80f;
        [SerializeField] private float interactDistance = 3.0f;

        private PlayerInputHandler inputHandler;
        private bool isGrounded;
        private float camVerticalRotation = 0f;
        private float waterLevel = 0;
        private float mouseLookSensitivity = 10f;
        private float controllerLookSensitivity = 10f;

        public InventoryModel Inventory => model.Inventory;
        public QuickSlotHandler QuickSlot => model.QuickSlot;

        public Status Status => model.Status;

        public void SetWaterLevel(float level)
        {
            waterLevel = level;
        }

        protected override void Awake()
        {
            base.Awake();
            inputHandler = new PlayerInputHandler();
            model = new PlayerModel(data);

            model.Status.OnHPChanged += HandleHPChanged;
            model.OnOxygenChanged += HandleOxygenChanged;
            model.OnDepthChanged += HandleDepthChanged;
            inventoryController.Initialize(Inventory, QuickSlot, inputHandler);

            inputHandler.OnInteractEvent += InteractObject;
            inputHandler.OnScanEvent += Scan;
            inputHandler.OnAttackEvent += Attack;
            inputHandler.OnInventoryToggleEvent += ToggleInventory;
            inputHandler.OnPauseToggleEvent += TogglePause;
            inputHandler.OnQuickSlotChangeEvent += QuickSlot.SelectSlot;

            QuickSlot.OnQuickSlotChanged += HandleSlotChanged;
            Inventory.OnPickUpItem = OnPickUpItem;

            Cursor.lockState = CursorLockMode.Locked;
            inputHandler.SetInputMap(InputMapType.Player);
        }

        private void OnDestroy()
        {
            model.Status.OnHPChanged -= HandleHPChanged;
            model.OnOxygenChanged -= HandleOxygenChanged;
            model.OnDepthChanged -= HandleDepthChanged;

            inputHandler.OnInteractEvent -= InteractObject;
            inputHandler.OnScanEvent -= Scan;
            inputHandler.OnAttackEvent -= Attack;
            inputHandler.OnInventoryToggleEvent -= ToggleInventory;
            inputHandler.OnPauseToggleEvent -= TogglePause;
            inputHandler.OnQuickSlotChangeEvent -= QuickSlot.SelectSlot;
            inputHandler.Dispose();

            QuickSlot.OnQuickSlotChanged -= HandleSlotChanged;

            inputHandler.SetInputMap(InputMapType.None);
        }

        private void Update()
        {
            HandleMove();
            HandleViewRotation();
            model.SetDepth(waterLevel - transform.position.y);

            if (inputHandler.JumpPressed && isGrounded)
            {
                Jump();
                inputHandler.ResetJumpFlag();
            }

            //デバッグ用
            if (UnityEngine.Input.GetKeyDown(KeyCode.Tab))
            {
                if (SceneLoader.CurrentSceneName == "Terrain") SceneLoader.LoadScene("Aquarium");
                if (SceneLoader.CurrentSceneName == "Aquarium") SceneLoader.LoadScene("Terrain");
            }
        }

        private void HandleMove()
        {
            Vector2 move_input = inputHandler.MoveInput;
            Vector3 forward = camTransform.forward;
            Vector3 right = camTransform.right;
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            Vector3 move_direction = (forward * move_input.y + right * move_input.x).normalized;
            Vector3 targetPosition = rb.position + move_direction * moveSpeed * Time.deltaTime;
            rb.MovePosition(targetPosition);
        }

        private void HandleViewRotation()
        {
            Vector2 look_input = inputHandler.LookInput;
            float sensitivity = IsUsingGamepad() ? controllerLookSensitivity : mouseLookSensitivity;
            Vector2 adjusted_look_input = look_input * sensitivity * Time.deltaTime;

            camVerticalRotation -= adjusted_look_input.y;
            camVerticalRotation = Mathf.Clamp(camVerticalRotation, -maxLookUpAngle, maxLookUpAngle);
            camTransform.localRotation = Quaternion.Euler(camVerticalRotation, 0f, 0f);

            transform.Rotate(Vector3.up * adjusted_look_input.x);
        }

        private void Jump()
        {
            rb.AddForce(Vector3.up * jumpStrength, ForceMode.Impulse);
            isGrounded = false;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.CompareTag("Ground"))
            {
                isGrounded = true;
            }
        }

        private bool IsUsingGamepad()
        {
            return Gamepad.current != null && Gamepad.current.enabled;
        }

        private void InteractObject()
        {
            if (RaycastFromCamera(out RaycastHit hit, interactDistance) && hit.collider.TryGetComponent(out IInteractable interactable))
            {
                Debug.Log($"調べた: {interactable.ObjectName}");
                interactable.Interact(this);
                return;
            }
            if (QuickSlot.CurrentEquippedItem != null) UseSelectedItem();
        }

        private void Scan()
        {
            scannerController.Scan(camTransform.position, camTransform.forward);
        }

        private void HandleHPChanged(float current, float max)
        {
            float ratio = current / max;
            playerStatusView.SetHPRatio(ratio);
        }

        private void HandleOxygenChanged(float current, float max)
        {
            float ratio = current / max;
            playerStatusView.SetOxygenRatio(ratio);
        }

        private void HandleDepthChanged(float depth)
        {
            playerStatusView.SetDepth(depth);
        }

        private void Attack()
        {
            InventoryItem item = QuickSlot.CurrentInventoryItem;
            int attack_power = 1;

            if (item != null && item.ItemData.HasAttribute(Item.ItemAttribute.AttackPower))
            {
                attack_power = item.ItemData.GetAttributeValue(Item.ItemAttribute.AttackPower);
            }

            if (RaycastFromCamera(out RaycastHit hit, interactDistance) && hit.collider.TryGetComponent(out IAttackable attackable))
            {
                Debug.Log($"攻撃: {hit.collider.gameObject.name}");
                attackable.Damage(new AttackData(this, attackable, attack_power, AttackType.Melee, transform.position + transform.forward));
            }
        }

        public void Damage(AttackData attack_data)
        {
            model.Damage(attack_data);
            Debug.Log($"{attack_data.Attacker}から{attack_data.Power}ダメージを受けた");

            if (model.IsDead) OnDead();
        }

        public void OnDead()
        {
            Debug.Log("PlayerController: 死亡処理実行");
            inputHandler.DisableInput();
        }

        private bool RaycastFromCamera(out RaycastHit hit, float range)
        {
            int player_layer = LayerMask.NameToLayer("Player");
            int layer_mask = ~(1 << player_layer);

            return Physics.Raycast(camTransform.position, camTransform.forward, out hit, range, layer_mask, QueryTriggerInteraction.Ignore);
        }

        private void ToggleInventory()
        {
            if (uiController.CurrentScreenState == ScreenState.Inventory)
            {
                CloseCurrentScreen();
            }
            else
            {
                if (uiController.CurrentScreenState == ScreenState.Ingame) OpenInventory();
            }
        }

        private void TogglePause()
        {
            if (uiController.CurrentScreenState == ScreenState.Pause)
            {
                CloseCurrentScreen();
            }
            else
            {
                if (uiController.CurrentScreenState == ScreenState.Ingame) OpenPause();
            }
        }

        private void OpenInventory()
        {
            inventoryController.UpdateInventory();
            uiController.ShowScreen(ScreenState.Inventory);
            inputHandler.SetInputMap(InputMapType.Inventory);
        }

        private void OpenPause()
        {
            uiController.ShowScreen(ScreenState.Pause);
            inputHandler.SetInputMap(InputMapType.Menu);
        }

        private void CloseCurrentScreen()
        {
            uiController.HideCurrentScreen();
            inputHandler.SetInputMap(InputMapType.Player);
        }

        private void UseSelectedItem()
        {
            if (!view.CurrentHeldItem.IsUseble()) return;
            
            view.CurrentHeldItem.OnUse(this);
            QuickSlot.Use(QuickSlot.CurrentSlotIndex);
        }

        private void HandleSlotChanged(int index, ItemData item)
        {
            view.ShowHeldItem(item);
        }

        public void CaptureEntity(EntityData captured)
        {
            model.AddCapturedEntity(captured);
            view.AddMessage(new MessageData($"{captured.Name}を捕獲しました"));
        }

        public void OnPickUpItem(ItemData item)
        {
            view.AddMessage(new MessageData($"{item.Name}を入手しました"));
        }
    }
}