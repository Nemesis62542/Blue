using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Blue.World;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Blue.Editor.World
{
    /// <summary>
    /// StageGeneratorSettings から、ステージのハイトマップを生成して EXR で書き出す。
    /// </summary>
    // 出力は StageRecipe が食う画像そのものなので、ここから先のベイク処理は
    // 外部ツールで描いた画像を置いた場合と完全に同じ経路を通る。
    //
    // 計算は一貫してワールド座標のメートルで行い、最後に一度だけ正規化する。
    // 正規化空間で起伏を足すと、水深帯の違うステージへ設定を転用したときに
    // 「40mの海山」が別の高さになってしまう。
    public static class StageHeightmapGenerator
    {
        private const string SOURCE_FOLDER = "Source";
        private const string HEIGHT_SUFFIX = "_Height";
        private const string REGION_MASK_SUFFIX = "_Mask_Region";
        private const string TERRAIN_MASK_SUFFIX = "_Mask_Terrain";

        /// <summary>1枚のマスク画像に詰められるリージョン数（RGBA）</summary>
        public const int MASK_CHANNELS = 4;

        #region Generate

        /// <summary>
        /// ハイトマップを生成して書き出す。生成できた場合のみ true。
        /// </summary>
        public static bool Generate(StageGeneratorSettings settings)
        {
            if (settings == null)
            {
                return false;
            }

            List<string> errors = settings.Validate();
            if (errors.Count > 0)
            {
                Debug.LogError(
                    $"[StageHeightmapGenerator] 生成を中止しました:\n - {string.Join("\n - ", errors)}", settings);
                return false;
            }

            foreach (string warning in settings.CollectWarnings())
            {
                Debug.LogWarning($"[StageHeightmapGenerator] {warning}", settings);
            }

            StageRecipe recipe = settings.Recipe;
            StageTileLayout layout = recipe.Layout;
            int size = layout.GlobalHeightSamples;

            Stopwatch stopwatch = Stopwatch.StartNew();
            float[] heights;

            try
            {
                EditorUtility.DisplayProgressBar("Stage Heightmap Generator", "断面と起伏を生成中...", 0.1f);
                heights = BuildHeightField(settings, layout, size, true);

                EditorUtility.DisplayProgressBar("Stage Heightmap Generator", "EXR を書き出し中...", 0.9f);
                WriteExr(recipe, layout, heights, size, out string path, out int clipped);

                EditorUtility.DisplayProgressBar("Stage Heightmap Generator", "マスクを書き出し中...", 0.95f);
                WriteMasks(settings, recipe, layout, heights, size);

                stopwatch.Stop();
                LogResult(settings, layout, heights, size, path, clipped, stopwatch.ElapsedMilliseconds);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            return true;
        }

        #endregion

        #region Height Field

        /// <summary>
        /// 指定解像度でハイトフィールド（ワールドY座標(m)の配列）を組み立てる。
        /// </summary>
        // プレビューが低解像度で同じ関数を呼べるように分けてある。
        // ノイズはワールド座標で引いているので、解像度を変えても大きな地形は一致し、
        // 崩落の閾値も cellSize から導くため斜面の限界角度は保たれる。
        // 変わるのはサンプル間隔より細かいディテールだけ。
        public static float[] BuildHeightField(
            StageGeneratorSettings settings, StageTileLayout layout, int size, bool reportProgress)
        {
            float[] heights = BuildHeights(settings, layout, size, out float[] talusAngles);

            if (settings.ThermalIterations > 0)
            {
                float cellSize = layout.WorldSize / (size - 1);
                float[] thresholds = BuildTalusThresholds(talusAngles, cellSize);
                ApplyThermalErosion(heights, size, thresholds, settings.ThermalIterations, reportProgress);
            }

            return heights;
        }

        /// <summary>
        /// 断面プロファイルと起伏から、各サンプルのワールドY座標(m)を求める。
        /// </summary>
        /// <param name="talusAngles">サンプルごとの安息角(度)。崩落で使う</param>
        // 起伏のパラメータはリージョンごとに変わるので、ノイズは固定周波数で持たず
        // サンプルごとに特徴サイズを渡して引く。境界では隣接リージョンの結果と補間する。
        private static float[] BuildHeights(
            StageGeneratorSettings settings, StageTileLayout layout, int size, out float[] talusAngles)
        {
            float[] heights = new float[size * size];
            talusAngles = new float[size * size];
            float worldSize = layout.WorldSize;

            // 岸→沖の方向。ステージの対角にも対応できるよう、射影の幅で正規化する
            float radians = settings.ShoreAngle * Mathf.Deg2Rad;
            Vector2 offshoreDirection = new Vector2(Mathf.Sin(radians), Mathf.Cos(radians));
            float projectionExtent = 0.5f * (Mathf.Abs(offshoreDirection.x) + Mathf.Abs(offshoreDirection.y));

            NoiseField wanderNoise = new NoiseField(settings.Seed, 3);
            NoiseField ridgeNoise = new NoiseField(settings.Seed + 101, settings.Octaves);
            NoiseField detailNoise = new NoiseField(settings.Seed + 202, settings.Octaves);
            NoiseField featureNoise = new NoiseField(settings.Seed + 303, settings.Octaves);

            StageFeature[] features = CollectEnabledFeatures(settings);
            Rect[] featureBounds = BuildFeatureBounds(features);
            float halfWorld = worldSize * 0.5f;

            bool useRegions = settings.UseRegions;
            StageRegionField regionField = useRegions ? settings.CreateRegionField(worldSize) : null;
            StageRegionProfile globalProfile = useRegions ? null : settings.BuildGlobalProfile();

            float biasBlendWidth = regionField != null
                ? regionField.BlendWidth * settings.RegionBiasBlendScale
                : 0f;

            // サンプルごとに確保すると 100万回の割り当てになるので使い回す
            int neighbourCapacity = regionField?.MaxNeighbours ?? 1;
            int[] regionCells = new int[neighbourCapacity];
            float[] regionDistances = new float[neighbourCapacity];
            float[] reliefWeights = new float[neighbourCapacity];
            float[] biasWeights = new float[neighbourCapacity];

            for (int z = 0; z < size; z++)
            {
                float v = (float)z / (size - 1);
                float worldZ = v * worldSize;

                for (int x = 0; x < size; x++)
                {
                    float u = (float)x / (size - 1);
                    float worldX = u * worldSize;

                    float projection = (u - 0.5f) * offshoreDirection.x + (v - 0.5f) * offshoreDirection.y;
                    float offshore = (projection + projectionExtent) / (projectionExtent * 2f);

                    // 海岸線を直線にしないための蛇行
                    offshore += (wanderNoise.Sample(worldX, worldZ, settings.CoastlineWanderScale) - 0.5f)
                                * settings.CoastlineWander;
                    offshore = Mathf.Clamp01(offshore);

                    float depth = settings.DepthAt(offshore);

                    // 尾根は隆起なので水深を浅くする。棚の上では抑える
                    float ridgeMask = Mathf.Lerp(settings.RidgeOnShelf, 1f, Mathf.SmoothStep(0f, 1f, offshore));

                    int index = z * size + x;

                    if (!useRegions)
                    {
                        depth += Relief(globalProfile, ridgeNoise, detailNoise, worldX, worldZ, ridgeMask);
                        talusAngles[index] = globalProfile.talusAngle;
                    }
                    else
                    {
                        int count = regionField.SampleDistances(
                            worldX, worldZ, regionCells, regionDistances, out float minDistance);

                        StageRegionField.ToWeights(
                            regionDistances, count, minDistance, regionField.BlendWidth, reliefWeights);

                        // depthBias だけ広い幅でぼかす。高さのオフセットは起伏の振幅差より
                        // 段差として目立ち、同じ幅だと境界が斜面の折れ目として見える
                        StageRegionField.ToWeights(
                            regionDistances, count, minDistance, biasBlendWidth, biasWeights);

                        float relief = 0f;
                        float bias = 0f;
                        float talus = 0f;

                        for (int i = 0; i < count; i++)
                        {
                            StageRegionProfile profile = settings.GetProfile(regionCells[i]);

                            float biasWeight = biasWeights[i];
                            if (biasWeight > 0f)
                            {
                                bias += profile.depthBias * biasWeight;
                            }

                            // ノイズは重みを持つセルの分だけ引く。内部では1セル、
                            // 境界でも2〜3セルにしかならない
                            float reliefWeight = reliefWeights[i];
                            if (reliefWeight > 0f)
                            {
                                relief += Relief(profile, ridgeNoise, detailNoise, worldX, worldZ, ridgeMask)
                                          * reliefWeight;
                                talus += profile.talusAngle * reliefWeight;
                            }
                        }

                        depth += relief + bias;
                        talusAngles[index] = talus;
                    }

                    // 造作は断面にもリージョンにも縛られない。地図に描ける特徴はここで載る
                    depth -= EvaluateFeatures(
                        features, featureBounds, featureNoise,
                        worldX - halfWorld, worldZ - halfWorld, worldX, worldZ,
                        settings.FeatureBlendSmoothness);

                    heights[index] = -depth;
                }
            }

            return heights;
        }

        /// <summary>
        /// リージョン1つ分の起伏を、断面水深に足す差分(m)として返す。
        /// </summary>
        // 尾根は隆起なので水深を浅くする（負の値になる）。
        // depthBias はここに含めない。起伏より広い幅でぼかすため呼び元が別に足しており、
        // ここでも返すと二重に効いて、設定値の倍の深さの窪地になる。
        private static float Relief(
            StageRegionProfile profile,
            NoiseField ridgeNoise,
            NoiseField detailNoise,
            float worldX,
            float worldZ,
            float ridgeMask)
        {
            float ridge = (ridgeNoise.SampleRidged(worldX, worldZ, profile.ridgeScale) - 0.4f)
                          * profile.ridgeHeight * ridgeMask;
            float detail = (detailNoise.Sample(worldX, worldZ, profile.detailScale) - 0.5f) * profile.detailHeight;

            return -ridge - detail;
        }

        /// <summary>
        /// 造作による隆起量(m)の合計を返す。正で浅くなる。
        /// </summary>
        // 表面の荒れは造作の効き具合で減衰させる。均一に掛けると、造作の外まで
        // ノイズが漏れて周囲の地形が濁る。
        //
        // 合成はモードごとにグループへ集めてから最後に足す。累積値へ順番に
        // Max/Min を掛けると、ある地点で「何番目に効く造作か」で結果が変わり、
        // その切り替わりが先行する造作の縁で起きるため、円弧状の段差ができる。
        // グループ方式なら結果が順序に依存しない。
        //
        // 各モードは、対象が1つしかない地点では加算と同じ結果になる。
        // 造作の縁では寄与が0へ滑らかに向かうので、グループから外れる瞬間も連続。
        private static float EvaluateFeatures(
            StageFeature[] features,
            Rect[] bounds,
            NoiseField noise,
            float centeredX,
            float centeredZ,
            float worldX,
            float worldZ,
            float smoothness)
        {
            float addTotal = 0f;
            FeatureBlendGroup maxGroup = default;
            FeatureBlendGroup minGroup = default;

            Vector2 point = new Vector2(centeredX, centeredZ);

            for (int i = 0; i < features.Length; i++)
            {
                // 折れ線の距離計算は線分数に比例するので、矩形で先に弾く
                if (!bounds[i].Contains(point))
                {
                    continue;
                }

                StageFeature feature = features[i];
                float shape = feature.Shape(centeredX, centeredZ, out float influence);
                if (influence <= 0f)
                {
                    continue;
                }

                float contribution = feature.height * shape;

                if (feature.roughness != 0f)
                {
                    contribution += (noise.Sample(worldX, worldZ, feature.roughnessScale) - 0.5f)
                                    * 2f * feature.roughness * influence;
                }

                switch (feature.ResolvedBlend)
                {
                    case StageFeatureBlend.Max:
                        maxGroup.Add(contribution, influence, true);
                        break;

                    case StageFeatureBlend.Min:
                        minGroup.Add(contribution, influence, false);
                        break;

                    default:
                        addTotal += contribution;
                        break;
                }
            }

            return addTotal + maxGroup.Resolve(true, smoothness) + minGroup.Resolve(false, smoothness);
        }

        /// <summary>
        /// Max / Min グループの合成結果を、要素の順序に依らずに求める。
        /// </summary>
        // 累積値へ順番に SmoothMax を掛けると、3つ以上重なったときに結合則が成り立たず、
        // 丸め幅も直前までの累積に依存するため、配列を並べ替えただけで高さが変わる。
        // 造作を1つ足しただけで既存の地形が最大 k/4 動くと、Digger で彫った後には
        // 洞窟と地形の位置関係がずれる。原因の特定が難しい種類の事故になる。
        //
        // 折り目が出るのは上位2つの面が交差する場所なので、丸めもその2つだけで決まればよい。
        // 上位2件だけ保持すれば、集合が同じなら結果も同じになる。
        private struct FeatureBlendGroup
        {
            private float first;
            private float second;
            private float firstInfluence;
            private float secondInfluence;
            private int count;

            /// <param name="keepHighest">true で大きい方、false で小さい方を上位とする</param>
            public void Add(float value, float influence, bool keepHighest)
            {
                if (count == 0 || (keepHighest ? value > first : value < first))
                {
                    second = first;
                    secondInfluence = firstInfluence;
                    first = value;
                    firstInfluence = influence;
                }
                else if (count == 1 || (keepHighest ? value > second : value < second))
                {
                    second = value;
                    secondInfluence = influence;
                }

                count++;
            }

            /// <summary>グループの合成値。要素が無ければ0</summary>
            public float Resolve(bool keepHighest, float smoothness)
            {
                if (count == 0)
                {
                    return 0f;
                }

                // 1つしか効いていない地点では丸める相手がいないので、加算と同じ結果になる
                if (count == 1)
                {
                    return first;
                }

                // 丸め量は弱い方の効き具合まで落とす。定数のままだと、造作の影響範囲に
                // 入った瞬間に最大 k/4 だけ地面が持ち上がり、輪郭が段差として見える
                float smoothing = smoothness * Mathf.Min(firstInfluence, secondInfluence);

                return keepHighest
                    ? SmoothMax(first, second, smoothing)
                    : SmoothMin(first, second, smoothing);
            }
        }

        /// <summary>
        /// 接合部を丸めた最大値。
        /// </summary>
        // 素の Mathf.Max は値としては連続だが傾きが不連続で、2つの斜面が交差する線が
        // 折り目として見える。k の範囲で滑らかに繋ぎ、接合部を少し盛り上げることで、
        // 尾根が合流したときの堆積に近い形になる。
        private static float SmoothMax(float a, float b, float k)
        {
            if (k <= 0f)
            {
                return Mathf.Max(a, b);
            }

            float h = Mathf.Clamp01(0.5f + 0.5f * (a - b) / k);
            return Mathf.Lerp(b, a, h) + k * h * (1f - h);
        }

        /// <summary>接合部を丸めた最小値。海溝の合流に使う</summary>
        private static float SmoothMin(float a, float b, float k)
        {
            if (k <= 0f)
            {
                return Mathf.Min(a, b);
            }

            float h = Mathf.Clamp01(0.5f + 0.5f * (b - a) / k);
            return Mathf.Lerp(b, a, h) - k * h * (1f - h);
        }

        private static Rect[] BuildFeatureBounds(StageFeature[] features)
        {
            Rect[] bounds = new Rect[features.Length];
            for (int i = 0; i < features.Length; i++)
            {
                bounds[i] = features[i].Bounds();
            }

            return bounds;
        }

        private static StageFeature[] CollectEnabledFeatures(StageGeneratorSettings settings)
        {
            StageFeature[] source = settings.Features;
            if (source == null || source.Length == 0)
            {
                return System.Array.Empty<StageFeature>();
            }

            List<StageFeature> enabled = new List<StageFeature>(source.Length);
            foreach (StageFeature feature in source)
            {
                if (feature != null && feature.enabled)
                {
                    enabled.Add(feature);
                }
            }

            return enabled.ToArray();
        }

        /// <summary>安息角(度)をセル間の高低差の閾値(m)に変換する</summary>
        private static float[] BuildTalusThresholds(float[] talusAngles, float cellSize)
        {
            float[] thresholds = new float[talusAngles.Length];
            for (int i = 0; i < thresholds.Length; i++)
            {
                thresholds[i] = Mathf.Tan(talusAngles[i] * Mathf.Deg2Rad) * cellSize;
            }

            return thresholds;
        }

        #endregion

        #region Thermal Erosion

        /// <summary>
        /// 安息角を超えた斜面を崩して崖下に堆積させる。
        /// </summary>
        // ノイズだけの地形は斜面の角度が場所によらず一様で、崖の下に堆積が無いため作り物に見える。
        // 水中地形は河川侵食よりこの崩落が支配的なので、まずこれだけを入れている。
        //
        // 差分を別バッファに溜めてから一括で適用する。その場で書き換えると走査順で
        // 結果が変わり、シードが同じでも決定的にならない。
        // 閾値はサンプルごとに持つ。リージョンで安息角を変えると、砂地は緩やかに、
        // 岩礁は切り立ったまま、という差がそのまま形に出る。
        private static void ApplyThermalErosion(
            float[] heights, int size, float[] thresholds, int iterations, bool reportProgress)
        {
            float[] delta = new float[heights.Length];
            int[] neighbourOffsets = { -1, 1, -size, size };

            for (int iteration = 0; iteration < iterations; iteration++)
            {
                // プレビューは毎フレーム走るので、進捗ダイアログを出すと操作を奪ってしまう
                if (reportProgress && EditorUtility.DisplayCancelableProgressBar(
                        "Stage Heightmap Generator",
                        $"崩落を計算中... {iteration + 1}/{iterations}",
                        0.1f + 0.8f * iteration / iterations))
                {
                    Debug.LogWarning("[StageHeightmapGenerator] 崩落の計算を中断しました。途中結果で書き出します。");
                    return;
                }

                Array.Clear(delta, 0, delta.Length);

                for (int z = 1; z < size - 1; z++)
                {
                    for (int x = 1; x < size - 1; x++)
                    {
                        int index = z * size + x;
                        float height = heights[index];
                        float threshold = thresholds[index];

                        float maxDrop = 0f;
                        float totalExcess = 0f;

                        foreach (int offset in neighbourOffsets)
                        {
                            float drop = height - heights[index + offset];
                            if (drop <= threshold)
                            {
                                continue;
                            }

                            totalExcess += drop - threshold;
                            if (drop > maxDrop)
                            {
                                maxDrop = drop;
                            }
                        }

                        if (totalExcess <= 0f)
                        {
                            continue;
                        }

                        // 一度に動かす量は最大落差の半分まで。全部動かすと振動する
                        float moved = (maxDrop - threshold) * 0.5f;

                        foreach (int offset in neighbourOffsets)
                        {
                            float drop = height - heights[index + offset];
                            if (drop <= threshold)
                            {
                                continue;
                            }

                            float share = moved * (drop - threshold) / totalExcess;
                            delta[index + offset] += share;
                            delta[index] -= share;
                        }
                    }
                }

                for (int i = 0; i < heights.Length; i++)
                {
                    heights[i] += delta[i];
                }
            }
        }

        #endregion

        #region Output

        /// <summary>
        /// ワールドYをレイアウトのレンジで正規化し、EXR として書き出す。
        /// </summary>
        // レンジ外は clamp するしかないが、黙って潰すと「なぜか地形が平らな一角がある」
        // という分かりにくい不具合になるため、潰れたサンプル数を数えて呼び元に返す。
        private static void WriteExr(
            StageRecipe recipe,
            StageTileLayout layout,
            float[] heights,
            int size,
            out string path,
            out int clipped)
        {
            string recipePath = AssetDatabase.GetAssetPath(recipe);
            string sourceDir = $"{Path.GetDirectoryName(recipePath).Replace('\\', '/')}/{SOURCE_FOLDER}";
            EnsureFolder(sourceDir);

            path = $"{sourceDir}/{recipe.StageId}{HEIGHT_SUFFIX}.exr";

            float minHeight = layout.MinHeight;
            float inverseRange = 1f / layout.HeightRange;
            clipped = 0;

            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBAFloat, false, true);
            Color[] pixels = new Color[heights.Length];

            for (int i = 0; i < heights.Length; i++)
            {
                float normalized = (heights[i] - minHeight) * inverseRange;
                if (normalized < 0f || normalized > 1f)
                {
                    clipped++;
                    normalized = Mathf.Clamp01(normalized);
                }

                pixels[i] = new Color(normalized, normalized, normalized, 1f);
            }

            texture.SetPixels(pixels);
            texture.Apply(false, false);

            // 圧縮を掛ける。海底は滑らかで、しかも R しか使わないのに RGBA 4チャンネルに
            // 同じ値が入るため、非圧縮だと 1025x1025 で 17MB になる。
            // 生成しなおすたびに git の履歴へ積み上がるので、ここは絞っておく
            byte[] exr = texture.EncodeToEXR(
                Texture2D.EXRFlags.OutputAsFloat | Texture2D.EXRFlags.CompressZIP);
            UnityEngine.Object.DestroyImmediate(texture);

            File.WriteAllBytes(path, exr);

            // インポート設定は StageSourceTextureImporter が Source フォルダ配下として自動適用する
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            LinkHeightmap(recipe, path);
        }

        #endregion

        #region Masks

        /// <summary>ステージのリージョンマスクのアセットパス</summary>
        public static string RegionMaskPath(StageRecipe recipe) => MaskPath(recipe, REGION_MASK_SUFFIX);

        /// <summary>ステージの傾斜・水深マスクのアセットパス</summary>
        public static string TerrainMaskPath(StageRecipe recipe) => MaskPath(recipe, TERRAIN_MASK_SUFFIX);

        private static string MaskPath(StageRecipe recipe, string suffix)
        {
            string recipePath = AssetDatabase.GetAssetPath(recipe);
            string sourceDir = $"{Path.GetDirectoryName(recipePath).Replace('\\', '/')}/{SOURCE_FOLDER}";
            return $"{sourceDir}/{recipe.StageId}{suffix}.png";
        }

        /// <summary>
        /// スプラットと散布に使うマスクを書き出す。
        /// </summary>
        // 地形と同じリージョン分割・同じハイトフィールドから作るので、マスクと地形が
        // 構成上ズレない。外部ツールで地形とマスクを別々に作ると、ここの整合を
        // 手で保ち続けることになる。
        //
        // 解像度はハイトマップと同じ。ベイカーはマスクを UV でバイリニア参照するので、
        // アルファマップ解像度と一致していなくてよい。むしろ少し粗い方が、
        // スプラットの境界が硬くなりすぎない。
        private static void WriteMasks(
            StageGeneratorSettings settings, StageRecipe recipe, StageTileLayout layout, float[] heights, int size)
        {
            WriteTerrainMask(recipe, layout, heights, size);

            if (settings.UseRegions)
            {
                WriteRegionMask(settings, recipe, layout, size);
            }
        }

        /// <summary>
        /// R に傾斜、G に水深を詰めたマスク。
        /// </summary>
        // リージョンだけで塗り分けると平地も崖も同じ質感になる。「急斜面は岩」を
        // 出せるかどうかが見た目の説得力を大きく左右するので、傾斜は必ず出しておく。
        private static void WriteTerrainMask(
            StageRecipe recipe, StageTileLayout layout, float[] heights, int size)
        {
            float cellSize = layout.WorldSize / (size - 1);
            float inverseRange = 1f / layout.HeightRange;
            Color[] pixels = new Color[heights.Length];

            for (int z = 0; z < size; z++)
            {
                int down = Mathf.Max(0, z - 1);
                int up = Mathf.Min(size - 1, z + 1);
                float runZ = (up - down) * cellSize;

                for (int x = 0; x < size; x++)
                {
                    int left = Mathf.Max(0, x - 1);
                    int right = Mathf.Min(size - 1, x + 1);

                    float gradientX = (heights[z * size + right] - heights[z * size + left]) / ((right - left) * cellSize);
                    float gradientZ = (heights[up * size + x] - heights[down * size + x]) / runZ;

                    float slope = Mathf.Atan(Mathf.Sqrt(gradientX * gradientX + gradientZ * gradientZ))
                                  * Mathf.Rad2Deg / 90f;
                    float depth = Mathf.Clamp01((layout.MaxHeight - heights[z * size + x]) * inverseRange);

                    pixels[z * size + x] = new Color(slope, depth, 0f, 1f);
                }
            }

            WritePng(pixels, size, TerrainMaskPath(recipe));
        }

        /// <summary>
        /// RGBA にリージョン0〜3の重みを詰めたマスク。
        /// </summary>
        // 同じセルに同じプロファイルが割り当たっていることがあるので、
        // セル単位ではなくプロファイル単位で重みを足し合わせる。
        private static void WriteRegionMask(
            StageGeneratorSettings settings, StageRecipe recipe, StageTileLayout layout, int size)
        {
            StageRegionField field = settings.CreateRegionField(layout.WorldSize);

            int[] cells = new int[field.MaxNeighbours];
            float[] distances = new float[field.MaxNeighbours];
            float[] weights = new float[field.MaxNeighbours];
            float[] profileWeights = new float[MASK_CHANNELS];

            Color[] pixels = new Color[size * size];
            float worldSize = layout.WorldSize;

            for (int z = 0; z < size; z++)
            {
                float worldZ = (float)z / (size - 1) * worldSize;

                for (int x = 0; x < size; x++)
                {
                    float worldX = (float)x / (size - 1) * worldSize;

                    int count = field.SampleDistances(worldX, worldZ, cells, distances, out float minDistance);
                    StageRegionField.ToWeights(distances, count, minDistance, field.BlendWidth, weights);

                    System.Array.Clear(profileWeights, 0, MASK_CHANNELS);

                    for (int i = 0; i < count; i++)
                    {
                        int profile = settings.GetAssignment(cells[i]);
                        if (profile >= 0 && profile < MASK_CHANNELS)
                        {
                            profileWeights[profile] += weights[i];
                        }
                    }

                    pixels[z * size + x] = new Color(
                        profileWeights[0], profileWeights[1], profileWeights[2], profileWeights[3]);
                }
            }

            WritePng(pixels, size, RegionMaskPath(recipe));
        }

        private static void WritePng(Color[] pixels, int size, string path)
        {
            // linear 指定。sRGB 変換が挟まると重みの値そのものが変わる
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false, true);
            texture.SetPixels(pixels);
            texture.Apply(false, false);

            byte[] png = texture.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(texture);

            File.WriteAllBytes(path, png);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }

        #endregion

        #region Output

        /// <summary>
        /// 生成した画像をレシピに繋ぐ。
        /// </summary>
        // 2回目以降は同じパスに上書きするため GUID が変わらず、参照は生きたままになる。
        private static void LinkHeightmap(StageRecipe recipe, string path)
        {
            SerializedObject serializedObject = new SerializedObject(recipe);
            SerializedProperty property = serializedObject.FindProperty("heightmap");

            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (property.objectReferenceValue == texture)
            {
                return;
            }

            property.objectReferenceValue = texture;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(recipe);
            AssetDatabase.SaveAssets();
        }

        private static void LogResult(
            StageGeneratorSettings settings,
            StageTileLayout layout,
            float[] heights,
            int size,
            string path,
            int clipped,
            long elapsedMs)
        {
            float min = float.MaxValue;
            float max = float.MinValue;

            foreach (float height in heights)
            {
                if (height < min)
                {
                    min = height;
                }

                if (height > max)
                {
                    max = height;
                }
            }

            string clipNote = clipped > 0
                ? $"\n  レンジ外: {clipped} サンプル ({(float)clipped / heights.Length * 100f:F2}%) が平坦に潰れました"
                : string.Empty;

            Debug.Log(
                $"[StageHeightmapGenerator] '{settings.Recipe.StageId}' のハイトマップを生成しました。\n" +
                $"  解像度: {size}x{size} ({layout.WorldSize / (size - 1):F2}m/texel)\n" +
                $"  高さ: Y {min:F1} 〜 {max:F1} (水深 {-max:F1} 〜 {-min:F1}m)\n" +
                $"  レイアウト: Y {layout.MinHeight:F0} 〜 {layout.MaxHeight:F0} " +
                $"(レンジ使用率 {(max - min) / layout.HeightRange * 100f:F0}%)\n" +
                $"  出力: {path}{clipNote}\n" +
                $"  マスク: {TerrainMaskPath(settings.Recipe)} (R=傾斜 / G=水深)" +
                (settings.UseRegions ? $"\n        {RegionMaskPath(settings.Recipe)} (RGBA=リージョン0〜3)" : string.Empty) +
                $"\n  所要時間: {elapsedMs} ms\n" +
                "  レシピの Inspector から Bake Stage Terrain を実行してください。",
                settings.Recipe);
        }

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

        #region Noise

        /// <summary>
        /// メートル単位の座標で引ける fBm ノイズ。
        /// </summary>
        // Mathf.PerlinNoise は整数座標で 0.5 に張り付くため、シードは整数ではなく
        // 十分にばらけたオフセットとして与える。
        // 特徴サイズは呼び出しごとに渡す。リージョンによって尾根の粗さが変わるため、
        // 周波数をインスタンスに固定できない。
        private sealed class NoiseField
        {
            private readonly float offsetX;
            private readonly float offsetY;
            private readonly int octaves;

            public NoiseField(int seed, int octaves)
            {
                System.Random random = new System.Random(seed);
                offsetX = (float)random.NextDouble() * 1000f + 13.7f;
                offsetY = (float)random.NextDouble() * 1000f + 71.3f;
                this.octaves = Mathf.Max(1, octaves);
            }

            /// <summary>0-1 の fBm</summary>
            public float Sample(float worldX, float worldZ, float featureScale)
            {
                float sum = 0f;
                float amplitude = 1f;
                float totalAmplitude = 0f;
                float scale = 1f / Mathf.Max(1f, featureScale);

                for (int i = 0; i < octaves; i++)
                {
                    sum += Mathf.PerlinNoise(worldX * scale + offsetX, worldZ * scale + offsetY) * amplitude;
                    totalAmplitude += amplitude;
                    amplitude *= 0.5f;
                    scale *= 2f;
                }

                return sum / totalAmplitude;
            }

            /// <summary>尾根状の fBm。海山や海嶺の稜線を作る</summary>
            public float SampleRidged(float worldX, float worldZ, float featureScale)
            {
                float sum = 0f;
                float amplitude = 1f;
                float totalAmplitude = 0f;
                float scale = 1f / Mathf.Max(1f, featureScale);

                for (int i = 0; i < octaves; i++)
                {
                    float noise = Mathf.PerlinNoise(worldX * scale + offsetX, worldZ * scale + offsetY);
                    float ridge = 1f - Mathf.Abs(noise * 2f - 1f);
                    sum += ridge * ridge * amplitude;
                    totalAmplitude += amplitude;
                    amplitude *= 0.5f;
                    scale *= 2f;
                }

                return sum / totalAmplitude;
            }
        }

        #endregion
    }
}
