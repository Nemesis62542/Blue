using UnityEngine;

namespace Blue.Item
{
    public abstract class ItemUseHandler : MonoBehaviour
    {
        protected ItemData itemData;

        public virtual void Initialize(ItemData item_data)
        {
            itemData = item_data;
        }

        public abstract void Use();
    }
}
