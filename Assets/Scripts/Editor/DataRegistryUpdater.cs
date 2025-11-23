using Blue.Entity;
using Blue.Item;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Blue.Editor
{
    /// <summary>
    /// EntityDataRegistry と ItemDataRegistry を自動更新するエディタユーティリティ
    /// </summary>
    public static class DataRegistryUpdater
    {
        private const string ENTITY_REGISTRY_PATH = "Assets/Resources/EntityDataRegistry.asset";
        private const string ITEM_REGISTRY_PATH = "Assets/Resources/ItemDataRegistry.asset";

        /// <summary>
        /// 全てのレジストリを更新
        /// </summary>
        [MenuItem("Blue/Update All Data Registries")]
        public static void UpdateAllRegistries()
        {
            UpdateEntityDataRegistry();
            UpdateItemDataRegistry();
            Debug.Log("All data registries updated successfully!");
        }

        /// <summary>
        /// EntityDataRegistryを更新
        /// </summary>
        [MenuItem("Blue/Update EntityData Registry")]
        public static void UpdateEntityDataRegistry()
        {
            // Resourcesフォルダが存在するか確認
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }

            // レジストリを取得または作成
            EntityDataRegistry registry = AssetDatabase.LoadAssetAtPath<EntityDataRegistry>(ENTITY_REGISTRY_PATH);
            if (registry == null)
            {
                registry = ScriptableObject.CreateInstance<EntityDataRegistry>();
                AssetDatabase.CreateAsset(registry, ENTITY_REGISTRY_PATH);
            }

            // SerializedObjectを使ってプライベートフィールドにアクセス
            SerializedObject serialized_object = new SerializedObject(registry);
            SerializedProperty entities_property = serialized_object.FindProperty("entities");

            // リストをクリア
            entities_property.ClearArray();

            // 全てのEntityDataを検索して追加
            string[] guids = AssetDatabase.FindAssets("t:EntityData");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                EntityData entity = AssetDatabase.LoadAssetAtPath<EntityData>(path);

                if (entity != null)
                {
                    // GUIDをキャッシュに保存するためにEntityGUIDプロパティにアクセス
                    _ = entity.EntityGUID;

                    // レジストリに追加
                    int index = entities_property.arraySize;
                    entities_property.InsertArrayElementAtIndex(index);
                    entities_property.GetArrayElementAtIndex(index).objectReferenceValue = entity;
                }
            }

            serialized_object.ApplyModifiedProperties();
            EditorUtility.SetDirty(registry);
            AssetDatabase.SaveAssets();

            Debug.Log($"EntityDataRegistry updated with {guids.Length} entities at {ENTITY_REGISTRY_PATH}");
        }

        /// <summary>
        /// ItemDataRegistryを更新
        /// </summary>
        [MenuItem("Blue/Update ItemData Registry")]
        public static void UpdateItemDataRegistry()
        {
            // Resourcesフォルダが存在するか確認
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }

            // レジストリを取得または作成
            ItemDataRegistry registry = AssetDatabase.LoadAssetAtPath<ItemDataRegistry>(ITEM_REGISTRY_PATH);
            if (registry == null)
            {
                registry = ScriptableObject.CreateInstance<ItemDataRegistry>();
                AssetDatabase.CreateAsset(registry, ITEM_REGISTRY_PATH);
            }

            // SerializedObjectを使ってプライベートフィールドにアクセス
            SerializedObject serialized_object = new SerializedObject(registry);
            SerializedProperty items_property = serialized_object.FindProperty("items");

            // リストをクリア
            items_property.ClearArray();

            // 全てのItemDataを検索して追加
            string[] guids = AssetDatabase.FindAssets("t:ItemData");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(path);

                if (item != null)
                {
                    // GUIDをキャッシュに保存するためにItemIDプロパティにアクセス
                    _ = item.ItemID;

                    // レジストリに追加
                    int index = items_property.arraySize;
                    items_property.InsertArrayElementAtIndex(index);
                    items_property.GetArrayElementAtIndex(index).objectReferenceValue = item;
                }
            }

            serialized_object.ApplyModifiedProperties();
            EditorUtility.SetDirty(registry);
            AssetDatabase.SaveAssets();

            Debug.Log($"ItemDataRegistry updated with {guids.Length} items at {ITEM_REGISTRY_PATH}");
        }

        /// <summary>
        /// ビルド前に自動的にレジストリを更新
        /// </summary>
        private class BuildPreprocessor : UnityEditor.Build.IPreprocessBuildWithReport
        {
            public int callbackOrder => 0;

            public void OnPreprocessBuild(UnityEditor.Build.Reporting.BuildReport report)
            {
                UpdateAllRegistries();
            }
        }
    }
}
