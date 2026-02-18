using System.Collections.Generic;
using UnityEngine;

namespace Blue.Entity
{
    /// <summary>
    /// 全てのEntityDataへの参照を保持するレジストリ
    /// ビルド版でEntityDataを読み込むために使用
    /// </summary>
    [CreateAssetMenu(fileName = "EntityDataRegistry", menuName = "Blue/ScriptableObject/EntityDataRegistry")]
    public class EntityDataRegistry : ScriptableObject
    {
        private static EntityDataRegistry instance;

        [SerializeField] private List<EntityData> entities = new List<EntityData>();

        /// <summary>
        /// シングルトンインスタンスを取得
        /// </summary>
        public static EntityDataRegistry Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Resources.Load<EntityDataRegistry>("EntityDataRegistry");
                    if (instance == null)
                    {
                        Debug.LogError("EntityDataRegistry not found in Resources folder! Please create one at Assets/Resources/EntityDataRegistry.asset");
                    }
                }
                return instance;
            }
        }

        /// <summary>
        /// 登録されている全てのEntityDataを取得
        /// </summary>
        public IReadOnlyList<EntityData> Entities => entities;
    }
}
