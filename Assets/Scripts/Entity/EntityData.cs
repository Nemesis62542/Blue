using Blue.Item;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Blue.Entity
{
    [CreateAssetMenu(fileName = "EntityData", menuName = "Blue/ScriptableObject/EntityData")]
    public class EntityData : ScriptableObject
    {
        [SerializeField] int id;
        [SerializeField, HideInInspector] private string cachedGUID;
        [SerializeField] private new string name;
        [SerializeField] private int hp;
        [SerializeField] private int attackPower;
        [SerializeField] private float displaySize;
        [SerializeField] private HabitationArea habitation;
        [SerializeField] private GameObject @object;
        [SerializeField] private SchoolController school;

        public int ID => id;

        /// <summary>
        /// エンティティの一意なID（GUIDベース）
        /// </summary>
        public string EntityGUID
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
