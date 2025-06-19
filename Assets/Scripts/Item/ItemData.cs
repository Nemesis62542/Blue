using UnityEngine;
using System.Collections.Generic;
using System.Text;

namespace Blue.Item
{
    [CreateAssetMenu(fileName = "NewItem", menuName = "Blue/ScriptableObject/ItemData")]
    public class ItemData : ScriptableObject
    {
        [SerializeField] private string itemName;    // アイテムの名前
        [SerializeField] private string description; // 説明
        [SerializeField] private Sprite icon;        // アイコン
        [SerializeField] private ItemType itemType;  // アイテムの種類
        [SerializeField] private bool isStackable;   // スタックできるか
        [SerializeField] private GameObject heldItemPrefab;

        [SerializeField] private List<ItemAttributeData> attributes; // 属性データ

        public string ItemName => itemName;
        public string Description => description;
        public Sprite Icon => icon;
        public ItemType Type => itemType;
        public bool IsStackable => isStackable;
        public GameObject HeldItemPrefab => heldItemPrefab;

        public int GetAttributeValue(ItemAttribute attribute_type)
        {
            foreach (ItemAttributeData attr in attributes)
            {
                if (attr.Attribute == attribute_type)
                {
                    return attr.Value;
                }
            }
            return 0; // デフォルト値
        }

        public bool HasAttribute(ItemAttribute attribute)
        {
            foreach (ItemAttributeData attr in attributes)
            {
                if (attr.Attribute == attribute)
                {
                    return true;
                }
            }
            return false;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"アイテム名: {itemName}");
            sb.AppendLine($"説明: {description}");
            sb.AppendLine($"種類: {itemType}");
            sb.AppendLine($"スタック可: {isStackable}");
            sb.AppendLine("属性:");

            if (attributes.Count > 0)
            {
                foreach (ItemAttributeData attr in attributes)
                {
                    sb.AppendLine($"  - {attr.Attribute}: {attr.Value}");
                }
            }
            else
            {
                sb.AppendLine("  - なし");
            }

            return sb.ToString();
        }
    }

    // アイテムの属性データ
    [System.Serializable]
    public class ItemAttributeData
    {
        [SerializeField] private ItemAttribute attribute; // 属性の種類
        [SerializeField] private int value;              // 属性の値

        public ItemAttribute Attribute => attribute;
        public int Value => value;
    }

    //アイテムの種類
    public enum ItemType
    {
        Consumable,  // 消費アイテム（回復薬など）
        Weapon,      // 武器
        Tool,        // 道具
        Ammo,        // 弾薬
        QuestItem,   // クエストアイテム
        Misc         // その他（特殊アイテムなど）
    }

    //アイテムのデータ値
    public enum ItemAttribute
    {
        AttackPower,  // 武器の攻撃力
        MaxAmmo,      // 銃の装弾数
        HealingValue, // 回復量
        Level,        // アイテムのレベル
    }
}