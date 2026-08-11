using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Blue.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

namespace Blue.Editor.World
{
    /// <summary>
    /// StageRecipe から Terrain タイル群とタイルシーンを生成するベイカー。
    ///
    /// 【なぜ Terrain Tools パッケージを使わないか】
    /// Terrain Toolbox が提供するのは「ハイトマップ→タイル分割 Terrain 生成」までで、
    /// しかもカレントシーンに生成するため、タイルごとにシーンを分ける構成では結局後処理が必要になる。
    /// 加えて 5.x は Unity 2022.2 想定で Unity 6 対応が明示されていない。
    /// 自作範囲（マスク→アルファマップ、散布、スポーンフィールド、タイルシーン）が支配的なので、
    /// 全工程をスクリプトから再実行できる形にしている。
    ///
    /// 【再ベイクの安全性】
    /// TerrainData は作り直さず、既存アセットを上書き更新して GUID を維持する。
    /// Digger は編集データを TerrainData の GUID でフォルダ分けしているため、
    /// GUID が変わると掘削済みの洞窟が全て参照を失う。
    /// </summary>
    public static class StageTerrainBaker
    {
        private const string GENERATED_FOLDER = "Generated";
        private const string TERRAIN_DATA_FOLDER = "TerrainData";
        private const string TILES_FOLDER = "Tiles";

        #region Menu

        [MenuItem("Blue/World/Bake Selected Stage Recipe")]
        public static void BakeSelected()
        {
            StageRecipe recipe = Selection.activeObject as StageRecipe;
            if (recipe == null)
            {
                EditorUtility.DisplayDialog("Stage Terrain Baker",
                    "StageRecipe アセットを選択してから実行してください。", "OK");
                return;
            }

            Bake(recipe);
        }

        [MenuItem("Blue/World/Bake Selected Stage Recipe", true)]
        private static bool BakeSelectedValidate() => Selection.activeObject is StageRecipe;

        #endregion

        #region Bake

        /// <summary>
        /// レシピをベイクする。既存の生成物は上書き更新される。
        /// </summary>
        public static bool Bake(StageRecipe recipe)
        {
            if (recipe == null)
            {
                return false;
            }

            List<string> errors = recipe.Validate();
            if (errors.Count > 0)
            {
                Debug.LogError($"[StageTerrainBaker] '{recipe.StageId}' のベイクを中止しました:\n - {string.Join("\n - ", errors)}", recipe);
                EditorUtility.DisplayDialog("Stage Terrain Baker",
                    $"設定に問題があります:\n\n・{string.Join("\n・", errors)}", "OK");
                return false;
            }

            foreach (string warning in recipe.CollectWarnings())
            {
                Debug.LogWarning($"[StageTerrainBaker] {warning}", recipe);
            }

            // タイルシーンを開閉するため、未保存の変更を先に処理させる
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.Log("[StageTerrainBaker] ユーザーがキャンセルしました。");
                return false;
            }

            string recipePath = AssetDatabase.GetAssetPath(recipe);
            if (string.IsNullOrEmpty(recipePath))
            {
                Debug.LogError("[StageTerrainBaker] StageRecipe がアセットとして保存されていません。", recipe);
                return false;
            }

            string recipeDir = Path.GetDirectoryName(recipePath).Replace('\\', '/');
            string generatedDir = $"{recipeDir}/{GENERATED_FOLDER}";
            string terrainDataDir = $"{generatedDir}/{TERRAIN_DATA_FOLDER}";
            string tilesDir = $"{generatedDir}/{TILES_FOLDER}";

            EnsureFolder(terrainDataDir);
            EnsureFolder(tilesDir);

            StageTileLayout layout = recipe.Layout;
            TextureSampler heightSampler = new TextureSampler(recipe.Heightmap, MaskChannel.R);
            BiomeSamplers biomeSamplers = BiomeSamplers.Create(recipe);

            List<StageTileEntry> entries = new List<StageTileEntry>(layout.TileCount);
            Stopwatch stopwatch = Stopwatch.StartNew();
            bool cancelled = false;

