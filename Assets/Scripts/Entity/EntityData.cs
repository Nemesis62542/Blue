using Blue.Item;
using UnityEngine;

namespace Blue.Entity
{
    [CreateAssetMenu(fileName = "EntityData", menuName = "Blue/ScriptableObject/EntityData")]
    public class EntityData : ScriptableObject
    {
        [SerializeField] int id;
        [SerializeField] private new string name;
        [SerializeField] private int hp;
        [SerializeField] private int attackPower;
        [SerializeField] private float displaySize;
        [SerializeField] private HabitationArea habitation;
        [SerializeField] private GameObject @object;
        [SerializeField] private SchoolController school;

        public int ID => id;
        public string Name => name;
        public int HP => hp;
        public int AttackPower => attackPower;
        public float DisplaySize => displaySize;
        public HabitationArea Habitation => habitation;
        public GameObject Object => @object;
        public SchoolController School => school;
    }

    public enum HabitationArea //生息地域
    {
        None,    // なし
        Shallow, // 浅い場所
        Depth,   // 深海
    }
}
