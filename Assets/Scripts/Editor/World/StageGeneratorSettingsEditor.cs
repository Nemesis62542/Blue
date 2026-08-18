using System.Collections.Generic;
using Blue.World;
using UnityEditor;
using UnityEngine;

namespace Blue.Editor.World
{
    /// <summary>
    /// StageGeneratorSettings のカスタムインスペクター。
    /// 断面を図で出して、生成して焼くまで結果が分からない状態を避ける。
    /// </summary>
    // 起伏と崩落は生成しないと見えないが、断面（どこが棚でどこが崖か）はパラメータだけで
    // 決まる。ここが意図通りかを先に確認できれば、生成→ベイクの往復がかなり減る。
    [CustomEditor(typeof(StageGeneratorSettings))]
    public class StageGeneratorSettingsEditor : UnityEditor.Editor
    {
        #region Constants

        private const int PREVIEW_HEIGHT = 150;
        private const int PROFILE_SAMPLES = 160;

        #endregion

        #region Fields

        private static GUIStyle overlayLabelStyle;

        #endregion

        #region Inspector

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            StageGeneratorSettings settings = (StageGeneratorSettings)target;
            List<string> errors = settings.Validate();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Profile", EditorStyles.boldLabel);

            if (settings.Recipe != null && settings.Recipe.Layout.Validate(out _))
            {
                StageTileLayout layout = settings.Recipe.Layout;
                DrawProfilePreview(settings, layout);
                DrawSummary(settings, layout);
            }

            if (errors.Count > 0)
            {
                EditorGUILayout.HelpBox("・" + string.Join("\n・", errors), MessageType.Error);
            }
            else
            {
                List<string> warnings = settings.CollectWarnings();
                if (warnings.Count > 0)
                {
                    EditorGUILayout.HelpBox("・" + string.Join("\n・", warnings), MessageType.Warning);
                }
            }

            EditorGUILayout.HelpBox(
                "生成結果は最終的な高さなので、レシピの heightCurve は Linear のままにしてください。" +
                "曲線を掛けると断面が二重に変形します。",
                MessageType.Info);

            DrawActions(settings, errors.Count > 0);
        }

        private void DrawActions(StageGeneratorSettings settings, bool hasErrors)
        {
            EditorGUILayout.Space();

            if (GUILayout.Button("Open Preview Window", GUILayout.Height(24)))
            {
                StagePreviewWindow.Open(settings);
            }

            using (new EditorGUI.DisabledScope(hasErrors || !settings.UseRegions))
            {
                if (GUILayout.Button("レシピのバイオームをリージョンから再構築", GUILayout.Height(24)))
                {
                    RebuildBiomes(settings);
                }
            }

            using (new EditorGUI.DisabledScope(hasErrors))
            {
                if (GUILayout.Button("Generate Heightmap", GUILayout.Height(30)))
                {
                    StageHeightmapGenerator.Generate(settings);
                }

                if (GUILayout.Button("Generate & Bake Terrain", GUILayout.Height(24)))
                {
                    if (StageHeightmapGenerator.Generate(settings))
                    {
                        StageTerrainBaker.Bake(settings.Recipe);
                    }
                }
            }
        }

        #endregion

        #region Biomes

