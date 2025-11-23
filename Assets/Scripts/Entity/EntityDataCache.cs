using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Blue.Entity
{
    /// <summary>
    /// ランタイムでEntityDataのGUID検索を高速化するキャッシュシステム
    /// </summary>
    public static class EntityDataCache
    {
        private static Dictionary<string, EntityData> guidToEntityCache = new Dictionary<string, EntityData>();
        private static Dictionary<EntityData, string> entityToGuidCache = new Dictionary<EntityData, string>();
        private static bool isInitialized = false;

        /// <summary>
        /// キャッシュを初期化（ゲーム起動時に一度だけ呼び出す）
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            if (isInitialized) return;

#if UNITY_EDITOR
            // エディタではAssetDatabaseから全エンティティを取得
            string[] guids = AssetDatabase.FindAssets("t:EntityData");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                EntityData entity = AssetDatabase.LoadAssetAtPath<EntityData>(path);
                if (entity != null)
                {
                    RegisterEntity(entity, guid);
                }
            }
#else
            // ビルド版ではレジストリから読み込み
            EntityDataRegistry registry = EntityDataRegistry.Instance;
            if (registry != null)
            {
                foreach (EntityData entity in registry.Entities)
                {
                    if (entity != null)
                    {
                        string guid = GetGUIDFromEntity(entity);
                        if (!string.IsNullOrEmpty(guid))
                        {
                            RegisterEntity(entity, guid);
                        }
                    }
                }
            }
#endif

            isInitialized = true;
            Debug.Log($"EntityDataCache initialized with {guidToEntityCache.Count} entities");
        }

        /// <summary>
        /// キャッシュにエンティティを登録
        /// </summary>
        private static void RegisterEntity(EntityData entity, string guid)
        {
            if (entity == null || string.IsNullOrEmpty(guid)) return;

            guidToEntityCache[guid] = entity;
            entityToGuidCache[entity] = guid;
        }

        /// <summary>
        /// GUIDからEntityDataを取得
        /// </summary>
        public static EntityData GetEntityByGUID(string guid)
        {
            if (!isInitialized) Initialize();

            if (guidToEntityCache.TryGetValue(guid, out EntityData entity))
            {
                return entity;
            }

            return null;
        }

        /// <summary>
        /// EntityDataからGUIDを取得
        /// </summary>
        public static string GetGUID(EntityData entity)
        {
            if (!isInitialized) Initialize();

            if (entityToGuidCache.TryGetValue(entity, out string guid))
            {
                return guid;
            }

            return string.Empty;
        }

        /// <summary>
        /// ビルド版でEntityDataからGUIDを取得する方法
        /// </summary>
        private static string GetGUIDFromEntity(EntityData entity)
        {
            // EntityGUIDプロパティが自動的にエディタ/ランタイムを処理
            return entity.EntityGUID;
        }

        /// <summary>
        /// キャッシュをクリア（テストやデバッグ用）
        /// </summary>
        public static void ClearCache()
        {
            guidToEntityCache.Clear();
            entityToGuidCache.Clear();
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
        /// すべてのエンティティを取得
        /// </summary>
        public static IEnumerable<EntityData> GetAllEntities()
        {
            if (!isInitialized) Initialize();
            return guidToEntityCache.Values;
        }
    }
}
