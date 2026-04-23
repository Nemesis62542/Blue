using UnityEngine;
using UnityEditor;
using Blue.Procedural.Coral;

namespace Blue.Editor.Procedural.Coral
{
    /// <summary>
    /// プロシージャルサンゴのMesh/Prefab保存ユーティリティ
    /// </summary>
    public static class CoralMeshSaver
    {
        private const string MESH_FOLDER = "Assets/Mesh/ProceduralCoral";
        private const string PREFAB_FOLDER = "Assets/Prefab/Object/ProceduralCoral";

        /// <summary>
        /// MeshをアセットとしてMeshを保存
        /// </summary>
        public static void SaveMeshAsset(ProceduralCoralGenerator generator)
        {
            MeshFilter meshFilter = generator.GetComponent<MeshFilter>();
            Mesh mesh = meshFilter?.sharedMesh;

            if (mesh == null)
            {
                EditorUtility.DisplayDialog("Error", "Meshが生成されていません。\n先に「Generate」を実行してください。", "OK");
                return;
            }

            // フォルダ確認・作成
            EnsureFolder(MESH_FOLDER);

            // ファイル名生成
            string meshName = $"{generator.CoralTypeName}_Seed{generator.Seed}";
            string path = $"{MESH_FOLDER}/{meshName}.asset";
            path = AssetDatabase.GenerateUniqueAssetPath(path);

            // Meshをクローン（シーン上のMeshはアセットではないため）
            Mesh meshClone = UnityEngine.Object.Instantiate(mesh);
            meshClone.name = meshName;

            // アセットとして保存
            AssetDatabase.CreateAsset(meshClone, path);
            AssetDatabase.SaveAssets();

            // 保存したMeshをコンポーネントに設定
            Undo.RecordObject(meshFilter, "Apply Saved Mesh");
            meshFilter.sharedMesh = meshClone;
            EditorUtility.SetDirty(meshFilter);

            Debug.Log($"[CoralMeshSaver] Meshを保存しました: {path}");
            EditorUtility.DisplayDialog("Save Complete", $"Meshを保存しました:\n{path}", "OK");
            EditorGUIUtility.PingObject(meshClone);
        }

        /// <summary>
        /// プレハブとして保存
        /// </summary>
        public static void SaveAsPrefab(ProceduralCoralGenerator generator)
        {
            MeshFilter meshFilter = generator.GetComponent<MeshFilter>();
            Mesh mesh = meshFilter?.sharedMesh;

            if (mesh == null)
            {
                EditorUtility.DisplayDialog("Error", "Meshが生成されていません。\n先に「Generate」を実行してください。", "OK");
                return;
            }

            // Meshがアセットでない場合は先に保存
            if (!AssetDatabase.Contains(mesh))
            {
                if (!EditorUtility.DisplayDialog(
                    "Save Mesh First",
                    "Meshがまだアセットとして保存されていません。\n先にMeshを保存しますか？",
                    "Save Mesh", "Cancel"))
                {
                    return;
                }

                SaveMeshAsset(generator);

                // 保存後のMeshを取得
                mesh = meshFilter.sharedMesh;
                if (!AssetDatabase.Contains(mesh))
                {
                    Debug.LogError("[CoralMeshSaver] Meshの保存に失敗しました。");
                    return;
                }
            }

            // プレハブフォルダを確保
            EnsureFolder(PREFAB_FOLDER);

            // プレハブ名を生成
            string prefabName = $"Coral_{generator.CoralTypeName}_Seed{generator.Seed}";
            string path = $"{PREFAB_FOLDER}/{prefabName}.prefab";
            path = AssetDatabase.GenerateUniqueAssetPath(path);

            // 一時的なGameObjectを作成（Generatorコンポーネントを除外）
            GameObject tempGO = new GameObject(prefabName);

            try
            {
                // MeshFilter
                MeshFilter newMeshFilter = tempGO.AddComponent<MeshFilter>();
                newMeshFilter.sharedMesh = mesh;

                // MeshRenderer
                MeshRenderer originalRenderer = generator.GetComponent<MeshRenderer>();
                MeshRenderer newRenderer = tempGO.AddComponent<MeshRenderer>();
                if (originalRenderer != null)
                {
                    newRenderer.sharedMaterials = originalRenderer.sharedMaterials;
                    newRenderer.shadowCastingMode = originalRenderer.shadowCastingMode;
                    newRenderer.receiveShadows = originalRenderer.receiveShadows;
                }

                // MeshCollider（オプション）
                if (EditorUtility.DisplayDialog(
                    "Add MeshCollider?",
                    "MeshColliderを追加しますか？",
                    "Add", "Skip"))
                {
                    MeshCollider collider = tempGO.AddComponent<MeshCollider>();
                    collider.sharedMesh = mesh;
                }

                // プレハブとして保存
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(tempGO, path);

                Debug.Log($"[CoralMeshSaver] Prefabを保存しました: {path}");
                EditorUtility.DisplayDialog("Save Complete", $"Prefabを保存しました:\n{path}", "OK");
                EditorGUIUtility.PingObject(prefab);
            }
            finally
            {
                // 一時オブジェクトを削除
                UnityEngine.Object.DestroyImmediate(tempGO);
            }
        }

        /// <summary>
        /// フォルダが存在しない場合は作成
        /// </summary>
        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;

            string[] folders = path.Split('/');
            string current = folders[0]; // "Assets"

            for (int i = 1; i < folders.Length; i++)
            {
                string next = current + "/" + folders[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, folders[i]);
                    Debug.Log($"[CoralMeshSaver] フォルダを作成しました: {next}");
                }
                current = next;
            }
        }
    }
}
