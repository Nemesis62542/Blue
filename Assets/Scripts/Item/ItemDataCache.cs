using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Blue.Item
{
    /// <summary>
    /// ランタイムでItemDataのGUID検索を高速化するキャッシュシステム
    /// </summary>
    public static class ItemDataCache
    {
        private static Dictionary<string, ItemData> guidToItemCache = new Dictionary<string, ItemData>();
        private static Dictionary<ItemData, string> itemToGuidCache = new Dictionary<ItemData, string>();
        private static bool isInitialized = false;

        /// <summary>
        /// キャッシュを初期化（ゲーム起動時に一度だけ呼び出す）
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            if (isInitialized) return;

#if UNITY_EDITOR
            // エディタではAssetDatabaseから全アイテムを取得
            string[] guids = AssetDatabase.FindAssets("t:ItemData");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
                if (item != null)
                {
                    RegisterItem(item, guid);
                }
            }
#else
            // ビルド版ではResourcesフォルダから読み込み
            // ItemDataをResourcesフォルダに配置する必要があります
            ItemData[] allItems = Resources.LoadAll<ItemData>("Items");
            foreach (ItemData item in allItems)
            {
                if (item != null)
                {
                    // ビルド版ではGUIDをItemDataに埋め込む必要があるため、
                    // 別の方法で管理するか、エディタ時にGUIDをシリアライズする必要があります
                    string guid = GetGUIDFromItem(item);
                    if (!string.IsNullOrEmpty(guid))
                    {
                        RegisterItem(item, guid);
                    }
                }
            }
#endif

            isInitialized = true;
            Debug.Log($"ItemDataCache initialized with {guidToItemCache.Count} items");
        }

        /// <summary>
        /// キャッシュにアイテムを登録
        /// </summary>
        private static void RegisterItem(ItemData item, string guid)
        {
            if (item == null || string.IsNullOrEmpty(guid)) return;

            guidToItemCache[guid] = item;
            itemToGuidCache[item] = guid;
        }

        /// <summary>
        /// GUIDからItemDataを取得
        /// </summary>
        public static ItemData GetItemByGUID(string guid)
        {
            if (!isInitialized) Initialize();

            if (guidToItemCache.TryGetValue(guid, out ItemData item))
            {
                return item;
            }

            return null;
        }

        /// <summary>
        /// ItemDataからGUIDを取得
        /// </summary>
        public static string GetGUID(ItemData item)
        {
            if (!isInitialized) Initialize();

            if (itemToGuidCache.TryGetValue(item, out string guid))
            {
                return guid;
            }

            return string.Empty;
        }

        /// <summary>
        /// ビルド版でItemDataからGUIDを取得する方法
        /// （ItemDataにGUIDをシリアライズしている場合に使用）
        /// </summary>
        private static string GetGUIDFromItem(ItemData item)
        {
#if UNITY_EDITOR
            return item.ItemID;
#else
            // ビルド版ではItemDataにシリアライズされたGUIDを使用
            // この実装は後ほど追加が必要です
            return item.ItemID;
#endif
        }

        /// <summary>
        /// キャッシュをクリア（テストやデバッグ用）
        /// </summary>
        public static void ClearCache()
        {
            guidToItemCache.Clear();
            itemToGuidCache.Clear();
            isInitialized = false;
        }

        /// <summary>
        /// キャッシュを再構築
        /// </summary>
        public static void RebuildCache()
        {
            ClearCache();
            Initialize();
        }

        /// <summary>
        /// すべてのアイテムを取得
        /// </summary>
        public static IEnumerable<ItemData> GetAllItems()
        {
            if (!isInitialized) Initialize();
            return guidToItemCache.Values;
        }
    }
}
