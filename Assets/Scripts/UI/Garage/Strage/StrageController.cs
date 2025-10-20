using Blue.Input;
using Blue.Inventory;
using Blue.Item;
using Blue.UI.QuickSlot;
using Blue.UI.DragAndDrop;
using UnityEngine;

namespace Blue.UI.Garage.Strage
{
    public class StrageController : MonoBehaviour
    {
        [SerializeField] private InventoryController strageInventory;
        [SerializeField] private InventoryController playerInventory;
        [SerializeField] private QuickSlotController quickSlot;
        [SerializeField] private GenericItemDropHandler strageDropHandler;
        [SerializeField] private GenericItemDropHandler playerDropHandler;
        [SerializeField] private ItemData item;

        private PlayerInputHandler playerInput;

        public void Initialize(PlayerInputHandler player_input)
        {
            playerInput = player_input;
            playerInput.SetInputMap(InputMapType.Inventory);

            //仮の実装で一旦Newする
            InventoryModel strage_inventory = new InventoryModel();
            InventoryModel player_inventory = new InventoryModel();
            //デバッグ用にアイテムを追加
            player_inventory.AddItem(item);

            strageInventory.Initialize(strage_inventory, playerInput);
            playerInventory.Initialize(player_inventory, playerInput);
            quickSlot.Initialize(new QuickSlotModel());

            if (strageDropHandler != null)
            {
                strageDropHandler.Setup(strageInventory);
            }
            if (playerDropHandler != null)
            {
                playerDropHandler.Setup(playerInventory);
            }

            InitializeView();
        }

        private void InitializeView()
        {
            strageInventory.RefreshInventoryUI();
            playerInventory.RefreshInventoryUI();
            quickSlot.RefreshQuickSlotUI();
        }
    }
}