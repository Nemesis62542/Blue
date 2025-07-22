using Blue.Item;
using UnityEngine;

namespace Blue.Entity
{
    [CreateAssetMenu(fileName = "EntityData", menuName = "Blue/ScriptableObject/EntityData")]
    public class EntityData : ScriptableObject
    {
        [SerializeField] private new string name;
        [SerializeField] private int hp;
        [SerializeField] private int attackPower;
        [SerializeField] private float size;
        [SerializeField] private GameObject @object;
        [SerializeField] private SchoolController school;

        public string Name => name;
        public int HP => hp;
        public int AttackPower => attackPower;
        public float Size => size;
        public GameObject Object => @object;
        public SchoolController School => school;
    }
}
