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

        [Tooltip("マスクの値にかける倍率")]
        public float weight = 1f;
    }

    /// <summary>
    /// ステージ1つ分の生成レシピ。地形パイプラインの唯一のソース。
    ///
    /// 【設計方針】
    /// Terrain は成果物であってソースデータではない。手作業は「マスクを塗る」に集約し、
    /// ベイカーがそこから TerrainData・タイルシーン・（後のフェーズで）散布物とスポーン情報を生成する。
    /// レシピと元画像だけがコミット対象の正本になる。
    ///
    /// 【再ベイクと Digger の関係】
    /// Digger は編集データを TerrainData アセットの GUID で紐付けている
    /// （Assets/DiggerData/Scenes/(シーン名)/(TerrainDataのGUID)/）。
    /// このためベイカーは TerrainData を作り直さず、既存アセットを上書き更新して GUID を維持する。
    /// GUID が変わると掘削済みの洞窟が全て迷子になる。
    /// </summary>
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
        [Tooltip("散布物プロトタイプの一覧。散布ベイク(フェーズ4)で使用する")]
        [SerializeField] private Scatter.ScatterPrototypeRegistry scatterRegistry;

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
        /// 1件目で止めないのは、エディタ上でまとめて直せるようにするため。
        /// </summary>
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
        /// 8bitテクスチャは高さ方向の量子化が粗く、段差(テラス)として見える。
        /// エラーではないので Validate とは分けて警告として返す。
        /// </summary>
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
