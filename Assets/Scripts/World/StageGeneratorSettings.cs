using System;
using System.Collections.Generic;
using UnityEngine;

namespace Blue.World
{
    /// <summary>
    /// ステージのハイトマップを手続き的に生成するための設定。
    /// </summary>
    // 値は全て「水深(m)」と「特徴の実サイズ(m)」で持つ。正規化値やUVで持つと、
    // 水深帯の違うステージへ転用するたびに読み替えが必要になり、
    // 「大陸棚は水深20m」という設計上の意図がデータから読み取れなくなる。
    //
    // 出力は StageRecipe が食うハイトマップ画像そのものなので、この仕組みを使うか
    // 外部ツール(WorldMachine 等)で描いた画像を置くかは、後からいつでも入れ替えられる。
    // ベイカー側はどちらで作られたかを知らない。
    [CreateAssetMenu(fileName = "StageGenerator", menuName = "Blue/ScriptableObject/StageGeneratorSettings")]
    public class StageGeneratorSettings : ScriptableObject
    {
        #region Serialized Fields

        [Header("Target")]
        [Tooltip("生成対象のレシピ。レイアウトと出力先をここから決める")]
        [SerializeField] private StageRecipe recipe;

        [Header("Profile / 断面")]
        [Tooltip("岸から沖へ向かう方向(度)。0でZ+方向、90でX+方向")]
        [SerializeField] private float shoreAngle = 30f;

        [Tooltip("岸際の水深(m)")]
        [SerializeField] private float shoreDepth = 5f;

        [Tooltip("大陸棚の外縁の水深(m)")]
        [SerializeField] private float shelfDepth = 90f;

        [Tooltip("大陸棚がステージに占める割合")]
        [Range(0.05f, 0.9f)]
        [SerializeField] private float shelfExtent = 0.45f;

        [Tooltip("ドロップオフ(斜面)がステージに占める割合")]
        [Range(0.05f, 0.9f)]
        [SerializeField] private float slopeExtent = 0.2f;

        [Tooltip("ドロップオフ下端の水深(m)")]
        [SerializeField] private float slopeDepth = 170f;

        [Tooltip("海盆の水深(m)")]
        [SerializeField] private float basinDepth = 250f;

        [Header("Coastline / 海岸線")]
        [Tooltip("海岸線の蛇行量。0だと断面が直線的になり人工物に見える")]
        [Range(0f, 0.5f)]
        [SerializeField] private float coastlineWander = 0.12f;

        [Tooltip("蛇行の波長(m)。小さいほど入り組んだ海岸線になる")]
        [Min(1f)]
        [SerializeField] private float coastlineWanderScale = 600f;

        [Header("Relief / 起伏")]
        [Tooltip("海山・尾根の特徴サイズ(m)")]
        [Min(1f)]
        [SerializeField] private float ridgeScale = 400f;

        [Tooltip("尾根による起伏の大きさ(m)")]
        [SerializeField] private float ridgeHeight = 40f;

        [Tooltip("大陸棚の上で尾根をどれだけ抑えるか。棚は平坦な方が「浅くて広い」感じが出る")]
        [Range(0f, 1f)]
        [SerializeField] private float ridgeOnShelf = 0.25f;

        [Tooltip("細かい起伏の特徴サイズ(m)")]
        [Min(1f)]
        [SerializeField] private float detailScale = 60f;

        [Tooltip("細かい起伏の大きさ(m)")]
        [SerializeField] private float detailHeight = 6f;

        [Tooltip("ノイズの重ね数。増やすほど細部が出るが生成は遅くなる")]
        [Range(1, 6)]
        [SerializeField] private int octaves = 4;

        [Header("Regions / 領域")]
        [Tooltip("領域ごとに起伏を変える。無効の場合は上の Relief の値が全域に掛かる")]
        [SerializeField] private bool useRegions = true;

        [Tooltip("一辺あたりの領域数。総数はこの二乗")]
        [Range(2, 8)]
        [SerializeField] private int regionCellsPerAxis = 4;

        [Tooltip("領域の中心を揺らす量。0だと正方格子になり分割が見え透ける")]
        [Range(0f, 1f)]
        [SerializeField] private float regionJitter = 0.8f;

        [Tooltip("領域境界のブレンド幅(m)。0だと境界が段差になる")]
        [Min(0f)]
        [SerializeField] private float regionBlend = 60f;

        [Tooltip("領域境界を歪ませる量(m)。0だと境界が直線的な多角形として見える")]
        [Min(0f)]
        [SerializeField] private float regionWarp = 120f;

