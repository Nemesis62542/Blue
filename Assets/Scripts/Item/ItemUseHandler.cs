using UnityEngine;

namespace Blue.Item
{
    public abstract class ItemUseHandler : MonoBehaviour
    {
        protected ItemData itemData;

        [SerializeField] private bool destroyAfterUse = true;
        [SerializeField] private int useCountRemaining = 1; // -1 = 無限使用, 0 = 即破棄

        public virtual void Initialize(ItemData item_data)
        {
            itemData = item_data;
        }

        public void Execute()
        {
            Use();

            if (!destroyAfterUse) return;

            if (useCountRemaining > 0)
            {
                useCountRemaining--;
            }

            if (useCountRemaining == 0)
            {
                OnUseLimitReached();
            }
        }

        protected abstract void Use();

        /// <summary>
        /// 使用回数がなくなった時(useCountRemining=0)のときの処理
        /// </summary>
        protected virtual void OnUseLimitReached()
        {
            Destroy(gameObject);
        }
    }
}