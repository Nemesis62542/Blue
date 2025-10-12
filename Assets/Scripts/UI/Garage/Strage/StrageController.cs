using Blue.Inventory;
using Blue.UI.QuickSlot;
using UnityEngine;

namespace Blue.UI.Garage.Strage
{
    public class StrageController : MonoBehaviour
    {
        [SerializeField] private InventoryController strageInventory;
        [SerializeField] private InventoryController playerInventory;
        [SerializeField] private QuickSlotController quickSlot;

        private void Awake()
        {
            Initialize();
            InitializeView();
        }

        private void Initialize()
        {
            //仮の実装で一旦Newする
            InventoryModel strage_inventory = new InventoryModel();
            InventoryModel player_inventory = new InventoryModel();

            strageInventory.Initialize(strage_inventory, null);
            playerInventory.Initialize(player_inventory, null);
            quickSlot.Initialize(new QuickSlotHandler(player_inventory), null);
        }

        private void InitializeView()
        {
            strageInventory.RefreshInventoryUI();
            playerInventory.RefreshInventoryUI();
            quickSlot.RefreshQuickSlotUI();
        }
    }
}