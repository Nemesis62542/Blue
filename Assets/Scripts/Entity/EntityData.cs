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

        public string EntityName => entityName;
        public int HP => hp;
        public int AttackPower => attackPower;
        public float Size => size;
    }
}
