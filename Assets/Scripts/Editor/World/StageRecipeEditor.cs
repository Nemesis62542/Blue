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
        }
    }
}
