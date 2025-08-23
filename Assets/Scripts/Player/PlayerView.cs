using UnityEngine;
using Blue.Item;
using Blue.Entity;
using Blue.UI;
using TMPro;

namespace Blue.Player
{
    public class PlayerView : BaseEntityView
    {
        [SerializeField] private Transform heldItemAnchor;
        [SerializeField] private TMP_Text inspectText;

        private ItemUseHandler currentHeldItem;

        public ItemUseHandler CurrentHeldItem => currentHeldItem;

        public void ShowHeldItem(ItemData item)
        {
            if (currentHeldItem != null)
            {
                Destroy(currentHeldItem.gameObject);
                currentHeldItem = null;
            }

            if (item?.HeldItemPrefab == null)
            {
                return;
            }

            currentHeldItem = Instantiate(item.HeldItemPrefab, heldItemAnchor);
            currentHeldItem.Initialize(item);
        }

        public void AddMessage(MessageData data)
        {
            MessageView.Instance.ShowMessage(data);
        }

        public void SetInspectText(string text)
        {
            inspectText.text = text;
        }
    }
}
