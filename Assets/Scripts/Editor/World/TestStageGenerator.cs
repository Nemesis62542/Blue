using System.IO;
using Blue.World;
using UnityEditor;
using UnityEngine;

namespace Blue.Editor.World
{
    /// <summary>
    /// 負荷計測用の 2km テストステージ一式（ハイトマップ + StageRecipe）を生成する。
    /// </summary>
    // 外部ツール(Gaea等)でハイトマップを作る前に、タイル分割・ストリーミング・
    // メモリ量を実測できるようにするためのもの。地形の作り込みは目的ではない。
    public static class TestStageGenerator
    {
        private const string STAGE_ROOT = "Assets/Stages/TestStage";
        private const string SOURCE_FOLDER = STAGE_ROOT + "/Source";
        private const string HEIGHTMAP_PATH = SOURCE_FOLDER + "/TestStage_Height.exr";
        private const string RECIPE_PATH = STAGE_ROOT + "/TestStage_Recipe.asset";

        [MenuItem("Blue/World/Create 2km Test Stage")]
        public static void CreateTestStage()
        {
            StageTileLayout layout = StageTileLayout.Default;
            int size = layout.GlobalHeightSamples;

            if (!EditorUtility.DisplayDialog(
                    "Create 2km Test Stage",
                    $"{size}x{size} のテスト用ハイトマップと StageRecipe を生成します。\n\n" +
                    $"出力先: {STAGE_ROOT}\n\n" +
                    "既存の同名アセットは上書きされます。",
                    "生成する", "キャンセル"))
            {
                return;
            }

            EnsureFolder(SOURCE_FOLDER);

            try
            {
                EditorUtility.DisplayProgressBar("Create Test Stage", "ハイトマップを生成中...", 0.2f);
                GenerateHeightmap(size);

                EditorUtility.DisplayProgressBar("Create Test Stage", "インポート設定を適用中...", 0.7f);
                ConfigureHeightmapImporter();

                EditorUtility.DisplayProgressBar("Create Test Stage", "StageRecipe を作成中...", 0.9f);
                StageRecipe recipe = CreateRecipe(layout);

                Selection.activeObject = recipe;
                EditorGUIUtility.PingObject(recipe);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            Debug.Log(
                $"[TestStageGenerator] テストステージを生成しました: {STAGE_ROOT}\n" +
                $"  ハイトマップ: {size}x{size} (RGBAFloat/EXR)\n" +
                "  StageRecipe の Inspector から Bake Stage Terrain を実行してください。");
        }

        #region Heightmap

        /// <summary>
        /// それらしい海底地形を生成する。
        /// 岸側の浅い大陸棚 → ドロップオフ → 深い海盆、という断面に起伏を重ねる。
        /// </summary>
        private static void GenerateHeightmap(int size)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBAFloat, false, true);
            Color[] pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                float v = (float)y / (size - 1);

                for (int x = 0; x < size; x++)
                {
                    float u = (float)x / (size - 1);

                    // 岸(南西)から沖(北東)へ向かう距離
                    float offshore = Mathf.Clamp01(u * 0.35f + v * 0.65f);

                    // 海岸線を直線にしないための蛇行
                    offshore += (Fbm(u * 3f, v * 3f, 3, 2f, 0.5f, 11.3f) - 0.5f) * 0.12f;
                    offshore = Mathf.Clamp01(offshore);

                    float height = ShelfProfile(offshore);

                    // 尾根状のノイズで海山と谷を作る
                    float ridged = RidgedFbm(u * 5f, v * 5f, 4, 2f, 0.5f, 37.7f);
                    height += (ridged - 0.4f) * 0.16f * Mathf.SmoothStep(0f, 1f, offshore);

                    // 細かい起伏
                    height += (Fbm(u * 24f, v * 24f, 4, 2f, 0.5f, 91.1f) - 0.5f) * 0.035f;

                    height = Mathf.Clamp01(height);
                    pixels[y * size + x] = new Color(height, height, height, 1f);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, false);

            byte[] exr = texture.EncodeToEXR(Texture2D.EXRFlags.OutputAsFloat);
            UnityEngine.Object.DestroyImmediate(texture);

            File.WriteAllBytes(HEIGHTMAP_PATH, exr);
            AssetDatabase.ImportAsset(HEIGHTMAP_PATH, ImportAssetOptions.ForceUpdate);
        }

