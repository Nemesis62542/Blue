using UnityEngine;
using UnityEditor;
using Blue.Procedural.Coral;

namespace Blue.Editor.Procedural.Coral
{
    /// <summary>
    /// ProceduralCoralGeneratorのカスタムインスペクター
    /// Generate/Clearボタンとアセット保存機能を提供
    /// </summary>
    [CustomEditor(typeof(ProceduralCoralGenerator), true)]
    public class ProceduralCoralGeneratorEditor : UnityEditor.Editor
    {
        private ProceduralCoralGenerator generator;

        private void OnEnable()
        {
            generator = (ProceduralCoralGenerator)target;
        }

        public override void OnInspectorGUI()
        {
            // デフォルトのインスペクターを描画
            DrawDefaultInspector();

            EditorGUILayout.Space(15);

            // 生成ボタンセクション
            DrawGenerationSection();

            EditorGUILayout.Space(10);

            // アセット保存セクション
            DrawSaveSection();

            EditorGUILayout.Space(10);

            // 情報セクション
            DrawInfoSection();
        }

        /// <summary>
        /// 生成ボタンセクション
        /// </summary>
        private void DrawGenerationSection()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Generation", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            // Generateボタン
            GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
            if (GUILayout.Button("Generate", GUILayout.Height(30)))
            {
                Undo.RecordObject(generator.gameObject, "Generate Coral");
                generator.Generate();
                EditorUtility.SetDirty(generator);
                SceneView.RepaintAll();
            }

            // Clearボタン
            GUI.backgroundColor = new Color(0.8f, 0.4f, 0.4f);
            if (GUILayout.Button("Clear", GUILayout.Height(30)))
            {
                Undo.RecordObject(generator.gameObject, "Clear Coral");
                generator.Clear();
                EditorUtility.SetDirty(generator);
                SceneView.RepaintAll();
            }

            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // Randomize Seedボタン
            GUI.backgroundColor = new Color(0.6f, 0.6f, 0.9f);
            if (GUILayout.Button("Randomize Seed & Generate", GUILayout.Height(25)))
            {
                Undo.RecordObject(generator, "Randomize Coral Seed");
                generator.RandomizeSeed();
                EditorUtility.SetDirty(generator);
                SceneView.RepaintAll();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// アセット保存セクション
        /// </summary>
        private void DrawSaveSection()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Save as Asset", EditorStyles.boldLabel);

            bool hasMesh = generator.HasMesh;

            EditorGUI.BeginDisabledGroup(!hasMesh);

            // Save Meshボタン
            if (GUILayout.Button("Save Mesh as Asset", GUILayout.Height(25)))
            {
                CoralMeshSaver.SaveMeshAsset(generator);
            }

            // Save as Prefabボタン
            if (GUILayout.Button("Save as Prefab", GUILayout.Height(25)))
            {
                CoralMeshSaver.SaveAsPrefab(generator);
            }

            EditorGUI.EndDisabledGroup();

            if (!hasMesh)
            {
                EditorGUILayout.HelpBox("Generate a mesh first before saving.", MessageType.Info);
            }

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 情報セクション
        /// </summary>
        private void DrawInfoSection()
        {
            if (!generator.HasMesh) return;

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Mesh Info", EditorStyles.boldLabel);

            Mesh mesh = generator.GeneratedMesh;
            if (mesh != null)
            {
                EditorGUILayout.LabelField($"Vertices: {mesh.vertexCount:N0}");
                EditorGUILayout.LabelField($"Triangles: {mesh.triangles.Length / 3:N0}");
                EditorGUILayout.LabelField($"Bounds: {mesh.bounds.size}");
            }

            EditorGUILayout.EndVertical();
        }
    }
}
