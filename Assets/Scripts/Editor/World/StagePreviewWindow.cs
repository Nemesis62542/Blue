using System.Diagnostics;
using Blue.World;
using UnityEditor;
using UnityEngine;

namespace Blue.Editor.World
{
    /// <summary>
    /// StageGeneratorSettings のパラメータを触りながら、生成結果を俯瞰で確認するウィンドウ。
    /// </summary>
    // ベイクまで回さないと結果が見えないと、パラメータの意味が体で分からないまま
    // 総当たりで触ることになる。ここでは低解像度で同じ生成関数を呼んで即座に描く。
    //
    // ノイズはワールド座標で引いているため、低解像度でも大きな地形は最終結果と一致する。
    // 崩落の閾値も cellSize から導くので斜面の限界角度は保たれる。
    // 一致しないのはサンプル間隔より細かいディテールだけ。
    public class StagePreviewWindow : EditorWindow
    {
        #region Constants

        private const float PARAM_WIDTH = 340f;

        private static readonly int[] PREVIEW_RESOLUTIONS = { 129, 257, 513 };

        private static GUIStyle compassLabelStyle;

        #endregion

        #region Fields

        [SerializeField] private StageGeneratorSettings settings;
        [SerializeField] private int previewResolution = 257;
        [SerializeField] private PreviewMode mode = PreviewMode.DepthShaded;
        [SerializeField] private bool showContours = true;
        [SerializeField] private float contourInterval = 25f;
        [SerializeField] private bool autoRefresh = true;
        [SerializeField] private int selectedProfile;
        [SerializeField] private int selectedFeature = -1;
        [SerializeField] private bool pathEditMode;

        private bool draggingFeature;
        private int draggingPathPoint = -1;
        private Vector2 lastDragWorld;

        private SerializedObject serializedSettings;
        private Texture2D previewTexture;
        private StageRegionField regionField;
        private Vector2 paramScroll;

        private double lastBuildMs;
        private float minHeight;
        private float maxHeight;
        private int clippedCount;
        private int sampleCount;
        private bool isStale;

        private enum PreviewMode
        {
            /// <summary>水深のグラデーションに陰影を掛ける。起伏の読み取り用</summary>
            DepthShaded,

            /// <summary>断面プロファイルの水深帯で塗り分ける。面積配分の確認用</summary>
            DepthBands,

            /// <summary>リージョンで塗り分ける。クリックで割り当てを変更できる</summary>
            Regions,

            /// <summary>造作の配置。クリックで選択、ドラッグで移動</summary>
            Features,
        }

        #endregion

        #region Window

        [MenuItem("Blue/World/Stage Preview")]
        public static void Open()
        {
            StagePreviewWindow window = GetWindow<StagePreviewWindow>("Stage Preview");
            window.minSize = new Vector2(820f, 560f);
        }

        public static void Open(StageGeneratorSettings target)
        {
            StagePreviewWindow window = GetWindow<StagePreviewWindow>("Stage Preview");
            window.minSize = new Vector2(820f, 560f);
            window.SetSettings(target);
        }

        private void OnEnable()
        {
            Undo.undoRedoPerformed += OnUndoRedo;

            if (settings == null)
            {
                settings = Selection.activeObject as StageGeneratorSettings;
            }

            SetSettings(settings);
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
            ReleaseTexture();
        }

        private void OnUndoRedo()
        {
            isStale = true;
            Repaint();
        }

        private void SetSettings(StageGeneratorSettings target)
        {
            settings = target;
            serializedSettings = settings != null ? new SerializedObject(settings) : null;
            isStale = true;
        }

        #endregion

        #region GUI

