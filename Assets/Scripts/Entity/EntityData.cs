using UnityEngine;

namespace NFPS.Entity
{
    [CreateAssetMenu(fileName = "EntityData", menuName = "NFPS/ScriptableObject/EntityData")]
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
