using System;
using System.Collections.Generic;
using UnityEngine;

namespace Blue.World
{
    /// <summary>
    /// マスク画像のどのチャンネルを読むか。
    /// </summary>
    public enum MaskChannel
    {
        R,
        G,
        B,
        A,
    }

    /// <summary>
    /// バイオーム1つと Terrain レイヤーの対応。
    /// マスクの濃さがそのまま Terrain のスプラットウェイトになる。
    /// </summary>
    [Serializable]
    public class BiomeLayerBinding
    {
        public string name = "Biome";

        [Tooltip("このバイオームで塗る Terrain レイヤー")]
        public TerrainLayer terrainLayer;

        [Tooltip("ステージ全体を覆うマスク画像。null の場合は常時ウェイト1（ベースレイヤー扱い）")]
        public Texture2D mask;

        public MaskChannel channel = MaskChannel.R;

        [Tooltip("マスク値の変換範囲。x未満を0、y以上を1として、間を滑らかに繋ぐ。\n" +
                 "x と y が同じなら変換しない（マスク値をそのまま使う）。\n" +
                 "x > y にすると反転する。ベースと切り替えレイヤーで逆向きの範囲を指定すると、" +
                 "きれいに塗り分かる")]
        public Vector2 maskRange;

        [Tooltip("マスクの値にかける倍率")]
        public float weight = 1f;