        /// <summary>
        /// リージョン構成から、レシピのスプラット設定を組み立てる。
        /// </summary>
        // 手で4つ作って channel を R/G/B/A に振り分けるのは間違えやすいうえ、
        // リージョンを増減するたびにやり直しになる。マスク画像の作り手である
        // ジェネレータ側が対応表も作る。
        //
        // TerrainLayer の割り当ては名前で引き継ぐ。ここが消えると、
        // リージョンを1つ触るたびに素材を割り当て直すことになる。
        private static void RebuildBiomes(StageGeneratorSettings settings)
        {
            StageRecipe recipe = settings.Recipe;
            string maskPath = StageHeightmapGenerator.RegionMaskPath(recipe);
            Texture2D mask = AssetDatabase.LoadAssetAtPath<Texture2D>(maskPath);

            if (mask == null)
            {
                EditorUtility.DisplayDialog(
                    "Stage Generator",
                    $"リージョンマスクが見つかりません:\n{maskPath}\n\n" +
                    "先に Generate Heightmap を実行してください。",
                    "OK");
                return;
            }

            Dictionary<string, TerrainLayer> assigned = CollectAssignedLayers(recipe);

            StageRegionProfile[] profiles = settings.RegionProfiles;
            int count = Mathf.Min(StageHeightmapGenerator.MASK_CHANNELS, profiles.Length);

            SerializedObject serializedObject = new SerializedObject(recipe);
            SerializedProperty biomes = serializedObject.FindProperty("biomes");
            biomes.arraySize = count;

            for (int i = 0; i < count; i++)
            {
                StageRegionProfile profile = profiles[i];
                string name = profile != null ? profile.name : $"Region {i}";

                SerializedProperty element = biomes.GetArrayElementAtIndex(i);

                // 配列を伸ばすと直前の要素の値が複製されるので、全ての項目を明示的に入れる
                element.FindPropertyRelative("name").stringValue = name;
                element.FindPropertyRelative("mask").objectReferenceValue = mask;
                element.FindPropertyRelative("channel").enumValueIndex = i;
                element.FindPropertyRelative("weight").floatValue = 1f;

                // リージョンの重みはそのまま使うので変換しない。
                // 明示しないと、直前の要素に入っていた傾斜の閾値を引き継いで重みが反転する
                element.FindPropertyRelative("maskRange").vector2Value = Vector2.zero;
                element.FindPropertyRelative("terrainLayer").objectReferenceValue =
                    assigned.TryGetValue(name, out TerrainLayer layer) ? layer : null;
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(recipe);
            AssetDatabase.SaveAssets();

            Selection.activeObject = recipe;
            EditorGUIUtility.PingObject(recipe);

            string overflow = profiles.Length > StageHeightmapGenerator.MASK_CHANNELS
                ? $"\n  リージョンが {profiles.Length} 種ありますが、1枚のマスクに入るのは " +
                  $"{StageHeightmapGenerator.MASK_CHANNELS} 種までです。残りは塗られません。"
                : string.Empty;

            Debug.Log(
                $"[StageGeneratorSettings] '{recipe.StageId}' のバイオームを {count} 件構築しました。\n" +
                $"  マスク: {maskPath}\n" +
                "  各バイオームに TerrainLayer を割り当ててからベイクしてください。" + overflow,
                recipe);
        }

        private static Dictionary<string, TerrainLayer> CollectAssignedLayers(StageRecipe recipe)
        {
            Dictionary<string, TerrainLayer> assigned = new Dictionary<string, TerrainLayer>();

            if (recipe.Biomes == null)
            {
                return assigned;
            }

            foreach (BiomeLayerBinding biome in recipe.Biomes)
            {
                if (biome != null && biome.terrainLayer != null && !string.IsNullOrEmpty(biome.name))
                {
                    assigned[biome.name] = biome.terrainLayer;
                }
            }

            return assigned;
        }

        #endregion

        #region Preview

        /// <summary>
        /// 岸から沖への断面を描く。起伏と崩落を含まない素のプロファイル。
        /// </summary>
        // プレビューウィンドウからも同じ図を出すため公開している。
        public static void DrawProfilePreview(StageGeneratorSettings settings, StageTileLayout layout)
        {
            Rect rect = GUILayoutUtility.GetRect(0f, PREVIEW_HEIGHT, GUILayout.ExpandWidth(true));
            if (Event.current.type != EventType.Repaint && rect.width <= 1f)
            {
                return;
            }

            EditorGUI.DrawRect(rect, new Color(0.10f, 0.13f, 0.17f));

            float top = layout.MaxHeight;
            float range = layout.HeightRange;

            float ToPixelY(float worldY) => rect.yMin + (top - worldY) / range * rect.height;

            DrawWater(rect, layout, ToPixelY);
            DrawSectionGuide(rect, settings.ShelfExtent);
            DrawSectionGuide(rect, settings.SlopeEnd);
            DrawSeafloor(rect, settings, ToPixelY);
            DrawLabels(rect, layout);
        }

        private static void DrawWater(Rect rect, StageTileLayout layout, System.Func<float, float> toPixelY)
        {
            if (layout.MaxHeight < 0f || layout.MinHeight > 0f)
            {
                return;
            }

            float surface = toPixelY(0f);
            EditorGUI.DrawRect(
                new Rect(rect.xMin, surface, rect.width, rect.yMax - surface),
                new Color(0.16f, 0.34f, 0.48f, 0.35f));
            EditorGUI.DrawRect(
                new Rect(rect.xMin, surface, rect.width, 1f),
                new Color(0.45f, 0.85f, 1f, 0.9f));
        }

        private static void DrawSectionGuide(Rect rect, float offshore)
        {
            float x = rect.xMin + Mathf.Clamp01(offshore) * rect.width;
            EditorGUI.DrawRect(new Rect(x, rect.yMin, 1f, rect.height), new Color(1f, 1f, 1f, 0.12f));
        }

        private static void DrawSeafloor(Rect rect, StageGeneratorSettings settings, System.Func<float, float> toPixelY)
        {
            Vector3[] points = new Vector3[PROFILE_SAMPLES];

            for (int i = 0; i < PROFILE_SAMPLES; i++)
            {
                float offshore = (float)i / (PROFILE_SAMPLES - 1);
                float pixelY = Mathf.Clamp(toPixelY(-settings.DepthAt(offshore)), rect.yMin, rect.yMax);
                points[i] = new Vector3(rect.xMin + offshore * rect.width, pixelY, 0f);
            }

            Handles.BeginGUI();
            Handles.color = new Color(0.87f, 0.76f, 0.55f);
            Handles.DrawAAPolyLine(2.5f, points);
            Handles.EndGUI();
        }

        private static void DrawLabels(Rect rect, StageTileLayout layout)
        {
            overlayLabelStyle ??= new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = new Color(0.75f, 0.8f, 0.85f) },
            };

