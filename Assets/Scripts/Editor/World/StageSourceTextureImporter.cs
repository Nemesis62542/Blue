using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Blue.Editor.World
{
    /// <summary>
    /// ステージの Source フォルダに置かれたテクスチャを、ベイカーが読める設定に自動で揃える。
    /// </summary>
    // ベイカーは CPU 側で GetPixels するため Read/Write が必須で、圧縮・sRGB 変換・ミップが
    // 入るとハイトマップとマスクの値そのものが変わる。手で設定する運用にすると、
    // 外部ツールから書き出し直すたびに設定が飛んで StageRecipe.Validate に弾かれるため、
    // インポートのたびに強制する。Inspector で変更しても再インポートで元に戻る。
    //
    // 対象は Assets/Stages/(ステージ)/Source/ 配下のみ。Generated/ の生成物や
    // 通常のテクスチャには触らない。
    public class StageSourceTextureImporter : AssetPostprocessor
    {
        #region Constants

        private const string STAGES_ROOT = "Assets/Stages/";
        private const string SOURCE_SEGMENT = "/Source/";

        /// <summary>この語をファイル名に含むものをハイトマップとして扱う</summary>
        private const string HEIGHT_MARKER = "_Height";

        #endregion

        #region Postprocess

        private void OnPreprocessTexture()
        {
            if (!IsStageSource(assetPath))
            {
                return;
            }

            if (assetImporter is not TextureImporter importer)
            {
                return;
            }

            Configure(importer, assetPath);
        }

        /// <summary>
        /// ベイクに使うテクスチャの共通設定を適用する。
        /// </summary>
        private static void Configure(TextureImporter importer, string path)
        {
            // SingleChannel は無圧縮指定でも 8bit に落ちることがあるので Default を使う
            importer.textureType = TextureImporterType.Default;
            importer.isReadable = true;
            importer.sRGBTexture = false;
            importer.mipmapEnabled = false;

            // Repeat だと u=1 が反対側に回り込み、ステージの端が破綻する
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;

            // 1025 のような非2冪をリサイズされるとサンプル位置がずれる
            importer.npotScale = TextureImporterNPOTScale.None;

            // RGBA にマスクを詰める運用があるので、アルファは入力のまま残す
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = false;

            importer.maxTextureSize = 8192;
            importer.textureCompression = TextureImporterCompression.Uncompressed;

            if (IsHeightmap(path))
            {
                ApplyHeightFormat(importer, path);
            }
        }

        /// <summary>
        /// ハイトマップのフォーマットを明示する。
        /// </summary>
        // 無圧縮のままだと Unity は 16bit PNG を RGBA32 に展開してしまい、
        // 元データが 16bit でも高さが 256 段に量子化される。
        // 300m レンジなら 1.2m の段差になり、緩斜面がテラス状に見える。
        private static void ApplyHeightFormat(TextureImporter importer, string path)
        {
            TextureImporterPlatformSettings settings = importer.GetDefaultPlatformTextureSettings();
            settings.textureCompression = TextureImporterCompression.Uncompressed;
            settings.format = IsFloatSource(path) ? TextureImporterFormat.RFloat : TextureImporterFormat.R16;
            importer.SetPlatformTextureSettings(settings);
        }

        #endregion

        #region Menu

        /// <summary>
        /// 既存の Source テクスチャに設定を反映し直す。
        /// </summary>
        // OnPreprocessTexture はインポート時にしか走らないため、
        // この仕組みを入れる前から置かれていたテクスチャには手動で掛ける必要がある。
        [MenuItem("Blue/World/Reimport Stage Source Textures")]
        private static void ReimportAll()
        {
            if (!AssetDatabase.IsValidFolder(STAGES_ROOT.TrimEnd('/')))
            {
                Debug.LogWarning($"[StageSourceTextureImporter] {STAGES_ROOT} が存在しません。");
                return;
            }

            List<string> targets = new List<string>();
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { STAGES_ROOT.TrimEnd('/') }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (IsStageSource(path))
                {
                    targets.Add(path);
                }
            }

            foreach (string path in targets)
            {
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }

            Debug.Log($"[StageSourceTextureImporter] {targets.Count} 件の Source テクスチャを再インポートしました。");
        }

        #endregion

        #region Path

        /// <summary>Assets/Stages/(ステージ)/Source/ 配下かどうか</summary>
        private static bool IsStageSource(string path)
        {
            if (string.IsNullOrEmpty(path) || !path.StartsWith(STAGES_ROOT, StringComparison.Ordinal))
            {
                return false;
            }

            return path.IndexOf(SOURCE_SEGMENT, STAGES_ROOT.Length, StringComparison.Ordinal) >= 0;
        }

        private static bool IsHeightmap(string path)
        {
            return path.IndexOf(HEIGHT_MARKER, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsFloatSource(string path)
        {
            return path.EndsWith(".exr", StringComparison.OrdinalIgnoreCase)
                   || path.EndsWith(".hdr", StringComparison.OrdinalIgnoreCase);
        }

        #endregion
    }
}
