using System.Collections.Generic;
using Blue.World.Scatter;
using UnityEditor;
using UnityEngine;

namespace Blue.Editor.World
{
    /// <summary>
    /// ScatterPrototypeRegistry のカスタムインスペクター。
    /// </summary>
    // インスタンシング描画にはメッシュとマテリアルが要るが、プレハブから手で取り出すのは
    // FBX を展開する手間がかかる。プレハブを指定すれば埋められるようにする。
    [CustomEditor(typeof(ScatterPrototypeRegistry))]
    public class ScatterPrototypeRegistryEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            ScatterPrototypeRegistry registry = (ScatterPrototypeRegistry)target;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Tools", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "prefab を設定した要素について、そこから Mesh と Material を取り出して " +
                "lodMeshes / material に埋めます。既に入っている値は上書きしません。",
                MessageType.None);

            if (GUILayout.Button("Fill Meshes And Materials From Prefabs", GUILayout.Height(26)))
            {
                FillFromPrefabs(registry);
            }

            List<string> issues = Validate(registry);
            if (issues.Count > 0)
            {
                EditorGUILayout.HelpBox("・" + string.Join("\n・", issues), MessageType.Warning);
            }
        }

        private static void FillFromPrefabs(ScatterPrototypeRegistry registry)
        {
            SerializedObject serializedObject = new SerializedObject(registry);
            SerializedProperty prototypes = serializedObject.FindProperty("prototypes");
            int filled = 0;

            for (int i = 0; i < prototypes.arraySize; i++)
            {
                SerializedProperty prototype = prototypes.GetArrayElementAtIndex(i);
                SerializedProperty prefabProperty = prototype.FindPropertyRelative("prefab");

                if (prefabProperty.objectReferenceValue is not GameObject prefab)
                {
                    continue;
                }

                MeshFilter meshFilter = prefab.GetComponentInChildren<MeshFilter>(true);
                MeshRenderer meshRenderer = prefab.GetComponentInChildren<MeshRenderer>(true);

                if (meshFilter == null || meshFilter.sharedMesh == null)
                {
                    continue;
                }

                SerializedProperty lodMeshes = prototype.FindPropertyRelative("lodMeshes");
                if (lodMeshes.arraySize == 0)
                {
                    lodMeshes.InsertArrayElementAtIndex(0);
                    lodMeshes.GetArrayElementAtIndex(0).objectReferenceValue = meshFilter.sharedMesh;
                    filled++;
                }
                else if (lodMeshes.GetArrayElementAtIndex(0).objectReferenceValue == null)
                {
                    lodMeshes.GetArrayElementAtIndex(0).objectReferenceValue = meshFilter.sharedMesh;
                    filled++;
                }

                SerializedProperty materialProperty = prototype.FindPropertyRelative("material");
                if (materialProperty.objectReferenceValue == null &&
                    meshRenderer != null && meshRenderer.sharedMaterial != null)
                {
                    materialProperty.objectReferenceValue = meshRenderer.sharedMaterial;
                    filled++;
                }
            }

            serializedObject.ApplyModifiedProperties();
            Debug.Log($"[ScatterPrototypeRegistry] プレハブから {filled} 件を埋めました。", registry);
        }

        private static List<string> Validate(ScatterPrototypeRegistry registry)
        {
            List<string> issues = new List<string>();

            if (!registry.ValidateIds(out string idError))
            {
                issues.Add(idError);
            }

            foreach (ScatterPrototype prototype in registry.Prototypes)
            {
                if (prototype == null)
                {
                    continue;
                }

                string label = string.IsNullOrEmpty(prototype.displayName)
                    ? $"id {prototype.id}"
                    : prototype.displayName;

                if (prototype.id == 0)
                {
                    issues.Add($"{label}: id が 0 です。0 は未設定と紛らわしいので 1 以上を推奨します。");
                }

                bool hasMesh = prototype.lodMeshes != null && prototype.lodMeshes.Length > 0 &&
                               prototype.lodMeshes[0] != null;

                if (!prototype.instantiate && !hasMesh)
                {
                    issues.Add($"{label}: lodMeshes が空です。インスタンシング描画にはメッシュが必要です。");
                }

                if (!prototype.instantiate && prototype.material == null)
                {
                    issues.Add($"{label}: material が未設定です。");
                }

                if (prototype.material != null && !prototype.material.enableInstancing)
                {
                    issues.Add($"{label}: マテリアル '{prototype.material.name}' の Enable GPU Instancing が無効です。");
                }

                if (prototype.instantiate && prototype.prefab == null)
                {
                    issues.Add($"{label}: instantiate が有効ですが prefab が未設定です。");
                }
            }

            return issues;
        }
    }
}
