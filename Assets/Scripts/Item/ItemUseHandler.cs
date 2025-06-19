using UnityEngine;

namespace Blue.Item
{
    public class ItemUseHandler : MonoBehaviour
    {
        protected ItemData itemData;

        public virtual void Initialize(ItemData item_data)
        {
            itemData = item_data;
        }

        public virtual void OnUse(MonoBehaviour user)
        {

        }
    }
}