            try
            {
                for (int tileZ = 0; tileZ < layout.TilesPerAxis; tileZ++)
                {
                    for (int tileX = 0; tileX < layout.TilesPerAxis; tileX++)
                    {
                        int index = layout.TileIndex(tileX, tileZ);
                        float progress = (float)index / layout.TileCount;

                        if (EditorUtility.DisplayCancelableProgressBar(
                                "Stage Terrain Baker",
                                $"タイル {index + 1}/{layout.TileCount} (x={tileX}, z={tileZ}) をベイク中...",
                                progress))
                        {
                            cancelled = true;
                            break;
                        }

                        string terrainDataPath = $"{terrainDataDir}/TD_r{tileZ}_c{tileX}.asset";
                        TerrainData terrainData = BakeTerrainData(
                            recipe, heightSampler, biomeSamplers, tileX, tileZ, terrainDataPath,
                            out float minBaked, out float maxBaked);

                        string scenePath = $"{tilesDir}/Tile_r{tileZ}_c{tileX}.unity";
                        BakeTileScene(recipe, terrainData, tileX, tileZ, scenePath);

                        entries.Add(new StageTileEntry
                        {
                            tileIndex = index,
                            tileX = tileX,
                            tileZ = tileZ,
                            scenePath = scenePath,
                            bounds = layout.TileBounds(tileX, tileZ),
                            minBakedHeight = minBaked,
                            maxBakedHeight = maxBaked,
                        });
                    }

                    if (cancelled)
                    {
                        break;
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            if (cancelled)
            {
                Debug.LogWarning($"[StageTerrainBaker] '{recipe.StageId}' のベイクを中断しました（{entries.Count}/{layout.TileCount} タイル完了）。");
                AssetDatabase.SaveAssets();
                return false;
            }

            string manifestPath = $"{generatedDir}/{recipe.StageId}_TileManifest.asset";
            WriteManifest(recipe, entries.ToArray(), manifestPath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            stopwatch.Stop();
            Debug.Log(
                $"[StageTerrainBaker] '{recipe.StageId}' をベイクしました。\n" +
                $"  タイル: {entries.Count} 枚 ({layout.TilesPerAxis}x{layout.TilesPerAxis}, 1枚 {layout.TileSize:F0}m)\n" +
                $"  ワールド: {layout.WorldSize:F0}m 四方 / 高さ {layout.MinHeight:F0}〜{layout.MaxHeight:F0}m\n" +
                $"  出力: {generatedDir}\n" +
                $"  所要時間: {stopwatch.ElapsedMilliseconds} ms",
                AssetDatabase.LoadAssetAtPath<StageTileManifest>(manifestPath));

            return true;
        }

        #endregion

        #region TerrainData

        private static TerrainData BakeTerrainData(
            StageRecipe recipe,
            TextureSampler heightSampler,
            BiomeSamplers biomeSamplers,
            int tileX,
            int tileZ,
            string assetPath,
            out float minBakedHeight,
            out float maxBakedHeight)
        {
            StageTileLayout layout = recipe.Layout;

            // 既存アセットがあれば必ず再利用する。作り直すと GUID が変わり Digger のデータが迷子になる
            TerrainData terrainData = AssetDatabase.LoadAssetAtPath<TerrainData>(assetPath);
            if (terrainData == null)
            {
                terrainData = new TerrainData { name = Path.GetFileNameWithoutExtension(assetPath) };
                AssetDatabase.CreateAsset(terrainData, assetPath);
            }

            int resolution = layout.HeightmapResolution;

            // 解像度の変更はハイトマップを破棄するため、必要なときだけ触る
            if (terrainData.heightmapResolution != resolution)
            {
                terrainData.heightmapResolution = resolution;
            }

            terrainData.size = layout.TerrainSize;

            float[,] heights = BuildHeights(recipe, heightSampler, tileX, tileZ, out minBakedHeight, out maxBakedHeight);
            terrainData.SetHeights(0, 0, heights);

            if (biomeSamplers.HasLayers)
            {
                ApplyAlphamaps(recipe, biomeSamplers, terrainData, tileX, tileZ);
            }

            EditorUtility.SetDirty(terrainData);
            return terrainData;
        }

        /// <summary>
        /// タイルのハイトマップを構築する。
        ///
        /// 隣接タイルは境界の1列を共有するので、グローバルなサンプル添字から UV を求める。
        /// こうすると共有列は必ず同じ UV → 同じ高さになり、継ぎ目が原理的に発生しない。
        /// </summary>
        private static float[,] BuildHeights(
            StageRecipe recipe,
            TextureSampler sampler,
            int tileX,
            int tileZ,
            out float minBakedHeight,
            out float maxBakedHeight)
        {
            StageTileLayout layout = recipe.Layout;
            int resolution = layout.HeightmapResolution;
            int globalMax = layout.GlobalHeightSamples - 1;
            AnimationCurve curve = recipe.HeightCurve;

            // TerrainData.SetHeights は [z, x] の順で受け取る
            float[,] heights = new float[resolution, resolution];
            float minNormalized = float.MaxValue;
            float maxNormalized = float.MinValue;

            for (int z = 0; z < resolution; z++)
            {
                float v = (float)(tileZ * (resolution - 1) + z) / globalMax;

                for (int x = 0; x < resolution; x++)
                {
                    float u = (float)(tileX * (resolution - 1) + x) / globalMax;
                    float raw = sampler.SampleBilinear(u, v);
                    float shaped = Mathf.Clamp01(curve.Evaluate(raw));

                    heights[z, x] = shaped;

                    if (shaped < minNormalized)
                    {
                        minNormalized = shaped;
                    }

                    if (shaped > maxNormalized)
                    {
                        maxNormalized = shaped;
                    }
                }
            }

            minBakedHeight = layout.MinHeight + minNormalized * layout.HeightRange;
            maxBakedHeight = layout.MinHeight + maxNormalized * layout.HeightRange;
            return heights;
        }

        /// <summary>
        /// バイオームマスクから Terrain のスプラットウェイトを構築する。
        ///
        /// ハイトマップと違いアルファマップは頂点ではなくテクセル（面積サンプル）なので、
        /// 境界を共有せずテクセル中心で UV を取る。
        /// </summary>
        private static void ApplyAlphamaps(
            StageRecipe recipe,
            BiomeSamplers biomeSamplers,
            TerrainData terrainData,
            int tileX,
            int tileZ)
        {
            StageTileLayout layout = recipe.Layout;
            int resolution = layout.AlphamapResolution;
            int layerCount = biomeSamplers.LayerCount;

            terrainData.terrainLayers = biomeSamplers.Layers;

            if (terrainData.alphamapResolution != resolution)
            {
                terrainData.alphamapResolution = resolution;
            }

            terrainData.baseMapResolution = resolution;

            int globalResolution = layout.TilesPerAxis * resolution;
            float[,,] maps = new float[resolution, resolution, layerCount];
            float[] weights = new float[layerCount];

            for (int z = 0; z < resolution; z++)
            {
                float v = (tileZ * resolution + z + 0.5f) / globalResolution;

                for (int x = 0; x < resolution; x++)
                {
                    float u = (tileX * resolution + x + 0.5f) / globalResolution;

                    float total = 0f;
                    for (int layer = 0; layer < layerCount; layer++)
                    {
                        float weight = biomeSamplers.SampleWeight(layer, u, v);
                        weights[layer] = weight;
                        total += weight;
                    }

                    if (total <= Mathf.Epsilon)
                    {
                        // どのマスクにも属さない領域は先頭レイヤーで埋める
                        maps[z, x, 0] = 1f;
                        continue;
                    }

                    float inverseTotal = 1f / total;
                    for (int layer = 0; layer < layerCount; layer++)
                    {
                        maps[z, x, layer] = weights[layer] * inverseTotal;
                    }
                }
            }

            terrainData.SetAlphamaps(0, 0, maps);
        }

        #endregion

        #region Tile Scene

        /// <summary>
        /// タイルシーンを生成または更新する。
        /// 既存シーンは開き直して中身を更新するため、手で足したオブジェクトは失われない。
        /// </summary>
        private static void BakeTileScene(
            StageRecipe recipe,
            TerrainData terrainData,
            int tileX,
            int tileZ,
            string scenePath)
        {
            StageTileLayout layout = recipe.Layout;
            int tileIndex = layout.TileIndex(tileX, tileZ);

            Scene activeScene = SceneManager.GetActiveScene();
            bool sceneExists = File.Exists(scenePath);
            bool wasAlreadyOpen = sceneExists && SceneManager.GetSceneByPath(scenePath).isLoaded;

            Scene tileScene = sceneExists
                ? EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive)
                : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);

            StageTile stageTile = FindStageTile(tileScene);
            if (stageTile == null)
            {
                // CreateTerrainGameObject はアクティブシーンに生成するので、一時的に切り替える
                SceneManager.SetActiveScene(tileScene);
                GameObject terrainObject = Terrain.CreateTerrainGameObject(terrainData);
                SceneManager.SetActiveScene(activeScene);

                stageTile = terrainObject.AddComponent<StageTile>();
            }

            GameObject tileObject = stageTile.gameObject;
            tileObject.name = $"Terrain_r{tileZ}_c{tileX}";
            tileObject.transform.position = layout.TileOrigin(tileX, tileZ);
            tileObject.transform.rotation = Quaternion.identity;
            tileObject.transform.localScale = Vector3.one;

            Terrain terrain = stageTile.Terrain;
            terrain.terrainData = terrainData;

            // 同じ groupingID を持つ Terrain 同士は、ロードされた時点で Unity が自動的に接続する。
            // ストリーミングでは隣タイルが未ロードのことがあるため、手動 SetNeighbors ではなくこれを使う
            terrain.groupingID = recipe.GroupingId;
            terrain.allowAutoConnect = true;

            TerrainCollider collider = tileObject.GetComponent<TerrainCollider>();
            if (collider == null)
            {
                collider = tileObject.AddComponent<TerrainCollider>();
            }

            collider.terrainData = terrainData;

            stageTile.Setup(recipe.StageId, tileX, tileZ, tileIndex);

            EditorSceneManager.MarkSceneDirty(tileScene);
            EditorSceneManager.SaveScene(tileScene, scenePath);

            // ベイク前から開いていたシーンはユーザーの作業中なので閉じない
            if (!wasAlreadyOpen)
            {
                EditorSceneManager.CloseScene(tileScene, true);
            }
        }

        private static StageTile FindStageTile(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                StageTile tile = root.GetComponentInChildren<StageTile>(true);
                if (tile != null)
                {
                    return tile;
                }
            }

            return null;
        }

