using UnityEngine;
using Blue.Item;
using Blue.Entity;

namespace Blue.Player
{
    public class PlayerView : BaseEntityView
    {
        [SerializeField] private Transform heldItemAnchor;

        private GameObject currentHeldItem;

        public Transform HeldItemAnchor
        {
            get => heldItemAnchor;
            set => heldItemAnchor = value;
        }

        public void ShowHeldItem(ItemData item)
        {
            if (currentHeldItem != null)
            {
                Destroy(currentHeldItem);
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
    }
}
