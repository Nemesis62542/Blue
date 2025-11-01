using UnityEngine;
using System.Collections.Generic;
using System.Text;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Blue.Item
{
    [CreateAssetMenu(fileName = "NewItem", menuName = "Blue/ScriptableObject/ItemData")]
    public class ItemData : ScriptableObject
    {
        [SerializeField] private new string name;    // アイテムの名前
        [SerializeField, TextArea] private string description; // 説明
        [SerializeField] private Sprite icon;        // アイコン
        [SerializeField] private ItemType type;  // アイテムの種類
        [SerializeField] private bool isStackable;   // スタックできるか
        [SerializeField] private ItemUseHandler heldItemPrefab;

        [SerializeField] private List<ItemAttributeData> attributes; // 属性データ

        [SerializeField, HideInInspector] private string cachedGUID; // ビルド版用のキャッシュGUID

        public string Name => name;
        public string Description => description;
        public Sprite Icon => icon;
        public ItemType Type => type;
        public bool IsStackable => isStackable;
        public ItemUseHandler HeldItemPrefab => heldItemPrefab;

        // アイテムの一意なID（GUIDベース）
        public string ItemID
        {
            get
            {
#if UNITY_EDITOR
                string assetPath = AssetDatabase.GetAssetPath(this);
                if (!string.IsNullOrEmpty(assetPath))
                {
                    string guid = AssetDatabase.AssetPathToGUID(assetPath);
                    // エディタではGUIDをキャッシュに保存（ビルド時に使用）
                    if (cachedGUID != guid)
                    {
                        cachedGUID = guid;
                        EditorUtility.SetDirty(this);
                    }
                    return guid;
                }
#else
                // ビルド版ではキャッシュされたGUIDを返す
                return cachedGUID;
#endif
                return string.Empty;
            }
        }

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
            sb.AppendLine($"アイテム名: {name}");
            sb.AppendLine($"説明: {description}");
            sb.AppendLine($"種類: {type}");
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
        Material,    // 素材
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
        CoolDown,     // アイテムクールダウン(ミリ秒想定)
    }
}