        [Tooltip("depthBias のブレンド幅を regionBlend の何倍にするか。" +
                 "高さのオフセットは起伏の振幅差より段差として目立つので、広くぼかす")]
        [Range(1f, 8f)]
        [SerializeField] private float regionBiasBlendScale = 3f;

        [Tooltip("領域の性格の一覧。各セルにこのどれかを割り当てる")]
        [SerializeField] private StageRegionProfile[] regionProfiles = BuildDefaultProfiles();

        [Tooltip("セルごとの割り当て。空ならシードから自動で振られる")]
        [SerializeField] private int[] cellAssignments = Array.Empty<int>();

        [Header("Features / 造作")]
        [Tooltip("座標を指定して置く海丘・海嶺・クレーターなど。プレビューの Features モードで置ける")]
        [SerializeField] private StageFeature[] features = Array.Empty<StageFeature>();

        [Tooltip("Max / Min で合成するときの接合部の丸め量(m)。0だと稜線が交差する場所に折り目が出る。" +
                 "大きくすると接合部が膨らんで、実際の地形の合流に近くなる")]
        [Min(0f)]
        [SerializeField] private float featureBlendSmoothness = 20f;

        [Header("Thermal Erosion / 崩落")]
        [Tooltip("崩落の反復回数。0で無効")]
        [Min(0)]
        [SerializeField] private int thermalIterations = 30;

        [Tooltip("この傾斜(度)を超えた斜面が崩れて崖下に堆積する。小さいほど地形が緩やかになる")]
        [Range(10f, 60f)]
        [SerializeField] private float talusAngle = 38f;

        [Header("Seed")]
        [SerializeField] private int seed = 1234;

        #endregion

        #region Properties

        public StageRecipe Recipe => recipe;
        public float ShoreAngle => shoreAngle;
        public float ShoreDepth => shoreDepth;
        public float ShelfDepth => shelfDepth;
        public float ShelfExtent => shelfExtent;
        public float SlopeExtent => slopeExtent;
        public float SlopeDepth => slopeDepth;
        public float BasinDepth => basinDepth;
        public float CoastlineWander => coastlineWander;
        public float CoastlineWanderScale => coastlineWanderScale;
        public float RidgeScale => ridgeScale;
        public float RidgeHeight => ridgeHeight;
        public float RidgeOnShelf => ridgeOnShelf;
        public float DetailScale => detailScale;
        public float DetailHeight => detailHeight;
        public int Octaves => octaves;
        public int ThermalIterations => thermalIterations;
        public float TalusAngle => talusAngle;
        public int Seed => seed;

        public StageFeature[] Features => features;
        public float FeatureBlendSmoothness => featureBlendSmoothness;

        public bool UseRegions => useRegions && regionProfiles != null && regionProfiles.Length > 0;
        public int RegionCellsPerAxis => regionCellsPerAxis;
        public float RegionBiasBlendScale => regionBiasBlendScale;
        public StageRegionProfile[] RegionProfiles => regionProfiles;

        /// <summary>ドロップオフが終わって海盆に入る位置(0-1)</summary>
        public float SlopeEnd => Mathf.Min(1f, shelfExtent + slopeExtent);

        public int CellCount => regionCellsPerAxis * regionCellsPerAxis;

        #endregion

        #region Regions

        /// <summary>
        /// 領域分割を組み立てる。ベイクとプレビューで必ずこれを経由すること。
        /// </summary>
        public StageRegionField CreateRegionField(float worldSize)
        {
            return new StageRegionField(regionCellsPerAxis, worldSize, regionJitter, regionBlend, regionWarp, seed);
        }

        /// <summary>
        /// セルに割り当てられたプロファイルの添字を返す。プロファイルが空なら -1。
        /// </summary>
        // cellAssignments が未設定のときはシードから決める。初期状態から領域差が見える方が、
        // 何のための機能かが伝わる。手で割り当てた時点で配列が実体化して、そちらが優先される。
        public int GetAssignment(int cellIndex)
        {
            int count = regionProfiles?.Length ?? 0;
            if (count == 0)
            {
                return -1;
            }

            if (cellAssignments != null && cellIndex >= 0 && cellIndex < cellAssignments.Length)
            {
                return Mathf.Clamp(cellAssignments[cellIndex], 0, count - 1);
            }

            return (int)(Hash((uint)seed, (uint)cellIndex) % (uint)count);
        }

        /// <summary>セルに対応するプロファイル。割り当てが無い場合は null</summary>
        public StageRegionProfile GetProfile(int cellIndex)
        {
            int index = GetAssignment(cellIndex);
            return index >= 0 ? regionProfiles[index] : null;
        }

