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
            itemNameText.text = item_data.ItemName;
            itemDescriptionText.text = item_data.Description;
            itemTypeText.text = item_data.Type.ToString();
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
    }
}
