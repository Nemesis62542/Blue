using System;
using System.IO;
using Blue.World;
using UnityEditor;
using UnityEngine;

namespace Blue.Editor.World
{
    /// <summary>
    /// 実ステージのフォルダ構成と StageRecipe の雛形を作成する。
    /// 外部ツール側に入力する値もここで確認できる。
    /// </summary>
    // ステージは水深帯ごとに分かれるだけで、水平サイズとタイル分割は全ステージ共通。
    // 毎回 Inspector で layout を手打ちすると 1m/texel の関係（タイル数 x (解像度-1) + 1）を
    // 崩しやすく、崩れたまま焼くと拡大補間で地形が鈍る。入力を「水深帯」に絞って、
    // レイアウトは StageTileLayout.Stage から取る。
    //
    // ベイク後に layout を変えると、TerrainData の解像度もタイル境界も変わり、
    // Digger で掘った洞窟が全て失われる。既存レシピの layout 上書きに確認を挟むのはこのため。
    public class StageScaffolder : EditorWindow
    {
        #region Constants

        private const string STAGES_ROOT = "Assets/Stages";
        private const string SOURCE_FOLDER = "Source";
        private const string HEIGHT_SUFFIX = "_Height";

        #endregion

        #region Fields

        [SerializeField] private string stageId = "Stage01";
        [SerializeField] private int groupingId = 1;

        [SerializeField] private float topDepth;
        [SerializeField] private float bottomDepth = 250f;
        [SerializeField] private float ceilingMargin = 20f;
        [SerializeField] private float floorMargin = 30f;
        [SerializeField] private int seed = 1234;

        private Vector2 scroll;

        #endregion

        #region Properties

        /// <summary>最浅部のY座標。水面より上に出す余白ぶんだけ持ち上げる</summary>
        private float MaxHeight => -topDepth + ceilingMargin;

        /// <summary>最深部のY座標。洞窟を掘る余地ぶんだけ下げる</summary>
        private float MinHeight => -(bottomDepth + floorMargin);

        private StageTileLayout Layout => StageTileLayout.Stage(MinHeight, MaxHeight);

        private string StageDir => $"{STAGES_ROOT}/{stageId}";

        private string SourceDir => $"{StageDir}/{SOURCE_FOLDER}";

        private string RecipePath => $"{StageDir}/{stageId}_Recipe.asset";

        #endregion

        #region Window

        [MenuItem("Blue/World/Stage Scaffolder")]
        private static void Open()
        {
            StageScaffolder window = GetWindow<StageScaffolder>("Stage Scaffolder");
            window.minSize = new Vector2(420f, 520f);
        }

        private void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);

            DrawIdentity();
            EditorGUILayout.Space();
            DrawDepth();
            EditorGUILayout.Space();
            DrawSummary();
            EditorGUILayout.Space();
            DrawExportSettings();
            EditorGUILayout.Space();
            DrawActions();

