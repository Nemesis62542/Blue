using System.Collections.Generic;
using UnityEngine;

namespace Blue.Item
{
    /// <summary>
    /// 全てのItemDataへの参照を保持するレジストリ
    /// ビルド版でItemDataを読み込むために使用
    /// </summary>
    [CreateAssetMenu(fileName = "ItemDataRegistry", menuName = "Blue/ScriptableObject/ItemDataRegistry")]
    public class ItemDataRegistry : ScriptableObject
    {
        private static ItemDataRegistry instance;

        [SerializeField] private List<ItemData> items = new List<ItemData>();

        /// <summary>
        /// シングルトンインスタンスを取得
        /// </summary>
        public static ItemDataRegistry Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Resources.Load<ItemDataRegistry>("ItemDataRegistry");
                    if (instance == null)
                    {
                        Debug.LogError("ItemDataRegistry not found in Resources folder! Please create one at Assets/Resources/ItemDataRegistry.asset");
                    }
                }
                return instance;
            }
        }

        /// <summary>
        /// 登録されている全てのItemDataを取得
        /// </summary>
        public IReadOnlyList<ItemData> Items => items;
    }
}