        #endregion

        #region Manifest

        private static void WriteManifest(StageRecipe recipe, StageTileEntry[] entries, string manifestPath)
        {
            StageTileManifest manifest = AssetDatabase.LoadAssetAtPath<StageTileManifest>(manifestPath);
            if (manifest == null)
            {
                manifest = ScriptableObject.CreateInstance<StageTileManifest>();
                AssetDatabase.CreateAsset(manifest, manifestPath);
            }

            manifest.SetContents(recipe.StageId, recipe.Layout, entries);
            EditorUtility.SetDirty(manifest);
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

        #region Sampling

        /// <summary>
        /// テクスチャを1度だけ CPU 側に読み出して、クランプ付きバイリニアでサンプルする。
        ///
        /// Texture2D.GetPixelBilinear を直接使わない理由:
        ///   - テクスチャの wrapMode が Repeat だと u=1 が反対側に回り込み、タイル境界が壊れる
        ///   - 2km を 8x8 タイルでベイクすると数百万回の呼び出しになる
        /// </summary>
        private sealed class TextureSampler
        {
            private readonly float[] values;
            private readonly int width;
            private readonly int height;

            public TextureSampler(Texture2D texture, MaskChannel channel)
            {
                width = texture.width;
                height = texture.height;
                values = new float[width * height];

                Color[] pixels = texture.GetPixels();
                for (int i = 0; i < values.Length; i++)
                {
                    Color pixel = pixels[i];
                    values[i] = channel switch
                    {
                        MaskChannel.R => pixel.r,
                        MaskChannel.G => pixel.g,
                        MaskChannel.B => pixel.b,
                        MaskChannel.A => pixel.a,
                        _ => pixel.r,
                    };
                }
            }

            /// <summary>UV(0-1)を画像の端から端に写してサンプルする</summary>
            public float SampleBilinear(float u, float v)
            {
                float px = Mathf.Clamp01(u) * (width - 1);
                float py = Mathf.Clamp01(v) * (height - 1);

                int x0 = Mathf.FloorToInt(px);
                int y0 = Mathf.FloorToInt(py);
                int x1 = Mathf.Min(x0 + 1, width - 1);
                int y1 = Mathf.Min(y0 + 1, height - 1);

                float tx = px - x0;
                float ty = py - y0;

                float v00 = values[y0 * width + x0];
                float v10 = values[y0 * width + x1];
                float v01 = values[y1 * width + x0];
                float v11 = values[y1 * width + x1];

                return Mathf.Lerp(Mathf.Lerp(v00, v10, tx), Mathf.Lerp(v01, v11, tx), ty);
            }
        }

        /// <summary>
        /// バイオームごとのマスクサンプラーと TerrainLayer の対応をまとめたもの。
        /// </summary>
        private sealed class BiomeSamplers
        {
            private TextureSampler[] samplers;
            private float[] weights;

            public TerrainLayer[] Layers { get; private set; }

            public int LayerCount => Layers?.Length ?? 0;

            public bool HasLayers => LayerCount > 0;

            public static BiomeSamplers Create(StageRecipe recipe)
            {
                BiomeSamplers result = new BiomeSamplers();

                if (!recipe.HasBiomes)
                {
                    result.Layers = System.Array.Empty<TerrainLayer>();
                    return result;
                }

                List<TerrainLayer> layers = new List<TerrainLayer>();
                List<TextureSampler> samplers = new List<TextureSampler>();
                List<float> weights = new List<float>();

                foreach (BiomeLayerBinding biome in recipe.Biomes)
                {
                    if (biome == null || biome.terrainLayer == null)
                    {
                        continue;
                    }

                    layers.Add(biome.terrainLayer);
                    // マスクが無いものは常時ウェイト1のベースレイヤーとして扱う
                    samplers.Add(biome.mask != null ? new TextureSampler(biome.mask, biome.channel) : null);
                    weights.Add(biome.weight);
                }

                result.Layers = layers.ToArray();
                result.samplers = samplers.ToArray();
                result.weights = weights.ToArray();
                return result;
            }

            public float SampleWeight(int layer, float u, float v)
            {
                TextureSampler sampler = samplers[layer];
                float value = sampler == null ? 1f : sampler.SampleBilinear(u, v);
                return Mathf.Max(0f, value * weights[layer]);
            }
        }

        #endregion
    }
}
