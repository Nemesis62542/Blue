using Blue.Entity;
using Blue.Item;
using UnityEngine;

namespace Blue.Game
{
    /// <summary>
    /// Resources.Load依存を減らすため、レジストリ参照を明示注入するブートストラップ
    /// </summary>
    public class DataRegistryBootstrap : MonoBehaviour
    {
        [SerializeField] private ItemDataRegistry itemDataRegistry;
        [SerializeField] private EntityDataRegistry entityDataRegistry;

        private void Awake()
        {
            if (itemDataRegistry != null)
            {
                ItemDataCache.SetRegistry(itemDataRegistry);
            }

            if (entityDataRegistry != null)
            {
                EntityDataCache.SetRegistry(entityDataRegistry);
            }
        }
    }
}
