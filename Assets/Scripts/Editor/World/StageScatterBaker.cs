using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Blue.World;
using Blue.World.Scatter;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

namespace Blue.Editor.World
{
    /// <summary>
    /// StageRecipe の散布ルールから、タイルごとの ScatterChunk を生成する。
    /// </summary>
    // 配置は物理レイキャストで決める。TerrainData.GetInterpolatedHeight の方が速いが、
    // それだと Digger で掘った洞窟の中に散布物を置けない。物理なら Terrain のコライダーにも
    // 洞窟メッシュのコライダーにも等しく当たるため、洞窟の内外を区別せず同じコードで扱える。
    // そのためベイク中はタイルシーンを開いておく必要がある。
    //
    // 候補点は層化ジッタグリッドで生成する。真の Poisson-disk より単純で決定的、
    // かつ十分自然に見える。品質が問題になったら差し替える。
    public static class StageScatterBaker
    {
        private const string GENERATED_FOLDER = "Generated";
        private const string SCATTER_FOLDER = "Scatter";

        // 地形の上端より十分高い位置から真下に撃つ
        private const float RAY_START_MARGIN = 50f;

        #region Menu

        [MenuItem("Blue/World/Bake Selected Stage Scatter")]
        public static void BakeSelected()
        {
            StageRecipe recipe = Selection.activeObject as StageRecipe;
            if (recipe == null)
            {
                EditorUtility.DisplayDialog("Stage Scatter Baker",
                    "StageRecipe アセットを選択してから実行してください。", "OK");
                return;
            }

            Bake(recipe);
        }

        [MenuItem("Blue/World/Bake Selected Stage Scatter", true)]
        private static bool BakeSelectedValidate() => Selection.activeObject is StageRecipe;

        #endregion

        #region Bake

