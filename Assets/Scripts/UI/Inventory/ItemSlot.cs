using UnityEngine;
using UnityEngine.UI;
using TMPro;
using NFPS.Item;
using UnityEngine.EventSystems;

namespace NFPS.UI.Inventory
{
    public class ItemSlot : MonoBehaviour
    {
        [SerializeField] private Image itemIcon;
        [SerializeField] private TextMeshProUGUI itemCountText;
        [SerializeField] private RectTransform hoverArea;
        private ItemData currentItemData;
        private int currentItemCount;

        public ItemData CurrentItem => currentItemData;
        public RectTransform HoverArea => hoverArea;

        public void SetItem(ItemData item_data, int count)
        {
            currentItemData = item_data;
            currentItemCount = count;
            if(item_data!= null) itemIcon.sprite = item_data.Icon;
            else itemIcon.gameObject.SetActive(false);
            itemCountText.text = count > 1 ? count.ToString() : "";
        }

        public void ShowItemDetails()
        {
            if (currentItemData != null)
            {
                ItemDetailUI.Instance.ShowItemDetails(currentItemData);
            }
        }

        public void HideItemDetails()
        {
            ItemDetailUI.Instance.HideItemDetails();
        }

        public void OnClick()
        {
            EventSystem.current.SetSelectedGameObject(gameObject);
        }
    }
}
