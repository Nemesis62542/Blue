using Blue.Item;
using UnityEngine;

namespace Blue.Entity
{
    [CreateAssetMenu(fileName = "EntityData", menuName = "Blue/ScriptableObject/EntityData")]
    public class EntityData : ScriptableObject
    {
        [SerializeField] private string entityName;
        [SerializeField] private int hp;
        [SerializeField] private int attackPower;
        [SerializeField] private float size;
        [SerializeField] private ItemData capturedItem; // 捕獲時にインベントリへ追加されるアイテム
        [SerializeField] private GameObject entityObject;

        public string EntityName => entityName;
        public int HP => hp;
        public int AttackPower => attackPower;
        public float Size => size;
        public ItemData CapturedItem => capturedItem;
        public GameObject EntityObject => entityObject;
    }
}