        /// <summary>
        /// 散布をベイクする。既存の ScatterChunk は上書き更新される。
        /// </summary>
        public static bool Bake(StageRecipe recipe)
        {
            if (!Validate(recipe, out StageTileManifest manifest, out string error))
            {
                Debug.LogError($"[StageScatterBaker] ベイクを中止しました: {error}", recipe);
                EditorUtility.DisplayDialog("Stage Scatter Baker", error, "OK");
                return false;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return false;
            }

            string generatedDir = Path.GetDirectoryName(AssetDatabase.GetAssetPath(manifest)).Replace('\\', '/');
            string scatterDir = $"{generatedDir}/{SCATTER_FOLDER}";
            EnsureFolder(scatterDir);

            LayerSampler[] samplers = LayerSampler.Build(recipe);
            Stopwatch stopwatch = Stopwatch.StartNew();
            List<Scene> openedScenes = new List<Scene>();
            int totalInstances = 0;
            bool cancelled = false;

            try
            {
                // レイキャストのためにコライダーが要る。ベイク中だけ全タイルを開く
                openedScenes = OpenTileScenes(manifest);

                // 開いた直後はコライダーの位置が物理側に反映されていないことがある
                Physics.SyncTransforms();

                for (int i = 0; i < manifest.Tiles.Length; i++)
                {
                    StageTileEntry tile = manifest.Tiles[i];

                    if (EditorUtility.DisplayCancelableProgressBar(
                            "Stage Scatter Baker",
                            $"タイル {i + 1}/{manifest.Tiles.Length} を散布中... (累計 {totalInstances} 個体)",
                            (float)i / manifest.Tiles.Length))
                    {
                        cancelled = true;
                        break;
                    }

                    totalInstances += BakeTile(recipe, manifest, tile, samplers, scatterDir);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                CloseTileScenes(openedScenes);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            stopwatch.Stop();

            if (cancelled)
            {
                Debug.LogWarning($"[StageScatterBaker] '{recipe.StageId}' の散布ベイクを中断しました。");
                return false;
            }

            // 「走ったが1個も置けていない」ベイクが黙って成立しないようにする
            if (totalInstances == 0)
            {
                Debug.LogError(
                    $"[StageScatterBaker] '{recipe.StageId}' で1個体も配置されませんでした。以下を確認してください。\n" +
                    "  ・depthRange: 水面(waterLevel)からの深さで判定します。地形が範囲外にあると全て弾かれます\n" +
                    "  ・slopeRange: 地形の傾斜(度)。既定の 0-30 は平坦地のみを対象にします\n" +
                    "  ・maskThreshold: マスクを設定している場合、閾値が高すぎると全て弾かれます\n" +
                    "  ・タイルシーンに TerrainCollider があるか（地形ベイクが済んでいるか）",
                    recipe);
                return false;
            }

            Debug.Log(
                $"[StageScatterBaker] '{recipe.StageId}' の散布をベイクしました。\n" +
                $"  レイヤー: {samplers.Length} 種 / タイル: {manifest.Tiles.Length} 枚\n" +
                $"  配置数: {totalInstances} 個体 (平均 {totalInstances / Mathf.Max(1, manifest.Tiles.Length)} /タイル)\n" +
                $"  出力: {scatterDir}\n" +
                $"  所要時間: {stopwatch.ElapsedMilliseconds} ms",
                manifest);

            return true;
        }

        private static bool Validate(StageRecipe recipe, out StageTileManifest manifest, out string error)
        {
            manifest = null;

            if (recipe == null)
            {
                error = "StageRecipe が null です。";
                return false;
            }

            if (recipe.ScatterRegistry == null)
            {
                error = "StageRecipe の scatterRegistry が未設定です。";
                return false;
            }

            if (!recipe.ScatterRegistry.ValidateIds(out string registryError))
            {
                error = registryError;
                return false;
            }

            if (recipe.ScatterLayers == null || recipe.ScatterLayers.Length == 0)
            {
                error = "StageRecipe に散布レイヤーが1つも設定されていません。";
                return false;
            }

            string recipePath = AssetDatabase.GetAssetPath(recipe);
            string manifestPath = $"{Path.GetDirectoryName(recipePath).Replace('\\', '/')}/" +
                                  $"{GENERATED_FOLDER}/{recipe.StageId}_TileManifest.asset";

            manifest = AssetDatabase.LoadAssetAtPath<StageTileManifest>(manifestPath);
            if (manifest == null)
            {
                error = $"タイルマニフェストが見つかりません: {manifestPath}\n先に地形をベイクしてください。";
                return false;
            }

            error = null;
            return true;
        }

        #endregion

        #region Tile

        private static int BakeTile(StageRecipe recipe, StageTileManifest manifest, StageTileEntry tile,
                                    LayerSampler[] samplers, string scatterDir)
        {
            StageTileLayout layout = manifest.Layout;
            Bounds bounds = tile.bounds;
            float rayStartY = layout.MaxHeight + RAY_START_MARGIN;
            float rayDistance = layout.HeightRange + RAY_START_MARGIN * 2f;

            List<ScatterGroup> groups = new List<ScatterGroup>();
            int total = 0;

            foreach (LayerSampler sampler in samplers)
            {
                List<ScatterInstance> instances = new List<ScatterInstance>();

                // シードにタイルとレイヤーを混ぜる。タイル単位で決定的にするため、
                // 隣タイルを再ベイクしてもこのタイルの結果は変わらない
                System.Random random = new System.Random(
                    recipe.Seed ^ (tile.tileIndex * 73856093) ^ (sampler.Layer.prototypeId * 19349663));

                ScatterLayer layer = sampler.Layer;
                int cellsX = Mathf.Max(1, Mathf.CeilToInt(bounds.size.x / layer.spacing));
                int cellsZ = Mathf.Max(1, Mathf.CeilToInt(bounds.size.z / layer.spacing));

                for (int cz = 0; cz < cellsZ; cz++)
                {
                    for (int cx = 0; cx < cellsX; cx++)
                    {
                        if (NextFloat(random) > layer.density)
                        {
                            continue;
                        }

                        float jitterX = (NextFloat(random) - 0.5f) * layer.jitter;
                        float jitterZ = (NextFloat(random) - 0.5f) * layer.jitter;

                        float x = bounds.min.x + (cx + 0.5f + jitterX) * layer.spacing;
                        float z = bounds.min.z + (cz + 0.5f + jitterZ) * layer.spacing;

                        if (x < bounds.min.x || x > bounds.max.x || z < bounds.min.z || z > bounds.max.z)
                        {
                            continue;
                        }

                        if (TryPlace(recipe, layout, sampler, random, new Vector3(x, rayStartY, z),
                                     rayDistance, out ScatterInstance instance))
                        {
                            instances.Add(instance);
                        }
                    }
                }

                if (instances.Count == 0)
                {
                    continue;
                }

                groups.Add(new ScatterGroup
                {
                    prototypeId = layer.prototypeId,
                    instantiate = layer.instantiate,
                    instances = instances.ToArray(),
                });

                total += instances.Count;
            }

            WriteChunk(tile, groups, bounds, scatterDir);
            return total;
        }

        private static bool TryPlace(StageRecipe recipe, StageTileLayout layout, LayerSampler sampler,
                                     System.Random random, Vector3 origin, float rayDistance,
                                     out ScatterInstance instance)
        {
            instance = default;
            ScatterLayer layer = sampler.Layer;

            if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, rayDistance))
            {
                return false;
            }

            // 水深フィルタ。水面より上や、深すぎる場所を弾く
            float depth = recipe.WaterLevel - hit.point.y;
            if (depth < layer.depthRange.x || depth > layer.depthRange.y)
            {
                return false;
            }

            float slope = Vector3.Angle(hit.normal, Vector3.up);
            if (slope < layer.slopeRange.x || slope > layer.slopeRange.y)
            {
                return false;
            }

            if (sampler.HasMask)
            {
                Vector2 uv = layout.WorldToStageUv(hit.point);
                float maskValue = sampler.SampleMask(uv.x, uv.y);

                if (maskValue < layer.maskThreshold)
                {
                    return false;
                }

                if (layer.maskAffectsDensity && NextFloat(random) > maskValue)
                {
                    return false;
                }
            }

            Quaternion rotation = Quaternion.identity;

            if (layer.alignToNormal)
            {
                Quaternion aligned = Quaternion.FromToRotation(Vector3.up, hit.normal);
                rotation = Quaternion.Slerp(Quaternion.identity, aligned, layer.normalAlignment);
            }

            if (layer.randomYaw)
            {
                rotation *= Quaternion.Euler(0f, NextFloat(random) * 360f, 0f);
            }

            instance = new ScatterInstance
            {
                position = hit.point + hit.normal * layer.surfaceOffset,
                rotation = rotation,
                scale = Mathf.Lerp(layer.scaleRange.x, layer.scaleRange.y, NextFloat(random)),
            };

            return true;
        }