        private void OnGUI()
        {
            DrawHeader();

            if (settings == null)
            {
                EditorGUILayout.HelpBox(
                    "StageGeneratorSettings を割り当ててください。\n" +
                    "Assets > Create > Blue > ScriptableObject > StageGeneratorSettings で作成できます。",
                    MessageType.Info);
                return;
            }

            if (settings.Recipe == null)
            {
                EditorGUILayout.HelpBox("設定の recipe が未割り当てです。レイアウトを決められません。", MessageType.Warning);
                return;
            }

            if (isStale && autoRefresh)
            {
                RebuildPreview();
            }

            EditorGUILayout.BeginHorizontal();
            DrawParameters();
            DrawPreviewColumn();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            EditorGUI.BeginChangeCheck();
            StageGeneratorSettings picked = (StageGeneratorSettings)EditorGUILayout.ObjectField(
                settings, typeof(StageGeneratorSettings), false, GUILayout.Width(240f));
            if (EditorGUI.EndChangeCheck())
            {
                SetSettings(picked);
            }

            GUILayout.FlexibleSpace();

            autoRefresh = GUILayout.Toggle(autoRefresh, "自動更新", EditorStyles.toolbarButton, GUILayout.Width(70f));

            using (new EditorGUI.DisabledScope(settings == null || settings.Recipe == null))
            {
                if (GUILayout.Button("更新", EditorStyles.toolbarButton, GUILayout.Width(50f)))
                {
                    RebuildPreview();
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// 設定アセットのフィールドをそのまま描く。
        /// </summary>
        // インスペクターを埋め込むとカスタムエディタ側の断面図とボタンが二重に出るため、
        // SerializedObject を直接舐める。Header や Tooltip はこの方法でも維持される。
        private void DrawParameters()
        {
            paramScroll = EditorGUILayout.BeginScrollView(paramScroll, GUILayout.Width(PARAM_WIDTH));

            serializedSettings.Update();

            EditorGUI.BeginChangeCheck();

            SerializedProperty property = serializedSettings.GetIterator();
            bool enterChildren = true;
            while (property.NextVisible(enterChildren))
            {
                enterChildren = false;

                if (property.propertyPath == "m_Script")
                {
                    continue;
                }

                EditorGUILayout.PropertyField(property, true);
            }

            if (EditorGUI.EndChangeCheck())
            {
                serializedSettings.ApplyModifiedProperties();
                isStale = true;
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawPreviewColumn()
        {
            EditorGUILayout.BeginVertical();

            DrawPreviewOptions();
            DrawPreviewImage();
            DrawFooter();

            EditorGUILayout.EndVertical();
        }

        private void DrawPreviewOptions()
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUI.BeginChangeCheck();

            mode = (PreviewMode)EditorGUILayout.EnumPopup(mode, GUILayout.Width(120f));

            int resolutionIndex = Mathf.Max(0, System.Array.IndexOf(PREVIEW_RESOLUTIONS, previewResolution));
            string[] resolutionLabels = new string[PREVIEW_RESOLUTIONS.Length];
            for (int i = 0; i < PREVIEW_RESOLUTIONS.Length; i++)
            {
                resolutionLabels[i] = $"{PREVIEW_RESOLUTIONS[i]}²";
            }

            int pickedResolution = EditorGUILayout.Popup(resolutionIndex, resolutionLabels, GUILayout.Width(70f));

            showContours = EditorGUILayout.ToggleLeft("等深線", showContours, GUILayout.Width(60f));

            using (new EditorGUI.DisabledScope(!showContours))
            {
                contourInterval = EditorGUILayout.FloatField(contourInterval, GUILayout.Width(45f));
                GUILayout.Label("m", GUILayout.Width(14f));
            }

            if (EditorGUI.EndChangeCheck())
            {
                previewResolution = PREVIEW_RESOLUTIONS[pickedResolution];
                contourInterval = Mathf.Max(1f, contourInterval);
                isStale = true;
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            DrawRegionTools();
            DrawFeatureTools();
        }

        /// <summary>
        /// 造作の追加と、選択中の造作のパラメータ編集。
        /// </summary>
        // 左の設定一覧にも配列として出ているが、地図上で選んだものが配列の何番目かは
        // 数えないと分からない。選択中のものだけをここに出す。
        private void DrawFeatureTools()
        {
            if (mode != PreviewMode.Features)
            {
                return;
            }

            StageFeature[] features = settings.Features;

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("造作を追加", GUILayout.Width(90f)))
            {
                AddFeature(Vector2.zero);
            }

            bool hasSelection = features != null && selectedFeature >= 0 && selectedFeature < features.Length;

            using (new EditorGUI.DisabledScope(!hasSelection))
            {
                pathEditMode = GUILayout.Toggle(
                    pathEditMode && hasSelection, "パス編集", EditorStyles.miniButton, GUILayout.Width(70f));
            }

            GUILayout.Label(
                pathEditMode
                    ? "地図をクリックで点を追加 / 点をドラッグで移動 / Alt+クリックで点を削除"
                    : "クリックで選択 / ドラッグで移動 / Shift+クリックで追加",
                EditorStyles.miniLabel);

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            if (features == null || selectedFeature < 0 || selectedFeature >= features.Length)
            {
                return;
            }

            serializedSettings.Update();
            SerializedProperty element = serializedSettings
                .FindProperty("features")
                .GetArrayElementAtIndex(selectedFeature);

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(
                element.FindPropertyRelative("name"), GUIContent.none, GUILayout.Width(110f));
            EditorGUILayout.PropertyField(
                element.FindPropertyRelative("shape"), GUIContent.none, GUILayout.Width(80f));
            EditorGUILayout.PropertyField(
                element.FindPropertyRelative("blend"), GUIContent.none, GUILayout.Width(55f));
            EditorGUILayout.PropertyField(
                element.FindPropertyRelative("enabled"), GUIContent.none, GUILayout.Width(20f));

            GUILayout.Space(8f);

            DrawCompactField(element, "radius", "半径", 50f);
            DrawCompactField(element, "height", "高さ", 50f);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            DrawCompactField(element, "elongation", "伸長", 45f);
            DrawCompactField(element, "rotation", "回転", 45f);
            DrawCompactField(element, "roughness", "荒れ", 45f);
            DrawCompactField(element, "roughnessScale", "荒れ幅", 45f);

            SerializedProperty path = element.FindPropertyRelative("path");
            GUILayout.Label($"パス {path.arraySize}点", EditorStyles.miniLabel, GUILayout.Width(60f));

            bool clearPathRequested = path.arraySize > 0 && GUILayout.Button("パス消去", GUILayout.Width(65f));

            GUILayout.FlexibleSpace();

            // 削除は EndChangeCheck を閉じてから行う。ここで return すると
            // BeginChangeCheck が閉じられず、以降の GUI が壊れる
            bool deleteRequested = GUILayout.Button("削除", GUILayout.Width(50f));

            EditorGUILayout.EndHorizontal();

            if (clearPathRequested)
            {
                path.ClearArray();
            }

            if (EditorGUI.EndChangeCheck() || clearPathRequested)
            {
                serializedSettings.ApplyModifiedProperties();
                isStale = true;
            }

            if (deleteRequested)
            {
                DeleteFeature(selectedFeature);
            }
        }

        private static void DrawCompactField(SerializedProperty element, string path, string label, float width)
        {
            GUILayout.Label(label, EditorStyles.miniLabel, GUILayout.Width(label.Length * 13f));
            EditorGUILayout.PropertyField(
                element.FindPropertyRelative(path), GUIContent.none, GUILayout.Width(width));
        }

        /// <summary>
        /// リージョンの割り当て操作。
        /// </summary>
        // セルの形はシードが決めるが、そこに何を置くかは人間が決める。
        // 配列を Inspector で番号指定するより、地図を直接クリックする方が空間を把握しやすい。
        private void DrawRegionTools()
        {
            if (mode != PreviewMode.Regions)
            {
                return;
            }

            if (!settings.UseRegions)
            {
                EditorGUILayout.HelpBox("useRegions が無効か、regionProfiles が空です。", MessageType.Warning);
                return;
            }

            EditorGUILayout.BeginHorizontal();

            StageRegionProfile[] profiles = settings.RegionProfiles;
            string[] names = new string[profiles.Length];
            for (int i = 0; i < profiles.Length; i++)
            {
                names[i] = profiles[i] != null ? $"{i}: {profiles[i].name}" : $"{i}: (null)";
            }

            GUILayout.Label("割り当てるリージョン", GUILayout.Width(110f));
            selectedProfile = EditorGUILayout.Popup(
                Mathf.Clamp(selectedProfile, 0, profiles.Length - 1), names, GUILayout.Width(140f));

            GUILayout.Label("地図をクリックでセルに割り当て", EditorStyles.miniLabel);

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawPreviewImage()
        {
            Rect area = GUILayoutUtility.GetRect(
                10f, 10f, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            float side = Mathf.Max(16f, Mathf.Min(area.width, area.height));
            Rect image = new Rect(area.x + (area.width - side) * 0.5f, area.y, side, side);

            EditorGUI.DrawRect(image, new Color(0.08f, 0.09f, 0.11f));

            if (previewTexture != null)
            {
                GUI.DrawTexture(image, previewTexture, ScaleMode.ScaleToFit);
            }

            DrawCompass(image);
            HandleRegionClick(image);
            DrawFeatureMarkers(image);
            HandleFeatureInput(image);
        }

        private void DrawFeatureMarkers(Rect image)
        {
            if (mode != PreviewMode.Features || settings.Features == null)
            {
                return;
            }

            float worldSize = settings.Recipe.Layout.WorldSize;
            StageFeature[] features = settings.Features;

            Handles.BeginGUI();

            for (int i = 0; i < features.Length; i++)
            {
                StageFeature feature = features[i];
                if (feature == null)
                {
                    continue;
                }

                bool isSelected = i == selectedFeature;
                Handles.color = feature.enabled
                    ? (isSelected ? new Color(1f, 0.95f, 0.4f) : new Color(1f, 1f, 1f, 0.55f))
                    : new Color(1f, 1f, 1f, 0.25f);

                float thickness = isSelected ? 2.5f : 1.5f;

                if (feature.UsesPath)
                {
                    DrawPathMarker(feature, image, worldSize, thickness, isSelected);
                    continue;
                }

                Handles.DrawAAPolyLine(thickness, BuildOutline(feature, image, worldSize));

                Vector2 center = WorldToGui(feature.position, image, worldSize);
                Handles.DrawAAPolyLine(
                    isSelected ? 2.5f : 1.5f,
                    new Vector3(center.x - 4f, center.y), new Vector3(center.x + 4f, center.y));
                Handles.DrawAAPolyLine(
                    isSelected ? 2.5f : 1.5f,
                    new Vector3(center.x, center.y - 4f), new Vector3(center.x, center.y + 4f));
            }

            Handles.EndGUI();
        }

        /// <summary>
        /// 折れ線の造作を描く。中心線と、幅を示す両側のオフセット線、制御点。
        /// </summary>
        // 正確な外形（線分の端が丸い帯）を描くのは手間の割に読み取りやすくならないので、
        // 各線分を法線方向に radius だけずらした線を2本引いて幅を示すに留めている。
        // 実際の効果は地形そのものに出ているので、ここは配置の目安があれば足りる。
        private void DrawPathMarker(
            StageFeature feature, Rect image, float worldSize, float thickness, bool isSelected)
        {
            Vector2[] path = feature.path;
            Vector3[] centerLine = new Vector3[path.Length];

            for (int i = 0; i < path.Length; i++)
            {
                Vector2 gui = WorldToGui(path[i], image, worldSize);
                centerLine[i] = new Vector3(gui.x, gui.y, 0f);
            }

            Handles.DrawAAPolyLine(thickness, centerLine);

            Color lineColor = Handles.color;
            Handles.color = new Color(lineColor.r, lineColor.g, lineColor.b, lineColor.a * 0.45f);

            for (int i = 0; i < path.Length - 1; i++)
            {
                Vector2 direction = path[i + 1] - path[i];
                if (direction.sqrMagnitude <= 1e-6f)
                {
                    continue;
                }

                Vector2 normal = new Vector2(-direction.y, direction.x).normalized * feature.radius;

                DrawWorldLine(path[i] + normal, path[i + 1] + normal, image, worldSize, 1f);
                DrawWorldLine(path[i] - normal, path[i + 1] - normal, image, worldSize, 1f);
            }

            Handles.color = lineColor;

            if (!isSelected)
            {
                return;
            }

            // 選択中だけ制御点を出す。常時出すと点だらけで地形が読めない
            for (int i = 0; i < path.Length; i++)
            {
                Vector2 gui = WorldToGui(path[i], image, worldSize);
                Handles.color = i == draggingPathPoint
                    ? new Color(1f, 0.5f, 0.3f)
                    : new Color(1f, 0.95f, 0.4f);
                Handles.DrawSolidRectangleWithOutline(
                    new Rect(gui.x - 3f, gui.y - 3f, 6f, 6f), Handles.color, Color.black);
            }
        }

        private static void DrawWorldLine(Vector2 from, Vector2 to, Rect image, float worldSize, float thickness)
        {
            Vector2 a = WorldToGui(from, image, worldSize);
            Vector2 b = WorldToGui(to, image, worldSize);
            Handles.DrawAAPolyLine(thickness, new Vector3(a.x, a.y, 0f), new Vector3(b.x, b.y, 0f));
        }

        /// <summary>
        /// 造作の影響範囲の輪郭を GUI 座標で組み立てる。
        /// </summary>
        // StageFeature.Shape は伸長方向をローカルX軸に合わせてから縮めて円に戻している。
        // ここはその逆変換なので、片方だけ直すと輪郭と実際の効果範囲がずれる。
        private static Vector3[] BuildOutline(StageFeature feature, Rect image, float worldSize)
        {
            const int SEGMENTS = 48;

            float radians = -feature.rotation * Mathf.Deg2Rad;
            float cos = Mathf.Cos(radians);
            float sin = Mathf.Sin(radians);

            Vector3[] points = new Vector3[SEGMENTS + 1];

            for (int i = 0; i <= SEGMENTS; i++)
            {
                float angle = (float)i / SEGMENTS * Mathf.PI * 2f;
                float localX = Mathf.Cos(angle) * feature.radius * Mathf.Max(1f, feature.elongation);
                float localZ = Mathf.Sin(angle) * feature.radius;

                Vector2 offset = new Vector2(
                    localX * cos + localZ * sin,
                    -localX * sin + localZ * cos);

                Vector2 gui = WorldToGui(feature.position + offset, image, worldSize);
                points[i] = new Vector3(gui.x, gui.y, 0f);
            }

            return points;
        }

        private void HandleFeatureInput(Rect image)
        {
            if (mode != PreviewMode.Features)
            {
                return;
            }

            Event current = Event.current;
            float worldSize = settings.Recipe.Layout.WorldSize;

            switch (current.type)
            {
                case EventType.MouseDown when current.button == 0 && image.Contains(current.mousePosition):
                {
                    Vector2 world = GuiToWorld(current.mousePosition, image, worldSize);

                    if (pathEditMode)
                    {
                        HandlePathMouseDown(current, image, worldSize, world);
                    }
                    else if (current.shift)
                    {
                        AddFeature(world);
                    }
                    else
                    {
                        selectedFeature = FindFeatureAt(world);
                        draggingFeature = selectedFeature >= 0;
                        lastDragWorld = world;
                    }

                    current.Use();
                    Repaint();
                    break;
                }

                case EventType.MouseDrag when draggingPathPoint >= 0:
                {
                    MovePathPoint(
                        selectedFeature, draggingPathPoint, GuiToWorld(current.mousePosition, image, worldSize));
                    current.Use();
                    Repaint();
                    break;
                }

                case EventType.MouseDrag when draggingFeature:
                {
                    Vector2 world = GuiToWorld(current.mousePosition, image, worldSize);
                    MoveFeatureBy(selectedFeature, world - lastDragWorld);
                    lastDragWorld = world;
                    current.Use();
                    Repaint();
                    break;
                }

                case EventType.MouseUp when draggingFeature || draggingPathPoint >= 0:
                {
                    draggingFeature = false;
                    draggingPathPoint = -1;

                    // ドラッグ中は再生成しない。1フレームごとに崩落まで回すと操作が引っかかる
                    isStale = true;
                    current.Use();
                    break;
                }
            }
        }

        /// <summary>
        /// パス編集中のクリックを、点の掴み・削除・追加に振り分ける。
        /// </summary>
        private void HandlePathMouseDown(Event current, Rect image, float worldSize, Vector2 world)
        {
            StageFeature[] features = settings.Features;
            if (features == null || selectedFeature < 0 || selectedFeature >= features.Length)
            {
                return;
            }

            int hit = FindPathPointAt(features[selectedFeature], current.mousePosition, image, worldSize);

            if (current.alt)
            {
                if (hit >= 0)
                {
                    RemovePathPoint(selectedFeature, hit);
                }

                return;
            }

            if (hit >= 0)
            {
                draggingPathPoint = hit;
                return;
            }

            AppendPathPoint(selectedFeature, world);
        }

        /// <summary>制御点の当たり判定。ワールド距離ではなく画面距離で見る</summary>
        // 半径の小さい造作でも掴めるようにするため。ワールド距離だと
        // ズーム相当の縮尺で掴みやすさが変わってしまう。
        private static int FindPathPointAt(StageFeature feature, Vector2 mouse, Rect image, float worldSize)
        {
            const float GRAB_RADIUS = 8f;

            if (feature.path == null)
            {
                return -1;
            }

            for (int i = 0; i < feature.path.Length; i++)
            {
                if (Vector2.Distance(WorldToGui(feature.path[i], image, worldSize), mouse) <= GRAB_RADIUS)
                {
                    return i;
                }
            }

            return -1;
        }

        private void AppendPathPoint(int featureIndex, Vector2 centeredWorld)
        {
            serializedSettings.Update();

            SerializedProperty path = FeatureProperty(featureIndex).FindPropertyRelative("path");
            int index = path.arraySize;
            path.arraySize = index + 1;
            path.GetArrayElementAtIndex(index).vector2Value = centeredWorld;

            serializedSettings.ApplyModifiedProperties();

            // 2点目が入った時点で楕円から折れ線に切り替わるので、そこで作り直す
            isStale = true;
        }

        private void MovePathPoint(int featureIndex, int pointIndex, Vector2 centeredWorld)
        {
            serializedSettings.Update();
            FeatureProperty(featureIndex)
                .FindPropertyRelative("path")
                .GetArrayElementAtIndex(pointIndex)
                .vector2Value = centeredWorld;
            serializedSettings.ApplyModifiedProperties();
        }

        private void RemovePathPoint(int featureIndex, int pointIndex)
        {
            serializedSettings.Update();
            FeatureProperty(featureIndex).FindPropertyRelative("path").DeleteArrayElementAtIndex(pointIndex);
            serializedSettings.ApplyModifiedProperties();

            isStale = true;
        }

        private SerializedProperty FeatureProperty(int index)
        {
            return serializedSettings.FindProperty("features").GetArrayElementAtIndex(index);
        }

        private int FindFeatureAt(Vector2 centeredWorld)
        {
            StageFeature[] features = settings.Features;
            if (features == null)
            {
                return -1;
            }

            int best = -1;
            float bestInfluence = 0f;

            for (int i = 0; i < features.Length; i++)
            {
                if (features[i] == null)
                {
                    continue;
                }

                features[i].Shape(centeredWorld.x, centeredWorld.y, out float influence);
                if (influence > bestInfluence)
                {
                    bestInfluence = influence;
                    best = i;
                }
            }

            return best;
        }

        /// <summary>
        /// 造作を追加する。
        /// </summary>
        // SerializedProperty で配列を伸ばすとフィールド初期化子が無視されてゼロ初期化される。
        // 半径0・高さ0の造作が生まれて「追加したのに何も起きない」になるため、全て明示的に入れる。
        private void AddFeature(Vector2 centeredWorld)
        {
            serializedSettings.Update();

            SerializedProperty array = serializedSettings.FindProperty("features");
            int index = array.arraySize;
            array.arraySize = index + 1;

            SerializedProperty element = array.GetArrayElementAtIndex(index);
            element.FindPropertyRelative("name").stringValue = $"Feature {index}";
            element.FindPropertyRelative("enabled").boolValue = true;
            element.FindPropertyRelative("shape").enumValueIndex = (int)StageFeatureShape.Dome;
            element.FindPropertyRelative("blend").enumValueIndex = (int)StageFeatureBlend.Add;
            element.FindPropertyRelative("position").vector2Value = centeredWorld;
            element.FindPropertyRelative("radius").floatValue = 120f;
            element.FindPropertyRelative("height").floatValue = 80f;
            element.FindPropertyRelative("elongation").floatValue = 1f;
            element.FindPropertyRelative("rotation").floatValue = 0f;
            element.FindPropertyRelative("roughness").floatValue = 6f;
            element.FindPropertyRelative("roughnessScale").floatValue = 40f;

            // 配列を伸ばすと直前の要素の値が複製されることがある。
            // 前の造作のパスを引き継ぐと、追加した瞬間に意図しない地形が出る
            element.FindPropertyRelative("path").ClearArray();

            serializedSettings.ApplyModifiedProperties();

            selectedFeature = index;
            isStale = true;
        }

        /// <summary>
        /// 造作を相対移動する。折れ線を持つ場合は点も一緒に動かす。
        /// </summary>
        // 掴んだ位置ではなく差分で動かす。マウス位置を直接 position に入れると、
        // 造作の縁を掴んだときに中心が飛んでくる。
        private void MoveFeatureBy(int index, Vector2 delta)
        {
            serializedSettings.Update();

            SerializedProperty element = FeatureProperty(index);
            SerializedProperty position = element.FindPropertyRelative("position");
            position.vector2Value += delta;

            SerializedProperty path = element.FindPropertyRelative("path");
            for (int i = 0; i < path.arraySize; i++)
            {
                SerializedProperty point = path.GetArrayElementAtIndex(i);
                point.vector2Value += delta;
            }

            serializedSettings.ApplyModifiedProperties();
        }

        private void DeleteFeature(int index)
        {
            serializedSettings.Update();
            serializedSettings.FindProperty("features").DeleteArrayElementAtIndex(index);
            serializedSettings.ApplyModifiedProperties();

            selectedFeature = -1;
            isStale = true;
        }

        private static Vector2 WorldToGui(Vector2 centeredWorld, Rect image, float worldSize)
        {
            float u = centeredWorld.x / worldSize + 0.5f;
            float v = centeredWorld.y / worldSize + 0.5f;
            return new Vector2(image.xMin + u * image.width, image.yMin + (1f - v) * image.height);
        }

        private static Vector2 GuiToWorld(Vector2 gui, Rect image, float worldSize)
        {
            float u = (gui.x - image.xMin) / image.width;
            float v = 1f - (gui.y - image.yMin) / image.height;
            return new Vector2((u - 0.5f) * worldSize, (v - 0.5f) * worldSize);
        }

        private void HandleRegionClick(Rect image)
        {
            if (mode != PreviewMode.Regions || regionField == null || !settings.UseRegions)
            {
                return;
            }

            Event current = Event.current;
            if (current.type != EventType.MouseDown || current.button != 0 || !image.Contains(current.mousePosition))
            {
                return;
            }

            Vector2 local = current.mousePosition - new Vector2(image.xMin, image.yMin);
            float worldSize = settings.Recipe.Layout.WorldSize;

            // GUI は上が原点、テクスチャは下が原点なので V を反転する
            float worldX = local.x / image.width * worldSize;
            float worldZ = (1f - local.y / image.height) * worldSize;

            AssignCell(regionField.CellAt(worldX, worldZ), selectedProfile);
            current.Use();
        }

        /// <summary>
        /// セルにリージョンを割り当てる。
        /// </summary>
        // cellAssignments は未設定だとシード由来の値が使われる。手で触った瞬間に
        // 現在の見た目をそのまま配列へ書き出してから変更しないと、
        // 1セル触っただけで他のセルまで別物に変わってしまう。
        private void AssignCell(int cellIndex, int profileIndex)
        {
            serializedSettings.Update();

            SerializedProperty array = serializedSettings.FindProperty("cellAssignments");
            int count = settings.CellCount;

            if (array.arraySize != count)
            {
                int[] snapshot = new int[count];
                for (int i = 0; i < count; i++)
                {
                    snapshot[i] = settings.GetAssignment(i);
                }

                array.arraySize = count;
                for (int i = 0; i < count; i++)
                {
                    array.GetArrayElementAtIndex(i).intValue = snapshot[i];
                }
            }

            if (cellIndex < 0 || cellIndex >= count)
            {
                return;
            }

            array.GetArrayElementAtIndex(cellIndex).intValue = profileIndex;
            serializedSettings.ApplyModifiedProperties();

            isStale = true;
            Repaint();
        }

        /// <summary>
        /// 岸と沖の向きを示す。断面パラメータと俯瞰図の対応が付かないと調整できない。
        /// </summary>
        private void DrawCompass(Rect image)
        {
            float radians = settings.ShoreAngle * Mathf.Deg2Rad;

            // 画面は上が +Z、右が +X。Yは下向きなので符号を反転する
            Vector2 offshore = new Vector2(Mathf.Sin(radians), -Mathf.Cos(radians));
            Vector2 center = image.center;
            float length = image.width * 0.12f;

            Handles.BeginGUI();
            Handles.color = new Color(1f, 0.85f, 0.4f, 0.85f);
            Handles.DrawAAPolyLine(3f, center - offshore * length, center + offshore * length);
            Handles.EndGUI();

            compassLabelStyle ??= new GUIStyle(EditorStyles.miniBoldLabel)
            {
                normal = { textColor = new Color(1f, 0.9f, 0.6f) },
            };

            Vector2 shore = center - offshore * (length + 14f);
            Vector2 sea = center + offshore * (length + 14f);
            GUI.Label(new Rect(shore.x - 10f, shore.y - 8f, 30f, 16f), "岸", compassLabelStyle);
            GUI.Label(new Rect(sea.x - 10f, sea.y - 8f, 30f, 16f), "沖", compassLabelStyle);
        }

        private void DrawFooter()
        {
            StageTileLayout layout = settings.Recipe.Layout;

            // 高さを固定しない。俯瞰図側が ExpandHeight で残りを取るので、
            // ここを縛ると断面図が見切れる
            EditorGUILayout.BeginVertical();

            StageGeneratorSettingsEditor.DrawProfilePreview(settings, layout);

            if (mode == PreviewMode.Regions && settings.UseRegions)
            {
                DrawRegionLegend();
            }

            string legend = mode switch
            {
                PreviewMode.DepthBands =>
                    $"■棚 (〜{settings.ShelfDepth:F0}m)　■斜面 (〜{settings.SlopeDepth:F0}m)　" +
                    $"■海盆 (〜{settings.BasinDepth:F0}m)　■水面上",
                PreviewMode.Regions => $"{settings.CellCount} セル / {settings.RegionProfiles.Length} 種",
                _ => "明るいほど浅い",
            };

            string clipNote = clippedCount > 0
                ? $" / レンジ外 {(float)clippedCount / Mathf.Max(1, sampleCount) * 100f:F1}%"
                : string.Empty;

            EditorGUILayout.LabelField(
                $"{legend}　|　水深 {-maxHeight:F0}〜{-minHeight:F0}m　|　" +
                $"レンジ使用率 {(maxHeight - minHeight) / layout.HeightRange * 100f:F0}%{clipNote}　|　" +
                $"{previewResolution}² を {lastBuildMs:F0}ms",
                EditorStyles.miniLabel);

            using (new EditorGUI.DisabledScope(settings.Validate().Count > 0))
            {
                if (GUILayout.Button("Generate & Bake Terrain", GUILayout.Height(26f)))
                {
                    if (StageHeightmapGenerator.Generate(settings))
                    {
                        StageTerrainBaker.Bake(settings.Recipe);
                    }
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawRegionLegend()
        {
            EditorGUILayout.BeginHorizontal();

            foreach (StageRegionProfile profile in settings.RegionProfiles)
            {
                if (profile == null)
                {
                    continue;
                }

                Rect swatch = GUILayoutUtility.GetRect(12f, 12f, GUILayout.Width(12f), GUILayout.Height(12f));
                EditorGUI.DrawRect(swatch, profile.previewColor);
                GUILayout.Label(profile.name, EditorStyles.miniLabel, GUILayout.Width(60f));
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region Preview Build

        private void RebuildPreview()
        {
            isStale = false;

            if (settings == null || settings.Recipe == null || settings.Validate().Count > 0)
            {
                return;
            }

            StageTileLayout layout = settings.Recipe.Layout;
            if (!layout.Validate(out _))
            {
                return;
            }

            regionField = settings.UseRegions ? settings.CreateRegionField(layout.WorldSize) : null;

            Stopwatch stopwatch = Stopwatch.StartNew();
            float[] heights = StageHeightmapGenerator.BuildHeightField(settings, layout, previewResolution, false);
            stopwatch.Stop();
            lastBuildMs = stopwatch.Elapsed.TotalMilliseconds;

            BuildTexture(heights, layout);
        }

        /// <summary>
        /// ハイトフィールドを俯瞰図に描き起こす。
        /// </summary>
        // 水深のグラデーションだけだと起伏がほとんど読めないので、法線から求めた陰影を掛ける。
        // 崩落や尾根が効いているかは、色ではなく陰影で判断することになる。
        private void BuildTexture(float[] heights, StageTileLayout layout)
        {
            int size = previewResolution;
            sampleCount = heights.Length;

            if (previewTexture == null || previewTexture.width != size)
            {
                ReleaseTexture();
                previewTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
                {
                    hideFlags = HideFlags.HideAndDontSave,
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                };
            }

            float cellSize = layout.WorldSize / (size - 1);
            Vector3 lightDirection = new Vector3(-0.45f, 0.72f, -0.53f).normalized;

            minHeight = float.MaxValue;
            maxHeight = float.MinValue;
            clippedCount = 0;

            Color[] pixels = new Color[heights.Length];

            for (int z = 0; z < size; z++)
            {
                for (int x = 0; x < size; x++)
                {
                    int index = z * size + x;
                    float height = heights[index];

                    if (height < minHeight)
                    {
                        minHeight = height;
                    }

                    if (height > maxHeight)
                    {
                        maxHeight = height;
                    }

                    if (height < layout.MinHeight || height > layout.MaxHeight)
                    {
                        clippedCount++;
                    }

                    float depth = -height;
                    Color color = mode switch
                    {
                        PreviewMode.DepthBands => BandColor(depth),
                        PreviewMode.Regions => RegionColor(
                            (float)x / (size - 1) * layout.WorldSize,
                            (float)z / (size - 1) * layout.WorldSize),
                        _ => DepthColor(height, layout),
                    };

                    color *= Shade(heights, size, x, z, cellSize, lightDirection);

                    if (showContours && IsContour(heights, size, x, z, depth))
                    {
                        color *= 0.72f;
                    }

                    color.a = 1f;
                    pixels[index] = color;
                }
            }

            previewTexture.SetPixels(pixels);
            previewTexture.Apply(false);
        }

        /// <summary>レイアウトの高さレンジに対する相対位置で色を決める</summary>
        // 絶対水深で色を決めると、水深帯の違うステージで全部同じ色になってしまう。
        private static Color DepthColor(float height, StageTileLayout layout)
        {
            if (height > 0f)
            {
                return new Color(0.58f, 0.53f, 0.44f);
            }

            float t = Mathf.Clamp01((layout.MaxHeight - height) / layout.HeightRange);

            if (t < 0.33f)
            {
                return Color.Lerp(new Color(0.52f, 0.86f, 0.84f), new Color(0.24f, 0.63f, 0.72f), t / 0.33f);
            }

            if (t < 0.66f)
            {
                return Color.Lerp(new Color(0.24f, 0.63f, 0.72f), new Color(0.12f, 0.34f, 0.52f), (t - 0.33f) / 0.33f);
            }

            return Color.Lerp(new Color(0.12f, 0.34f, 0.52f), new Color(0.04f, 0.10f, 0.22f), (t - 0.66f) / 0.34f);
        }

        /// <summary>断面プロファイルの水深帯で塗り分ける</summary>
        // offshore ではなく実際の水深で分類する。ノイズと崩落が効いた後に
        // どの帯がどれだけの面積を占めるかが、散布と敵配置の見積もりに直結する。
        private Color BandColor(float depth)
        {
            if (depth <= 0f)
            {
                return new Color(0.60f, 0.55f, 0.48f);
            }

            if (depth < settings.ShelfDepth)
            {
                return new Color(0.42f, 0.74f, 0.55f);
            }

            if (depth < settings.SlopeDepth)
            {
                return new Color(0.85f, 0.68f, 0.34f);
            }

            return new Color(0.36f, 0.40f, 0.68f);
        }

        /// <summary>リージョンの色で塗る。境界を落として区画の切れ目を見せる</summary>
        private Color RegionColor(float worldX, float worldZ)
        {
            if (regionField == null)
            {
                return new Color(0.5f, 0.5f, 0.5f);
            }

            regionField.Sample(worldX, worldZ, out int primaryCell, out _, out float weight);

            StageRegionProfile profile = settings.GetProfile(primaryCell);
            Color color = profile != null ? profile.previewColor : new Color(0.5f, 0.5f, 0.5f);

            return color * Mathf.Lerp(0.5f, 1f, weight);
        }

        private static float Shade(float[] heights, int size, int x, int z, float cellSize, Vector3 lightDirection)
        {
            int left = Mathf.Max(0, x - 1);
            int right = Mathf.Min(size - 1, x + 1);
            int down = Mathf.Max(0, z - 1);
            int up = Mathf.Min(size - 1, z + 1);

            float dx = heights[z * size + left] - heights[z * size + right];
            float dz = heights[down * size + x] - heights[up * size + x];

            Vector3 normal = new Vector3(dx, 2f * cellSize, dz).normalized;
            float lambert = Mathf.Clamp01(Vector3.Dot(normal, lightDirection));

            // 真っ黒にすると起伏が潰れて読めなくなるので下駄を履かせる
            return 0.42f + 0.68f * lambert;
        }

        private bool IsContour(float[] heights, int size, int x, int z, float depth)
        {
            if (x + 1 >= size || z + 1 >= size)
            {
                return false;
            }

            int band = Mathf.FloorToInt(depth / contourInterval);
            int bandRight = Mathf.FloorToInt(-heights[z * size + x + 1] / contourInterval);
            int bandUp = Mathf.FloorToInt(-heights[(z + 1) * size + x] / contourInterval);

            return band != bandRight || band != bandUp;
        }

        private void ReleaseTexture()
        {
            if (previewTexture != null)
            {
                DestroyImmediate(previewTexture);
                previewTexture = null;
            }
        }

        #endregion
    }
}
