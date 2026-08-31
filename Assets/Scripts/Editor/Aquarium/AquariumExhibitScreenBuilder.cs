using Blue.Aquarium;
using Blue.UI.Exhibit;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Blue.Editor
{
    /// <summary>
    /// 展示画面の Canvas と行プレハブを組み立てるエディタユーティリティ
    /// </summary>
    // 画面の作り込みは後で手を入れる前提の、動作確認用の素組み。
    // 位置は必ずアンカーで決める。中央からの固定オフセットで置くと、
    // パネルの大きさを変えた瞬間に重なる
    public static class AquariumExhibitScreenBuilder
    {
        private const string ENTRY_PREFAB_PATH = "Assets/Prefab/UI/ExhibitEntry.prefab";
        private const string FONT_PATH = "Assets/Material/Font/DotGothic16-Regular SDF.asset";

        private const float PANEL_WIDTH = 760f;
        private const float PANEL_HEIGHT = 820f;
        private const float MARGIN = 20f;
        // 行の高さ。各テキストの枠はフォントサイズの1.9倍以上を確保する。
        // 行高に足りない枠は、TMP の設定次第で中身ごと描かれなくなる
        private const float ROW_HEIGHT = 88f;

        // 行の右側で ＋ － が占める幅と、その内側に置く警告文の幅。
        // 名前の枠はこの合計だけ右を空ける。空けないと長い名前が警告に重なる
        private const float BUTTON_BAND = 156f;
        private const float REASON_WIDTH = 220f;

        private static readonly Color PanelColor = new Color(0.08f, 0.12f, 0.18f, 0.94f);
        private static readonly Color RowColor = new Color(1f, 1f, 1f, 0.06f);
        private static readonly Color TextColor = new Color(0.92f, 0.96f, 1f);
        private static readonly Color ReasonColor = new Color(1f, 0.55f, 0.55f);

        /// <summary>
        /// 展示画面を組み、編集モードと繋いだコントローラを返す
        /// </summary>
        public static ExhibitScreenController Build(AquariumSceneBootstrap bootstrap, AquariumEditController edit_controller, AquariumCameraDirector camera_director)
        {
            ExhibitEntryPanel entry_prefab = BuildEntryPrefab();

            GameObject canvas_owner = new GameObject("ExhibitCanvas");
            Canvas canvas = canvas_owner.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvas_owner.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 1f; // 縦を基準に合わせる。横長でも高さが崩れない

            canvas_owner.AddComponent<GraphicRaycaster>();

            GameObject panel = CreatePanel(canvas_owner.transform);
            CanvasGroup group = panel.AddComponent<CanvasGroup>();

            TMP_Text title = CreateText(panel.transform, "TankName", 34f);
            AnchorTop(title.rectTransform, MARGIN, 68f);

            TMP_Text capacity = CreateText(panel.transform, "Capacity", 24f);
            AnchorTop(capacity.rectTransform, MARGIN + 72f, 48f);

            Transform content = CreateScrollContent(panel.transform);

            TMP_Text empty = CreateText(panel.transform, "Empty", 24f);
            AnchorCenter(empty.rectTransform, Vector2.zero, new Vector2(PANEL_WIDTH - MARGIN * 2f, 48f));
            empty.text = "展示できる生物がいません";

            Button close = CreateButton(panel.transform, "Close", "閉じる", 26f);
            AnchorBottom(close.GetComponent<RectTransform>(), MARGIN, new Vector2(200f, 56f));

            ExhibitScreenView view = panel.AddComponent<ExhibitScreenView>();

            SerializedObject view_object = new SerializedObject(view);
            view_object.FindProperty("root").objectReferenceValue = group;
            view_object.FindProperty("entryPrefab").objectReferenceValue = entry_prefab;
            view_object.FindProperty("entryParent").objectReferenceValue = content;
            view_object.FindProperty("tankName").objectReferenceValue = title;
            view_object.FindProperty("capacityLabel").objectReferenceValue = capacity;
            view_object.FindProperty("emptyLabel").objectReferenceValue = empty;
            view_object.FindProperty("closeButton").objectReferenceValue = close;
            view_object.ApplyModifiedProperties();

            ExhibitScreenController controller = canvas_owner.AddComponent<ExhibitScreenController>();

            SerializedObject controller_object = new SerializedObject(controller);
            controller_object.FindProperty("bootstrap").objectReferenceValue = bootstrap;
            controller_object.FindProperty("view").objectReferenceValue = view;
            controller_object.FindProperty("editController").objectReferenceValue = edit_controller;
            controller_object.FindProperty("cameraDirector").objectReferenceValue = camera_director;
            controller_object.ApplyModifiedProperties();

            return controller;
        }

        private static ExhibitEntryPanel BuildEntryPrefab()
        {
            EnsureFolder("Assets/Prefab/UI");

            GameObject row = new GameObject("ExhibitEntry", typeof(RectTransform));
            Image background = row.AddComponent<Image>();
            background.color = RowColor;

            // 幅は並べる側の LayoutGroup が決める。高さだけ固定する
            LayoutElement layout = row.AddComponent<LayoutElement>();
            layout.minHeight = ROW_HEIGHT;
            layout.preferredHeight = ROW_HEIGHT;

            // 名前と個数は行幅に追従させ、右は ＋－ と警告文のぶんだけ必ず空ける。
            // 固定幅で置くと、行幅が変わったときに重なりが戻ってくる
            float text_right = BUTTON_BAND + REASON_WIDTH + 8f;

            TMP_Text name_label = CreateText(row.transform, "Name", 24f);
            name_label.alignment = TextAlignmentOptions.Left;
            AnchorLine(name_label.rectTransform, 16f, text_right, 20f, 46f);

            TMP_Text count_label = CreateText(row.transform, "Count", 18f);
            count_label.alignment = TextAlignmentOptions.Left;
            AnchorLine(count_label.rectTransform, 16f, text_right, -22f, 36f);

            TMP_Text reason_label = CreateText(row.transform, "Reason", 18f);
            reason_label.alignment = TextAlignmentOptions.Right;
            reason_label.color = ReasonColor;

            // 理由は切ると意味が通らなくなるので、ここだけ折り返して2行に収める
            reason_label.textWrappingMode = TextWrappingModes.Normal;
            AnchorRight(reason_label.rectTransform, BUTTON_BAND, 0f, new Vector2(REASON_WIDTH, 46f));

            Button add = CreateButton(row.transform, "Add", "＋", 26f);
            AnchorRight(add.GetComponent<RectTransform>(), 84f, 0f, new Vector2(64f, 48f));

            Button remove = CreateButton(row.transform, "Remove", "－", 26f);
            AnchorRight(remove.GetComponent<RectTransform>(), 12f, 0f, new Vector2(64f, 48f));

            ExhibitEntryPanel panel = row.AddComponent<ExhibitEntryPanel>();

            SerializedObject panel_object = new SerializedObject(panel);
            panel_object.FindProperty("entityName").objectReferenceValue = name_label;
            panel_object.FindProperty("countLabel").objectReferenceValue = count_label;
            panel_object.FindProperty("reasonLabel").objectReferenceValue = reason_label;
            panel_object.FindProperty("addButton").objectReferenceValue = add;
            panel_object.FindProperty("removeButton").objectReferenceValue = remove;
            panel_object.ApplyModifiedProperties();

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(row, ENTRY_PREFAB_PATH);
            UnityEngine.Object.DestroyImmediate(row);

            return saved.GetComponent<ExhibitEntryPanel>();
        }

        private static GameObject CreatePanel(Transform parent)
        {
            GameObject panel = new GameObject("ExhibitPanel", typeof(RectTransform));
            panel.transform.SetParent(parent, false);

            Image background = panel.AddComponent<Image>();
            background.color = PanelColor;

            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = new Vector2(-40f, 0f);
            rect.sizeDelta = new Vector2(PANEL_WIDTH, PANEL_HEIGHT);

            return panel;
        }

        private static Transform CreateScrollContent(Transform parent)
        {
            GameObject viewport = new GameObject("Viewport", typeof(RectTransform));
            viewport.transform.SetParent(parent, false);
            viewport.AddComponent<RectMask2D>();

            RectTransform viewport_rect = viewport.GetComponent<RectTransform>();

            // 見出しと閉じるボタンの間を埋める。パネルの大きさが変わっても追従する
            Stretch(viewport_rect, MARGIN, MARGIN, MARGIN + 130f, MARGIN + 72f);

            GameObject content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);

            RectTransform content_rect = content.GetComponent<RectTransform>();
            content_rect.anchorMin = new Vector2(0f, 1f);
            content_rect.anchorMax = new Vector2(1f, 1f);
            content_rect.pivot = new Vector2(0.5f, 1f);
            content_rect.offsetMin = new Vector2(0f, 0f);
            content_rect.offsetMax = new Vector2(0f, 0f);

            VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 6f;
            layout.childForceExpandHeight = false;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childControlWidth = true;

            ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect scroll = viewport.AddComponent<ScrollRect>();
            scroll.viewport = viewport_rect;
            scroll.content = content_rect;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30f;

            return content.transform;
        }

        // ---------------- 部品 ----------------

        private static TMP_Text CreateText(Transform parent, string name, float size)
        {
            GameObject owner = new GameObject(name, typeof(RectTransform));
            owner.transform.SetParent(parent, false);

            TextMeshProUGUI text = owner.AddComponent<TextMeshProUGUI>();
            text.fontSize = size;
            text.color = TextColor;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;

            // overflowMode は既定のまま触らない。
            // Ellipsis は省略記号の字形をフォントに要求し、Truncate は枠の高さが
            // 行高を下回ると行ごと捨てる。どちらもテキストが丸ごと消える。
            // はみ出しは枠を重ねない配置と、行高に足りる高さで防ぐ

            // 既定のフォントは日本語の字形を持たないため、明示的に差し替える
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_PATH);
            if (font != null) text.font = font;

            return text;
        }

        private static Button CreateButton(Transform parent, string name, string label, float size)
        {
            GameObject owner = new GameObject(name, typeof(RectTransform));
            owner.transform.SetParent(parent, false);

            Image background = owner.AddComponent<Image>();
            background.color = new Color(0.25f, 0.45f, 0.65f);

            Button button = owner.AddComponent<Button>();
            button.targetGraphic = background;

            TMP_Text text = CreateText(owner.transform, "Label", size);
            text.text = label;
            Stretch(text.rectTransform, 0f, 0f, 0f, 0f);

            return button;
        }

        // ---------------- アンカー ----------------

        private static void Stretch(RectTransform rect, float left, float right, float top, float bottom)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        private static void AnchorTop(RectTransform rect, float top, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(MARGIN, -(top + height));
            rect.offsetMax = new Vector2(-MARGIN, -top);
        }

        private static void AnchorBottom(RectTransform rect, float bottom, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, bottom);
            rect.sizeDelta = size;
        }

        // 行幅に追従する横一列。左右の余白を指定し、高さと上下位置だけ固定する
        private static void AnchorLine(RectTransform rect, float left, float right, float y, float height)
        {
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(left, y - height * 0.5f);
            rect.offsetMax = new Vector2(-right, y + height * 0.5f);
        }

        private static void AnchorLeft(RectTransform rect, float x, float y, Vector2 size)
        {
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = size;
        }

        private static void AnchorRight(RectTransform rect, float x, float y, Vector2 size)
        {
            rect.anchorMin = new Vector2(1f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = new Vector2(-x, y);
            rect.sizeDelta = size;
        }

        private static void AnchorCenter(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void EnsureFolder(string folder_path)
        {
            if (AssetDatabase.IsValidFolder(folder_path)) return;

            string parent = System.IO.Path.GetDirectoryName(folder_path).Replace('\\', '/');
            string leaf = System.IO.Path.GetFileName(folder_path);

            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