        private static float NextFloat(System.Random random) => (float)random.NextDouble();

        #endregion

        #region Output

        private static void WriteChunk(StageTileEntry tile, List<ScatterGroup> groups, Bounds bounds, string scatterDir)
        {
            string path = $"{scatterDir}/SC_r{tile.tileZ}_c{tile.tileX}.asset";

            ScatterChunk chunk = AssetDatabase.LoadAssetAtPath<ScatterChunk>(path);
            if (chunk == null)
            {
                chunk = ScriptableObject.CreateInstance<ScatterChunk>();
                AssetDatabase.CreateAsset(chunk, path);
            }

            chunk.SetContents(tile.tileIndex, groups.ToArray(), bounds, System.Array.Empty<int>());
            EditorUtility.SetDirty(chunk);

            BindChunkToTileScene(tile, chunk);
        }

        /// <summary>
        /// タイルシーンにチャンクへの参照を持たせる。
        /// </summary>
        // タイルと一緒にロード/アンロードさせるため、マニフェストではなくシーン側に持たせる。
        private static void BindChunkToTileScene(StageTileEntry tile, ScatterChunk chunk)
        {
            Scene scene = SceneManager.GetSceneByPath(tile.scenePath);
            if (!scene.isLoaded)
            {
                Debug.LogWarning($"[StageScatterBaker] タイルシーンが開かれていないため参照を設定できません: {tile.scenePath}");
                return;
            }

            StageTileScatter component = FindComponent(scene);
            if (component == null)
            {
                StageTile stageTile = FindStageTile(scene);
                if (stageTile == null)
                {
                    Debug.LogWarning($"[StageScatterBaker] StageTile が見つかりません: {tile.scenePath}");
                    return;
                }

                component = stageTile.gameObject.AddComponent<StageTileScatter>();
            }

            component.Setup(chunk);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static StageTileScatter FindComponent(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                StageTileScatter found = root.GetComponentInChildren<StageTileScatter>(true);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static StageTile FindStageTile(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                StageTile found = root.GetComponentInChildren<StageTile>(true);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        #endregion

        #region Scenes

        private static List<Scene> OpenTileScenes(StageTileManifest manifest)
        {
            List<Scene> opened = new List<Scene>();

            for (int i = 0; i < manifest.Tiles.Length; i++)
            {
                StageTileEntry tile = manifest.Tiles[i];

                EditorUtility.DisplayProgressBar("Stage Scatter Baker",
                    $"タイルシーンを開いています {i + 1}/{manifest.Tiles.Length}...",
                    (float)i / manifest.Tiles.Length);

                if (SceneManager.GetSceneByPath(tile.scenePath).isLoaded)
                {
                    continue;
                }

                opened.Add(EditorSceneManager.OpenScene(tile.scenePath, OpenSceneMode.Additive));
            }

            return opened;
        }

        private static void CloseTileScenes(List<Scene> scenes)
        {
            // ベイク前から開いていたシーンはこのリストに入っていないので閉じない
            foreach (Scene scene in scenes)
            {
                if (scene.isLoaded && SceneManager.loadedSceneCount > 1)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        #endregion

        #region Sampling

        /// <summary>
        /// レイヤー1つぶんのマスクサンプラー。
        /// </summary>
        private sealed class LayerSampler
        {
            private float[] maskValues;
            private int maskWidth;
            private int maskHeight;

            public ScatterLayer Layer { get; private set; }

            public bool HasMask => maskValues != null;

            public static LayerSampler[] Build(StageRecipe recipe)
            {
                List<LayerSampler> result = new List<LayerSampler>();

                foreach (ScatterLayer layer in recipe.ScatterLayers)
                {
                    if (layer == null || !layer.IsValid)
                    {
                        continue;
                    }

                    LayerSampler sampler = new LayerSampler { Layer = layer };

                    if (layer.mask != null && layer.mask.isReadable)
                    {
                        sampler.Load(layer.mask, layer.maskChannel);
                    }
                    else if (layer.mask != null)
                    {
                        Debug.LogWarning(
                            $"[StageScatterBaker] マスク '{layer.mask.name}' の Read/Write Enabled が無効なため無視します。");
                    }

                    result.Add(sampler);
                }

                return result.ToArray();
            }

            private void Load(Texture2D texture, MaskChannel channel)
            {
                maskWidth = texture.width;
                maskHeight = texture.height;
                maskValues = new float[maskWidth * maskHeight];

                Color[] pixels = texture.GetPixels();
                for (int i = 0; i < maskValues.Length; i++)
                {
                    Color pixel = pixels[i];
                    maskValues[i] = channel switch
                    {
                        MaskChannel.R => pixel.r,
                        MaskChannel.G => pixel.g,
                        MaskChannel.B => pixel.b,
                        MaskChannel.A => pixel.a,
                        _ => pixel.r,
                    };
                }
            }

            public float SampleMask(float u, float v)
            {
                int x = Mathf.Clamp(Mathf.RoundToInt(Mathf.Clamp01(u) * (maskWidth - 1)), 0, maskWidth - 1);
                int y = Mathf.Clamp(Mathf.RoundToInt(Mathf.Clamp01(v) * (maskHeight - 1)), 0, maskHeight - 1);
                return maskValues[y * maskWidth + x];
            }
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
