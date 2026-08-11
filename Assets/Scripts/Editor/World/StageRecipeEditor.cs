using System.Collections.Generic;
using Blue.World;
using UnityEditor;
using UnityEngine;

namespace Blue.Editor.World
{
    /// <summary>
    /// StageRecipe のカスタムインスペクター。
    /// ベイク前に構成の要約と検証結果を出して、64タイル回してから気づく事故を防ぐ。
    /// </summary>
    [CustomEditor(typeof(StageRecipe))]
    public class StageRecipeEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            StageRecipe recipe = (StageRecipe)target;
            StageTileLayout layout = recipe.Layout;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Bake", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                $"ワールド: {layout.WorldSize:F0}m 四方 / 高さ {layout.MinHeight:F0}〜{layout.MaxHeight:F0}m ({layout.HeightRange:F0}m)\n" +
                $"タイル: {layout.TilesPerAxis}x{layout.TilesPerAxis} = {layout.TileCount} 枚 (1枚 {layout.TileSize:F0}m)\n" +
                $"ハイトマップ: {layout.HeightmapResolution}/タイル → 必要な元画像 {layout.GlobalHeightSamples}x{layout.GlobalHeightSamples} " +
                $"({layout.TileSize / (layout.HeightmapResolution - 1):F2}m/texel)\n" +
                $"アルファマップ: {layout.AlphamapResolution}/タイル",
                MessageType.None);

            List<string> errors = recipe.Validate();
            if (errors.Count > 0)
            {
                EditorGUILayout.HelpBox("・" + string.Join("\n・", errors), MessageType.Error);
            }
            else
            {
                List<string> warnings = recipe.CollectWarnings();
                if (warnings.Count > 0)
                {
                    EditorGUILayout.HelpBox("・" + string.Join("\n・", warnings), MessageType.Warning);
                }
            }

            EditorGUILayout.HelpBox(
                "再ベイクしても TerrainData の GUID は維持されるため、Digger で掘った洞窟は保持されます。" +
                "ただしハイトマップ自体を変更した場合、地形が動いた箇所の掘削結果はずれます。",
                MessageType.Info);

            using (new EditorGUI.DisabledScope(errors.Count > 0))
            {
                if (GUILayout.Button("Bake Stage Terrain", GUILayout.Height(30)))
                {
                    StageTerrainBaker.Bake(recipe);
                }
            }

            DrawScatterSection(recipe);
        }

        private void DrawScatterSection(StageRecipe recipe)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Scatter", EditorStyles.boldLabel);

            if (GUILayout.Button("Add Scatter Layer (初期値つき)"))
            {
                AddScatterLayer(recipe);
            }

            List<string> issues = recipe.CollectScatterIssues();
            if (issues.Count > 0)
            {
                EditorGUILayout.HelpBox("・" + string.Join("\n・", issues), MessageType.Error);
            }
            else if (recipe.ScatterLayers != null && recipe.ScatterLayers.Length > 0)
            {
                EditorGUILayout.HelpBox($"散布レイヤー {recipe.ScatterLayers.Length} 件、設定に問題はありません。", MessageType.Info);
            }

            using (new EditorGUI.DisabledScope(issues.Count > 0 || recipe.ScatterLayers == null || recipe.ScatterLayers.Length == 0))
            {
                if (GUILayout.Button("Bake Stage Scatter", GUILayout.Height(30)))
                {
                    StageScatterBaker.Bake(recipe);
                }
            }
        }

        /// <summary>
        /// 初期値を入れた散布レイヤーを追加する。
        /// </summary>
        // Inspector の + で追加するとフィールド初期化子が無視されてゼロ初期化されるため、
        // 使える初期値を明示的に書き込む。
        private static void AddScatterLayer(StageRecipe recipe)
        {
            SerializedObject serializedObject = new SerializedObject(recipe);
            SerializedProperty layers = serializedObject.FindProperty("scatterLayers");

            int index = layers.arraySize;
            layers.InsertArrayElementAtIndex(index);

            SerializedProperty layer = layers.GetArrayElementAtIndex(index);
            layer.FindPropertyRelative("name").stringValue = "Scatter";
            layer.FindPropertyRelative("prototypeId").intValue = 1;
            layer.FindPropertyRelative("spacing").floatValue = 4f;
            layer.FindPropertyRelative("density").floatValue = 1f;
            layer.FindPropertyRelative("jitter").floatValue = 0.9f;
            layer.FindPropertyRelative("slopeRange").vector2Value = new Vector2(0f, 30f);
            layer.FindPropertyRelative("depthRange").vector2Value = new Vector2(0f, 300f);
            layer.FindPropertyRelative("maskChannel").enumValueIndex = (int)MaskChannel.R;
            layer.FindPropertyRelative("maskThreshold").floatValue = 0.5f;
            layer.FindPropertyRelative("maskAffectsDensity").boolValue = true;
            layer.FindPropertyRelative("scaleRange").vector2Value = new Vector2(0.8f, 1.2f);
            layer.FindPropertyRelative("alignToNormal").boolValue = false;
            layer.FindPropertyRelative("normalAlignment").floatValue = 1f;
            layer.FindPropertyRelative("randomYaw").boolValue = true;
            layer.FindPropertyRelative("surfaceOffset").floatValue = 0f;
            layer.FindPropertyRelative("instantiate").boolValue = false;

            serializedObject.ApplyModifiedProperties();
        }
    }
}
