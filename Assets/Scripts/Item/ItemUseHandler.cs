using UnityEngine;

namespace Blue.Item
{
    public class ItemUseHandler : MonoBehaviour
    {
        protected ItemData itemData;

        private float coolDown = 0f;

        public virtual bool IsUseble()
        {
            return coolDown <= 0;
        }

        public virtual void Initialize(ItemData item_data)
        {
            itemData = item_data;
        }

        public virtual void OnUse(MonoBehaviour user)
        {
            coolDown = (float)itemData.GetAttributeValue(ItemAttribute.CoolDown) / 1000;
        }

        void Update()
        {
            coolDown = Mathf.Max(0f, coolDown - Time.deltaTime);
        }
    }
}