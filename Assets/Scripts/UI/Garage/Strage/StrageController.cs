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

        public void Initialize(PlayerInputHandler player_input)
        {
            playerInput = player_input;
            playerInput.SetInputMap(InputMapType.Inventory);

            // セーブデータから読み込み
            strageInventoryModel = SaveDataConverter.LoadStorageInventory();
            playerInventoryModel = SaveDataConverter.LoadPlayerInventory();

            strageInventory.Initialize(strageInventoryModel, playerInput);
            playerInventory.Initialize(playerInventoryModel, playerInput);
            quickSlot.Initialize(new QuickSlotModel());

            // インベントリ変更時に自動保存
            strageInventoryModel.OnValueChanged += OnStorageChanged;
            playerInventoryModel.OnValueChanged += OnPlayerInventoryChanged;

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
        }

        private void OnStorageChanged()
        {
            SaveDataConverter.SaveStorageInventory(strageInventoryModel);
        }

        private void OnPlayerInventoryChanged()
        {
            SaveDataConverter.SavePlayerInventory(playerInventoryModel);
        }

        private void InitializeView()
        {
            strageInventory.RefreshInventoryUI();
            playerInventory.RefreshInventoryUI();
            quickSlot.RefreshQuickSlotUI();
        }
    }
}