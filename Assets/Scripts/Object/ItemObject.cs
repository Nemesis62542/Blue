using Blue.Inventory;
using Blue.Item;
using UnityEngine;

namespace Blue.Object
{
    public class ItemObject : MonoBehaviour, IInteractable
    {
        [SerializeField] private ItemData itemData;

        public string ObjectName => itemData != null ? itemData.ItemName : "不明なアイテム";
        public ItemData ItemData => itemData;

        public void Interact(MonoBehaviour interactor)
        {
            if (itemData == null)
            {
                Debug.LogWarning("アイテムデータが設定されていません！");
                return;
            }

            if (interactor is IInventoryHolder inventoryHolder)
            {
                inventoryHolder.Inventory.AddItem(itemData, 1);
                Debug.Log($"{interactor.name} がアイテムを取得: {itemData.ItemName}");
                Destroy(gameObject);
            }
            else
            {
                Debug.LogWarning($"{interactor.name} はインベントリを持っていないため、アイテムを取得できません。");
            }
        }
    }
}