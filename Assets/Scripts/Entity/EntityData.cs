using UnityEngine;

namespace Blue.Entity
{
    [CreateAssetMenu(fileName = "EntityData", menuName = "Blue/ScriptableObject/EntityData")]
    public class EntityData : ScriptableObject
    {
        [SerializeField] private string entityName;
        [SerializeField] private int hp;
        [SerializeField] private int attackPower;

        public string EntityName => entityName;
        public int HP => hp;
        public int AttackPower => attackPower;
    }
}
