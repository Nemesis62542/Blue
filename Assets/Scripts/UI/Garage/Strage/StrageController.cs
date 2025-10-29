using Blue.Input;
using Blue.Inventory;
using Blue.Item;
using Blue.Save;
using Blue.UI.QuickSlot;
using UnityEngine;

namespace Blue.UI.Garage.Strage
{
    public class StrageController : MonoBehaviour
    {
        [SerializeField] private InventoryController strageInventory;
        [SerializeField] private InventoryController playerInventory;
        [SerializeField] private QuickSlotController quickSlot;

        private PlayerInputHandler playerInput;
        private InventoryModel strageInventoryModel;
        private InventoryModel playerInventoryModel;
        private QuickSlotModel quickSlotModel;

        public void Initialize(PlayerInputHandler player_input)
        {
            playerInput = player_input;
            playerInput.SetInputMap(InputMapType.Inventory);

            // セーブデータから読み込み
            strageInventoryModel = SaveDataConverter.LoadStorageInventory();
            playerInventoryModel = SaveDataConverter.LoadPlayerInventory();
            quickSlotModel = SaveDataConverter.LoadQuickSlot();

            strageInventory.Initialize(strageInventoryModel, playerInput);
            playerInventory.Initialize(playerInventoryModel, playerInput);
            quickSlot.Initialize(quickSlotModel);

            // インベントリ変更時に自動保存
            strageInventoryModel.OnValueChanged += OnStorageChanged;
            playerInventoryModel.OnValueChanged += OnPlayerInventoryChanged;
            quickSlotModel.OnQuickSlotUpdated += OnQuickSlotChanged;

            InitializeView();
        }

        private void OnDestroy()
        {
            // イベント解除
            if (strageInventoryModel != null)
            {
                strageInventoryModel.OnValueChanged -= OnStorageChanged;
            }
            if (playerInventoryModel != null)
            {
                playerInventoryModel.OnValueChanged -= OnPlayerInventoryChanged;
            }
            if (quickSlotModel != null)
            {
                quickSlotModel.OnQuickSlotUpdated -= OnQuickSlotChanged;
            }
        }

        private void OnStorageChanged()
        {
            SaveDataConverter.SaveStorageInventory(strageInventoryModel);
        }

        private void OnPlayerInventoryChanged()
        {
            SaveDataConverter.SavePlayerInventory(playerInventoryModel);
        }

        private void OnQuickSlotChanged()
        {
            SaveDataConverter.SaveQuickSlot(quickSlotModel);
        }

        private void InitializeView()
        {
            strageInventory.RefreshInventoryUI();
            playerInventory.RefreshInventoryUI();
            quickSlot.RefreshQuickSlotUI();
        }
    }
}