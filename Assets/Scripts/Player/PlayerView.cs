using UnityEngine;
using Blue.Item;
using Blue.Entity;
using Blue.UI;

namespace Blue.Player
{
    public class PlayerView : BaseEntityView
    {
        [SerializeField] private Transform heldItemAnchor;
        [SerializeField] private MessageView messageView;

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
            currentHeldItem.transform.localPosition = Vector3.zero;
            currentHeldItem.transform.localRotation = Quaternion.identity;
        }

        public void AddMessage(MessageData data)
        {
            messageView.ShowMessage(data);
        }
    }
}