            GUI.Label(new Rect(rect.xMin + 4f, rect.yMin + 1f, 120f, 14f),
                $"Y {layout.MaxHeight:F0}m", overlayLabelStyle);
            GUI.Label(new Rect(rect.xMin + 4f, rect.yMax - 15f, 120f, 14f),
                $"Y {layout.MinHeight:F0}m", overlayLabelStyle);
            GUI.Label(new Rect(rect.xMin + 4f, rect.yMax - 29f, 120f, 14f),
                "岸", overlayLabelStyle);

            Rect offshoreLabel = new Rect(rect.xMax - 44f, rect.yMax - 29f, 40f, 14f);
            GUI.Label(offshoreLabel, "沖", overlayLabelStyle);
        }

        #endregion

        #region Summary

        private static void DrawSummary(StageGeneratorSettings settings, StageTileLayout layout)
        {
            int size = layout.GlobalHeightSamples;
            float shelfMeters = settings.ShelfExtent * layout.WorldSize;
            float slopeMeters = (settings.SlopeEnd - settings.ShelfExtent) * layout.WorldSize;
            float basinMeters = (1f - settings.SlopeEnd) * layout.WorldSize;

            // ドロップオフの平均傾斜。ここが緩すぎると「崖」に見えない
            float dropoffRise = settings.SlopeDepth - settings.ShelfDepth;
            float dropoffAngle = slopeMeters > 0f
                ? Mathf.Atan2(dropoffRise, slopeMeters) * Mathf.Rad2Deg
                : 90f;

            EditorGUILayout.HelpBox(
                $"棚       : {shelfMeters:F0}m 幅 / 水深 {settings.ShoreDepth:F0} 〜 {settings.ShelfDepth:F0}m\n" +
                $"ドロップオフ: {slopeMeters:F0}m 幅 / 水深 {settings.ShelfDepth:F0} 〜 {settings.SlopeDepth:F0}m " +
                $"(平均 {dropoffAngle:F0}度)\n" +
                $"海盆     : {basinMeters:F0}m 幅 / 水深 {settings.SlopeDepth:F0} 〜 {settings.BasinDepth:F0}m\n" +
                $"生成解像度: {size}x{size} ({layout.WorldSize / (size - 1):F2}m/texel)",
                MessageType.None);
        }

        #endregion
    }
}