        /// <summary>
        /// マスク値に変換範囲を適用する。
        /// </summary>
        // 傾斜マスクをそのまま使うと、値が 0.1〜0.2 の緩斜面にも中途半端にウェイトが乗り、
        // 全域が中間色になって「切り替わっている」ように見えない。
        // 「何度から何度で切り替えるか」を指定できるようにして、境界を作れるようにする。
        //
        // 範囲が退化しているとき素通しにするのは、Inspector の + で追加した要素が
        // ゼロ初期化されるため。(0,0) が「変換しない」を意味しないと、
        // 追加した瞬間に全部が0になって何も塗られなくなる。
        public float ApplyRange(float value)
        {
            if (Mathf.Approximately(maskRange.x, maskRange.y))
            {
                return value;
            }

            // InverseLerp は x > y でも符号込みで正しく効くので、反転は範囲の指定だけで済む
            return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(maskRange.x, maskRange.y, value));
        }
    }

    /// <summary>
    /// ステージ1つ分の生成レシピ。地形パイプラインの唯一のソース。
    /// </summary>
    // Terrain は成果物であってソースデータではない。手作業は「マスクを塗る」に集約し、
    // ベイカーがそこから TerrainData・タイルシーン・（後のフェーズで）散布物とスポーン情報を
    // 生成する。レシピと元画像だけがコミット対象の正本になる。
    //
    // ベイカーは TerrainData を作り直さず、既存アセットを上書き更新して GUID を維持する。
    // タイルシーンが TerrainData を参照しているため、作り直すと参照が切れる。
    //
    // Digger の掘削データは Assets/DiggerData/Scenes/(DiggerMaster.sceneDataFolder)/(DiggerSystem.guid)/
    // に置かれる。どちらもタイルシーン側のコンポーネントに保存された値なので、
    // 洞窟を守るうえで本当に重要なのはタイルシーンを作り直さないこと。
    // ベイカーは既存シーンを開き直して更新するため、この条件を満たしている。
    [CreateAssetMenu(fileName = "StageRecipe", menuName = "Blue/ScriptableObject/StageRecipe")]
    public class StageRecipe : ScriptableObject
    {
        #region Serialized Fields

        [Header("Identity")]
        [Tooltip("ステージ識別子。生成物のフォルダ名とアセット名に使う")]
        [SerializeField] private string stageId = "Stage01";

        [Tooltip("Terrain の自動接続グループID。ステージごとに別の値にすること")]
        [SerializeField] private int groupingId;

        [Header("Layout")]
        [SerializeField] private StageTileLayout layout = StageTileLayout.Default;

        [Header("Height Source")]
        [Tooltip("ステージ全体のハイトマップ。Read/Write Enabled が必要。8bitだと段差が出るので16bit以上を推奨")]
        [SerializeField] private Texture2D heightmap;

        [Tooltip("ハイトマップの値をどう高さに写すか。既定は線形")]
        [SerializeField] private AnimationCurve heightCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [Header("Biome / Splat")]
        [Tooltip("空の場合はアルファマップのベイクを丸ごとスキップする")]
        [SerializeField] private BiomeLayerBinding[] biomes = Array.Empty<BiomeLayerBinding>();

        [Header("Scatter / Spawn")]
        [Tooltip("散布物プロトタイプの一覧")]
        [SerializeField] private Scatter.ScatterPrototypeRegistry scatterRegistry;

        [Tooltip("散布の配置ルール。上から順にベイクされる")]
        [SerializeField] private Scatter.ScatterLayer[] scatterLayers = Array.Empty<Scatter.ScatterLayer>();

        [Tooltip("水面のY座標(m)。散布の水深フィルタで使う")]
        [SerializeField] private float waterLevel;

        [Tooltip("散布・洞窟生成の乱数シード。固定である限りベイクは決定的で、永続IDも不変")]
        [SerializeField] private int seed = 1234;

        #endregion

        #region Properties

        public string StageId => stageId;
        public int GroupingId => groupingId;
        public StageTileLayout Layout => layout;
        public Texture2D Heightmap => heightmap;
        public AnimationCurve HeightCurve => heightCurve;
        public BiomeLayerBinding[] Biomes => biomes;
        public Scatter.ScatterPrototypeRegistry ScatterRegistry => scatterRegistry;
        public Scatter.ScatterLayer[] ScatterLayers => scatterLayers;
        public float WaterLevel => waterLevel;
        public int Seed => seed;

        /// <summary>アルファマップをベイクする対象があるか</summary>
        public bool HasBiomes
        {
            get
            {
                if (biomes == null)
                {
                    return false;
                }

                foreach (BiomeLayerBinding biome in biomes)
                {
                    if (biome != null && biome.terrainLayer != null)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        #endregion

        #region Validation

        /// <summary>
        /// ベイク可能かを検証し、問題があればすべて列挙する。
        /// </summary>
        // 1件目で止めないのは、エディタ上でまとめて直せるようにするため。
        public List<string> Validate()
        {
            List<string> errors = new List<string>();

            if (string.IsNullOrWhiteSpace(stageId))
            {
                errors.Add("stageId が空です。");
            }

            if (!layout.Validate(out string layoutError))
            {
                errors.Add(layoutError);
            }

            if (heightmap == null)
            {
                errors.Add("heightmap が設定されていません。");
            }
            else if (!heightmap.isReadable)
            {
                errors.Add($"heightmap '{heightmap.name}' の Read/Write Enabled が無効です。テクスチャのインポート設定で有効にしてください。");
            }

            if (heightCurve == null || heightCurve.length == 0)
            {
                errors.Add("heightCurve が空です。");
            }

            if (biomes != null)
            {
                for (int i = 0; i < biomes.Length; i++)
                {
                    BiomeLayerBinding biome = biomes[i];
                    if (biome == null)
                    {
                        continue;
                    }

                    if (biome.terrainLayer == null)
                    {
                        errors.Add($"biomes[{i}] '{biome.name}' の terrainLayer が未設定です。");
                    }

                    if (biome.mask != null && !biome.mask.isReadable)
                    {
                        errors.Add($"biomes[{i}] のマスク '{biome.mask.name}' の Read/Write Enabled が無効です。");
                    }
                }
            }

            if (scatterRegistry != null && !scatterRegistry.ValidateIds(out string registryError))
            {
                errors.Add(registryError);
            }

            return errors;
        }

        /// <summary>
        /// ベイクは可能だが品質に影響する問題を列挙する。
        /// </summary>
        // 8bitテクスチャは高さ方向の量子化が粗く、段差(テラス)として見える。
        // エラーではないので Validate とは分けて警告として返す。
        public List<string> CollectWarnings()
        {
            List<string> warnings = new List<string>();

            if (heightmap != null && IsLowPrecision(heightmap.format))
            {
                float step = layout.HeightRange / 255f;
                warnings.Add(
                    $"heightmap '{heightmap.name}' が8bitフォーマット({heightmap.format})です。" +
                    $"高さレンジ {layout.HeightRange:F0}m に対して段差が約 {step:F2}m になります。" +
                    "R16 / RFloat / EXR を推奨します。");
            }

            if (heightmap != null)
            {
                int required = layout.GlobalHeightSamples;
                if (heightmap.width < required || heightmap.height < required)
                {
                    warnings.Add(
                        $"heightmap の解像度 {heightmap.width}x{heightmap.height} が、" +
                        $"必要サンプル数 {required}x{required} を下回っています。拡大補間されます。");
                }
            }

            return warnings;
        }

        /// <summary>
        /// 散布設定の問題を列挙する。
        /// </summary>
        // Unity は配列要素を Inspector の + で追加するとき C# のフィールド初期化子を無視して
        // ゼロ初期化する。既定値が効かず density = 0 のまま「1個も置かれない」事故になるため、
        // 退化した値を明示的に検出する。
        public List<string> CollectScatterIssues()
        {
            List<string> issues = new List<string>();

            if (scatterLayers == null || scatterLayers.Length == 0)
            {
                return issues;
            }

            if (scatterRegistry == null)
            {
                issues.Add("scatterRegistry が未設定です。");
                return issues;
            }

            for (int i = 0; i < scatterLayers.Length; i++)
            {
                Scatter.ScatterLayer layer = scatterLayers[i];
                if (layer == null)
                {
                    continue;
                }

                string label = string.IsNullOrEmpty(layer.name) ? $"scatterLayers[{i}]" : layer.name;

                if (layer.spacing <= 0f)
                {
                    issues.Add($"{label}: spacing が 0 です。候補点が生成されません。");
                }

                if (layer.density <= 0f)
                {
                    issues.Add($"{label}: density が 0 です。density は候補点の採用確率なので、0だと1個も置かれません。");
                }

                if (layer.scaleRange.y <= 0f)
                {
                    issues.Add($"{label}: scaleRange の上限が 0 です。配置されてもスケール0で見えません。");
                }

                if (layer.slopeRange.y <= layer.slopeRange.x)
                {
                    issues.Add($"{label}: slopeRange が空の範囲です（{layer.slopeRange.x}〜{layer.slopeRange.y}）。");
                }

                if (layer.depthRange.y <= layer.depthRange.x)
                {
                    issues.Add($"{label}: depthRange が空の範囲です（{layer.depthRange.x}〜{layer.depthRange.y}）。");
                }

                Scatter.ScatterPrototype prototype = scatterRegistry.Find(layer.prototypeId);
                if (prototype == null)
                {
                    issues.Add($"{label}: prototypeId {layer.prototypeId} がレジストリに存在しません。");
                    continue;
                }

                if (layer.instantiate)
                {
                    if (prototype.prefab == null)
                    {
                        issues.Add($"{label}: instantiate が有効ですが、プロトタイプ '{prototype.displayName}' に prefab がありません。");
                    }

                    continue;
                }

                if (prototype.lodMeshes == null || prototype.lodMeshes.Length == 0 || prototype.lodMeshes[0] == null)
                {
                    issues.Add($"{label}: プロトタイプ '{prototype.displayName}' に lodMeshes がありません。インスタンシング描画にはメッシュが必要です。");
                }

                if (prototype.material == null)
                {
                    issues.Add($"{label}: プロトタイプ '{prototype.displayName}' に material がありません。");
                }
                else if (!prototype.material.enableInstancing)
                {
                    issues.Add($"{label}: マテリアル '{prototype.material.name}' の Enable GPU Instancing が無効です。インスタンシング描画されません。");
                }
            }

            return issues;
        }

        private static bool IsLowPrecision(TextureFormat format)
        {
            switch (format)
            {
                case TextureFormat.Alpha8:
                case TextureFormat.R8:
                case TextureFormat.RG16:
                case TextureFormat.RGB24:
                case TextureFormat.RGBA32:
                case TextureFormat.ARGB32:
                case TextureFormat.BGRA32:
                case TextureFormat.RGB565:
                case TextureFormat.DXT1:
                case TextureFormat.DXT5:
                    return true;
                default:
                    return false;
            }
        }

        #endregion
    }
}