        /// <summary>
        /// 領域を使わない場合に全域へ適用するプロファイル。
        /// </summary>
        // 領域機能のオン/オフで生成経路を分けると、比較したいときに挙動が食い違う。
        // グローバル設定を1枚のプロファイルとして扱い、経路を1本に保つ。
        public StageRegionProfile BuildGlobalProfile()
        {
            return new StageRegionProfile
            {
                name = "Global",
                ridgeScale = ridgeScale,
                ridgeHeight = ridgeHeight,
                detailScale = detailScale,
                detailHeight = detailHeight,
                depthBias = 0f,
                talusAngle = talusAngle,
            };
        }

        /// <summary>
        /// 造作の合成モードが height の符号と噛み合っているかを見る。
        /// </summary>
        // 造作の縁では寄与が0へ近づくので、尾根に Min を使うと min(尾根, ほぼ0) ≒ 0 となり、
        // もう一方の造作の輪郭に沿って崖ができる。海溝に Max を使うと同じことが逆向きに起きる。
        // 結果が「なぜか輪郭が出る」という形で現れて原因を追いにくいため、明示的に警告する。
        private void CollectFeatureBlendWarnings(List<string> warnings)
        {
            if (features == null)
            {
                return;
            }

            foreach (StageFeature feature in features)
            {
                if (feature == null || !feature.enabled)
                {
                    continue;
                }

                // Merge は符号から自動で決まるので取り違えようがない
                if (feature.blend == StageFeatureBlend.Merge)
                {
                    continue;
                }

                if (feature.blend == StageFeatureBlend.Min && feature.height > 0f)
                {
                    warnings.Add(
                        $"造作 '{feature.name}' は height が正（隆起）なのに blend が Min です。" +
                        "接合部が凹み、他の造作の輪郭に沿って段差が出ます。Max か Merge を使ってください。");
                }
                else if (feature.blend == StageFeatureBlend.Max && feature.height < 0f)
                {
                    warnings.Add(
                        $"造作 '{feature.name}' は height が負（沈降）なのに blend が Max です。" +
                        "接合部が浅くなり、他の造作の輪郭に沿って段差が出ます。Min か Merge を使ってください。");
                }
            }
        }

        private static uint Hash(uint seed, uint value)
        {
            uint hash = seed ^ 2166136261u;
            hash = (hash ^ value) * 16777619u;
            hash ^= hash >> 13;
            hash *= 2654435761u;
            hash ^= hash >> 16;
            return hash;
        }

        /// <summary>
        /// 初期状態でも領域差が見えるよう、性格の違う4種を用意する。
        /// </summary>
        private static StageRegionProfile[] BuildDefaultProfiles()
        {
            return new[]
            {
                new StageRegionProfile
                {
                    name = "砂地",
                    previewColor = new Color(0.85f, 0.78f, 0.55f),
                    ridgeScale = 600f, ridgeHeight = 8f,
                    detailScale = 80f, detailHeight = 3f,
                    talusAngle = 28f,
                },
                new StageRegionProfile
                {
                    name = "岩礁",
                    previewColor = new Color(0.75f, 0.45f, 0.40f),
                    ridgeScale = 220f, ridgeHeight = 55f,
                    detailScale = 40f, detailHeight = 9f,
                    talusAngle = 50f,
                },
                new StageRegionProfile
                {
                    name = "海丘",
                    previewColor = new Color(0.50f, 0.75f, 0.50f),
                    ridgeScale = 450f, ridgeHeight = 30f,
                    detailScale = 70f, detailHeight = 5f,
                    talusAngle = 36f,
                },
                new StageRegionProfile
                {
                    name = "窪地",
                    previewColor = new Color(0.45f, 0.50f, 0.80f),
                    ridgeScale = 500f, ridgeHeight = 14f,
                    detailScale = 60f, detailHeight = 4f,
                    depthBias = 25f,
                    talusAngle = 32f,
                },
            };
        }

        #endregion

        #region Profile

        /// <summary>
        /// 岸からの距離(0-1)に対する水深(m)。棚 → ドロップオフ → 海盆の3区間。
        /// </summary>
        // ドロップオフだけ SmoothStep を掛けるのは、区間の入口と出口で傾きを0にして
        // 棚と海盆に滑らかに繋ぐため。線形で繋ぐと折れ線が視認できる。
        //
        // 起伏や崩落を含まない素の断面なので、インスペクターのプレビューもこれを引く。
        public float DepthAt(float offshore)
        {
            float shelfEnd = shelfExtent;
            float slopeEnd = SlopeEnd;

            if (offshore < shelfEnd)
            {
                float t = offshore / shelfEnd;
                return Mathf.Lerp(shoreDepth, shelfDepth, t);
            }

            if (offshore < slopeEnd)
            {
                float t = (offshore - shelfEnd) / (slopeEnd - shelfEnd);
                return Mathf.Lerp(shelfDepth, slopeDepth, Mathf.SmoothStep(0f, 1f, t));
            }

            float basinT = (offshore - slopeEnd) / Mathf.Max(1e-4f, 1f - slopeEnd);
            return Mathf.Lerp(slopeDepth, basinDepth, Mathf.SmoothStep(0f, 1f, basinT));
        }