        /// <summary>
        /// 大陸棚 → ドロップオフ → 海盆 の高さプロファイル（正規化高さ 0-1）。
        /// レイアウト既定の -300〜20m に対して、棚が約 -20m、海盆が約 -250m になる。
        /// </summary>
        private static float ShelfProfile(float offshore)
        {
            const float shelfEnd = 0.26f;
            const float slopeEnd = 0.46f;

            const float shelfHeight = 0.93f;
            const float slopeHeight = 0.34f;
            const float basinHeight = 0.16f;

            if (offshore < shelfEnd)
            {
                // 棚の上はごく緩やかに深くなる
                float t = offshore / shelfEnd;
                return Mathf.Lerp(shelfHeight + 0.03f, shelfHeight, t);
            }

            if (offshore < slopeEnd)
            {
                // ドロップオフ。急峻にするため SmoothStep を掛ける
                float t = (offshore - shelfEnd) / (slopeEnd - shelfEnd);
                return Mathf.Lerp(shelfHeight, slopeHeight, Mathf.SmoothStep(0f, 1f, t));
            }

            float basinT = (offshore - slopeEnd) / (1f - slopeEnd);
            return Mathf.Lerp(slopeHeight, basinHeight, Mathf.SmoothStep(0f, 1f, basinT));
        }

        private static float Fbm(float x, float y, int octaves, float lacunarity, float gain, float offset)
        {
            float sum = 0f;
            float amplitude = 1f;
            float totalAmplitude = 0f;
            float frequency = 1f;

            for (int i = 0; i < octaves; i++)
            {
                sum += Mathf.PerlinNoise(x * frequency + offset, y * frequency + offset) * amplitude;
                totalAmplitude += amplitude;
                amplitude *= gain;
                frequency *= lacunarity;
            }

            return sum / totalAmplitude;
        }

        private static float RidgedFbm(float x, float y, int octaves, float lacunarity, float gain, float offset)
        {
            float sum = 0f;
            float amplitude = 1f;
            float totalAmplitude = 0f;
            float frequency = 1f;

            for (int i = 0; i < octaves; i++)
            {
                float noise = Mathf.PerlinNoise(x * frequency + offset, y * frequency + offset);
                float ridge = 1f - Mathf.Abs(noise * 2f - 1f);
                sum += ridge * ridge * amplitude;
                totalAmplitude += amplitude;
                amplitude *= gain;
                frequency *= lacunarity;
            }

            return sum / totalAmplitude;
        }

        /// <summary>
        /// ベイカーは CPU 側でピクセルを読むため、Read/Write と無圧縮・リニアが必須。
        /// </summary>
        private static void ConfigureHeightmapImporter()
        {
            TextureImporter importer = AssetImporter.GetAtPath(HEIGHTMAP_PATH) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError($"[TestStageGenerator] TextureImporter を取得できませんでした: {HEIGHTMAP_PATH}");
                return;
            }

            // SingleChannel は無圧縮指定でも R8 に落ちることがあり、高さが 256 段に量子化される。
            // EXR の float 精度を保つため Default のままにする
            importer.textureType = TextureImporterType.Default;
            importer.isReadable = true;
            importer.sRGBTexture = false;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 8192;
            importer.textureCompression = TextureImporterCompression.Uncompressed;

            importer.SaveAndReimport();
        }

        #endregion

        #region Recipe

        private static StageRecipe CreateRecipe(StageTileLayout layout)
        {
            StageRecipe recipe = AssetDatabase.LoadAssetAtPath<StageRecipe>(RECIPE_PATH);
            if (recipe == null)
            {
                recipe = ScriptableObject.CreateInstance<StageRecipe>();
                AssetDatabase.CreateAsset(recipe, RECIPE_PATH);
            }

            Texture2D heightmap = AssetDatabase.LoadAssetAtPath<Texture2D>(HEIGHTMAP_PATH);

            SerializedObject serializedObject = new SerializedObject(recipe);
            serializedObject.FindProperty("stageId").stringValue = "TestStage";
            serializedObject.FindProperty("groupingId").intValue = 0;
            serializedObject.FindProperty("heightmap").objectReferenceValue = heightmap;
            serializedObject.FindProperty("seed").intValue = 1234;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(recipe);
            AssetDatabase.SaveAssets();

            return recipe;
        }

        #endregion

        #region Utility

        private static void EnsureFolder(string folder)
        {
            if (string.IsNullOrEmpty(folder) || AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
            string leaf = Path.GetFileName(folder);

            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        #endregion
    }
}