            EditorGUILayout.EndScrollView();
        }

        #endregion

        #region Draw

        private void DrawIdentity()
        {
            EditorGUILayout.LabelField("Identity", EditorStyles.boldLabel);
            stageId = EditorGUILayout.TextField(
                new GUIContent("Stage Id", "フォルダ名とアセット名になる。Stage01 のような連番を想定"), stageId);
            groupingId = EditorGUILayout.IntField(
                new GUIContent("Grouping Id", "Terrain の自動接続グループ。ステージごとに別の値にする"), groupingId);
            seed = EditorGUILayout.IntField(
                new GUIContent("Seed", "散布と洞窟生成の乱数シード"), seed);
        }

        private void DrawDepth()
        {
            EditorGUILayout.LabelField("Depth Band", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("水面を 0 とした水深(m)。下向きが正", EditorStyles.miniLabel);

            topDepth = EditorGUILayout.FloatField(
                new GUIContent("Top Depth", "このステージの最も浅い水深"), topDepth);
            bottomDepth = EditorGUILayout.FloatField(
                new GUIContent("Bottom Depth", "このステージの最も深い水深"), bottomDepth);

            ceilingMargin = EditorGUILayout.FloatField(
                new GUIContent("Ceiling Margin", "最浅部より上に取る余白(m)。水面から突き出す岩礁ぶん"), ceilingMargin);
            floorMargin = EditorGUILayout.FloatField(
                new GUIContent("Floor Margin", "最深部より下に取る余白(m)。Digger で洞窟を掘る余地"), floorMargin);
        }

        private void DrawSummary()
        {
            StageTileLayout layout = Layout;

            EditorGUILayout.LabelField("Layout", EditorStyles.boldLabel);

            if (!layout.Validate(out string error))
            {
                EditorGUILayout.HelpBox(error, MessageType.Error);
                return;
            }

            EditorGUILayout.HelpBox(
                $"ワールド : {layout.WorldSize:F0}m 四方 (X/Z は {-layout.WorldSize * 0.5f:F0} 〜 {layout.WorldSize * 0.5f:F0})\n" +
                $"タイル   : {layout.TilesPerAxis}x{layout.TilesPerAxis} = {layout.TileCount} 枚 / 1枚 {layout.TileSize:F0}m\n" +
                $"高さ     : Y {layout.MinHeight:F0} 〜 {layout.MaxHeight:F0} (レンジ {layout.HeightRange:F0}m)\n" +
                $"解像度   : ハイトマップ {layout.HeightmapResolution} / アルファマップ {layout.AlphamapResolution}\n" +
                $"          → ステージ全体で {layout.GlobalHeightSamples}x{layout.GlobalHeightSamples} サンプル " +
                $"({layout.WorldSize / (layout.GlobalHeightSamples - 1):F2}m/texel)",
                MessageType.None);
        }

        private void DrawExportSettings()
        {
            EditorGUILayout.LabelField("外部ツールへの入力値", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(BuildExportSettingsText(), MessageType.None);

            if (GUILayout.Button("クリップボードにコピー"))
            {
                EditorGUIUtility.systemCopyBuffer = BuildExportSettingsText();
                ShowNotification(new GUIContent("コピーしました"));
            }
        }

        /// <summary>
        /// WorldMachine 等で設定する値をまとめる。
        /// </summary>
        // 侵食系デバイスは世界の実寸を見て効き方を変えるため、水平サイズと標高レンジを
        // Unity 側と一致させないと、プレビューと焼き上がりで地形の質感がずれる。
        private string BuildExportSettingsText()
        {
            StageTileLayout layout = Layout;
            int samples = layout.GlobalHeightSamples;
            float step16 = layout.HeightRange / 65535f;

            return
                $"[{stageId}]\n" +
                $"World Extents  : {layout.WorldSize:F0} m x {layout.WorldSize:F0} m\n" +
                $"標高レンジ     : {layout.HeightRange:F0} m\n" +
                $"ビルド解像度   : {samples} x {samples}\n" +
                $"ハイトマップ   : 16bit グレースケール PNG\n" +
                $"                 {SourceDir}/{stageId}{HEIGHT_SUFFIX}.png\n" +
                $"バイオームマスク: 8bit PNG {SourceDir}/{stageId}_Mask_<名前>.png\n" +
                $"正規化の対応   : 0.0 → Y {layout.MinHeight:F0} m (水深 {-layout.MinHeight:F0}m)\n" +
                $"                 1.0 → Y {layout.MaxHeight:F0} m (水深 {-layout.MaxHeight:F0}m)\n" +
                $"16bit の高さ刻み: {step16:F4} m";
        }

        private void DrawActions()
        {
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);

            if (string.IsNullOrWhiteSpace(stageId))
            {
                EditorGUILayout.HelpBox("Stage Id を入力してください。", MessageType.Warning);
                return;
            }

            if (!Layout.Validate(out _))
            {
                return;
            }

            StageRecipe existing = AssetDatabase.LoadAssetAtPath<StageRecipe>(RecipePath);

            if (existing == null)
            {
                EditorGUILayout.LabelField($"作成先: {StageDir}", EditorStyles.miniLabel);

                if (GUILayout.Button("ステージ雛形を作成", GUILayout.Height(28f)))
                {
                    CreateStage();
                }

                return;
            }

            EditorGUILayout.HelpBox(
                $"{RecipePath} は既に存在します。\n" +
                "layout の上書きはベイク済みの Terrain と Digger の掘削データを無効にします。",
                MessageType.Info);

            if (GUILayout.Button("既存のレシピを選択"))
            {
                Selection.activeObject = existing;
                EditorGUIUtility.PingObject(existing);
            }

            if (GUILayout.Button("Source フォルダを開く"))
            {
                EditorUtility.RevealInFinder($"{SourceDir}/");
            }

            if (GUILayout.Button("layout を上書きする"))
            {
                bool confirmed = EditorUtility.DisplayDialog(
                    "Stage Scaffolder",
                    $"'{stageId}' の layout を上書きします。\n\n" +
                    "ベイク済みの場合、TerrainData の解像度とタイル境界が変わるため、\n" +
                    "Digger で掘った洞窟は復元できません。\n\n続けますか？",
                    "上書きする", "キャンセル");

                if (confirmed)
                {
                    ApplyToRecipe(existing, true);
                }
            }
        }

        #endregion

        #region Create

        private void CreateStage()
        {
            EnsureFolder(SourceDir);

            StageRecipe recipe = CreateInstance<StageRecipe>();
            AssetDatabase.CreateAsset(recipe, RecipePath);

            ApplyToRecipe(recipe, true);

            Debug.Log(
                $"[StageScaffolder] '{stageId}' の雛形を作成しました: {StageDir}\n" +
                $"  1. 外部ツールの書き出しを {SourceDir} に置く（インポート設定は自動で適用される）\n" +
                $"  2. レシピの Inspector で heightmap とバイオームを設定する\n" +
                $"  3. Bake Stage Terrain を実行する",
                recipe);
        }

        /// <summary>
        /// ウィンドウの入力値をレシピに書き込む。
        /// </summary>
        // フィールドは private なので SerializedObject 経由で触る。
        private void ApplyToRecipe(StageRecipe recipe, bool writeLayout)
        {
            SerializedObject serializedObject = new SerializedObject(recipe);

            serializedObject.FindProperty("stageId").stringValue = stageId;
            serializedObject.FindProperty("groupingId").intValue = groupingId;
            serializedObject.FindProperty("seed").intValue = seed;

            // 水面 Y=0・下向きが水深、という座標系を全ステージで共有する
            serializedObject.FindProperty("waterLevel").floatValue = 0f;

            if (writeLayout)
            {
                WriteLayout(serializedObject.FindProperty("layout"));
            }

            LinkHeightmap(serializedObject);

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(recipe);
            AssetDatabase.SaveAssets();

            Selection.activeObject = recipe;
            EditorGUIUtility.PingObject(recipe);
        }

        private void WriteLayout(SerializedProperty property)
        {
            StageTileLayout layout = Layout;

            property.FindPropertyRelative("worldSize").floatValue = layout.WorldSize;
            property.FindPropertyRelative("tilesPerAxis").intValue = layout.TilesPerAxis;
            property.FindPropertyRelative("heightmapResolution").intValue = layout.HeightmapResolution;
            property.FindPropertyRelative("alphamapResolution").intValue = layout.AlphamapResolution;
            property.FindPropertyRelative("minHeight").floatValue = layout.MinHeight;
            property.FindPropertyRelative("maxHeight").floatValue = layout.MaxHeight;
        }

        /// <summary>
        /// Source フォルダにハイトマップがあれば繋ぐ。
        /// </summary>
        // 既に設定済みの参照は触らない。書き出し直しはファイル差し替えで済み、
        // GUID が変わらないため参照は生きたままになる。
        private void LinkHeightmap(SerializedObject serializedObject)
        {
            SerializedProperty property = serializedObject.FindProperty("heightmap");
            if (property.objectReferenceValue != null || !AssetDatabase.IsValidFolder(SourceDir))
            {
                return;
            }

            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { SourceDir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!Path.GetFileNameWithoutExtension(path).EndsWith(HEIGHT_SUFFIX, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                property.objectReferenceValue = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                return;
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
