using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Blue.Item;

namespace Blue.UI.Inventory
{
    public class ItemDetailUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI itemNameText;
        [SerializeField] private TextMeshProUGUI itemDescriptionText;
        [SerializeField] private TextMeshProUGUI itemTypeText;
        [SerializeField] private Image itemIcon;

        public static ItemDetailUI Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void ShowItemDetails(ItemData item_data)
        {
            itemNameText.text = item_data.Name;
            itemDescriptionText.text = item_data.Description;
            itemTypeText.text = GenerateItemTypeText(item_data.Type);
            itemIcon.sprite = item_data.Icon;
            
            itemNameText.gameObject.SetActive(true);
            itemDescriptionText.gameObject.SetActive(true);
            itemTypeText.gameObject.SetActive(true);
            itemIcon.gameObject.SetActive(true);
        }

        public void HideItemDetails()
        {
            itemNameText.gameObject.SetActive(false);
            itemDescriptionText.gameObject.SetActive(false);
            itemTypeText.gameObject.SetActive(false);
            itemIcon.gameObject.SetActive(false);
        }

        private string GenerateItemTypeText(ItemType type)
        {
            switch(type)
            {
                case ItemType.Consumable:
                    return "消費アイテム";
                
                case ItemType.Weapon:
                    return "武器";

                case ItemType.Tool:
                    return "道具";

                case ItemType.Ammo:
                    return "弾薬";

                case ItemType.Material:
                    return "素材";

                case ItemType.QuestItem:
                    return "キーアイテム";

                default:
                    return "その他";
            }
        }
    }
}