        #endregion

        #region Validation

        /// <summary>
        /// 生成可能かを検証し、問題があればすべて列挙する。
        /// </summary>
        public List<string> Validate()
        {
            List<string> errors = new List<string>();

            if (recipe == null)
            {
                errors.Add("recipe が設定されていません。生成先のレイアウトを決められません。");
                return errors;
            }

            if (!recipe.Layout.Validate(out string layoutError))
            {
                errors.Add($"レシピのレイアウトが不正です: {layoutError}");
            }

            if (shelfExtent + slopeExtent >= 1f)
            {
                errors.Add(
                    $"shelfExtent ({shelfExtent:F2}) と slopeExtent ({slopeExtent:F2}) の合計が1以上です。海盆が生成されません。");
            }

            return errors;
        }

        /// <summary>
        /// 生成はできるが意図と違う結果になりそうな点を列挙する。
        /// </summary>
        public List<string> CollectWarnings()
        {
            List<string> warnings = new List<string>();

            if (recipe == null)
            {
                return warnings;
            }

            StageTileLayout layout = recipe.Layout;

            // 起伏はプロファイルの上下に振れるので、その分を見込んで比較する。
            // 領域を使う場合は一番暴れるプロファイルで見積もる
            float relief = Mathf.Abs(ridgeHeight) * 0.6f + Mathf.Abs(detailHeight) * 0.5f;
            float maxDepthBias = 0f;
            float minDepthBias = 0f;

            if (UseRegions)
            {
                relief = 0f;
                foreach (StageRegionProfile profile in regionProfiles)
                {
                    if (profile == null)
                    {
                        continue;
                    }

                    relief = Mathf.Max(relief, Mathf.Abs(profile.ridgeHeight) * 0.6f + Mathf.Abs(profile.detailHeight) * 0.5f);
                    maxDepthBias = Mathf.Max(maxDepthBias, profile.depthBias);
                    minDepthBias = Mathf.Min(minDepthBias, profile.depthBias);
                }
            }

            // 造作は場所を選ばず置けるので、最悪ケース（一番暴れるものが一番浅い/深い所にある）で見る
            float featureRise = 0f;
            float featureDrop = 0f;

            if (features != null)
            {
                foreach (StageFeature feature in features)
                {
                    if (feature == null || !feature.enabled)
                    {
                        continue;
                    }

                    float amplitude = Mathf.Abs(feature.height) + Mathf.Abs(feature.roughness);
                    if (feature.height >= 0f)
                    {
                        featureRise = Mathf.Max(featureRise, amplitude);
                    }
                    else
                    {
                        featureDrop = Mathf.Max(featureDrop, amplitude);
                    }
                }
            }

            float shallowest = Mathf.Min(shoreDepth, shelfDepth) - relief + minDepthBias - featureRise;
            float deepest = basinDepth + relief + maxDepthBias + featureDrop;

            if (-shallowest > layout.MaxHeight)
            {
                warnings.Add(
                    $"最浅部が Y {-shallowest:F0}m まで上がる見込みで、レイアウトの上限 {layout.MaxHeight:F0}m を超えます。" +
                    "超えた分は平坦に潰れます。");
            }

            if (-deepest < layout.MinHeight)
            {
                warnings.Add(
                    $"最深部が Y {-deepest:F0}m まで下がる見込みで、レイアウトの下限 {layout.MinHeight:F0}m を下回ります。" +
                    "下回った分は平坦に潰れます。");
            }

            float usage = (deepest - shallowest) / layout.HeightRange;
            if (usage < 0.5f)
            {
                warnings.Add(
                    $"地形が高さレンジの {usage * 100f:F0}% しか使っていません。" +
                    "レイアウトの minHeight/maxHeight を地形の実起伏に寄せると、ハイトマップの精度を活かせます。");
            }

            if (shelfDepth > slopeDepth || slopeDepth > basinDepth)
            {
                warnings.Add("水深が shore → shelf → slope → basin の順に深くなっていません。意図した断面か確認してください。");
            }

            CollectFeatureBlendWarnings(warnings);

            return warnings;
        }

        #endregion
    }
}
