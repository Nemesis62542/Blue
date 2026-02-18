using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using Blue.Item;

namespace Blue.Editor
{
    public class ItemManagerWindow : EditorWindow
    {
        private List<ItemData> allItems = new List<ItemData>();
        private List<ItemData> filteredItems = new List<ItemData>();

        private string searchText = "";
        private ItemType? filterType = null;
        private Vector2 scrollPosition;

        private GUIStyle headerStyle;
        private GUIStyle itemStyle;
        private GUIStyle alternateItemStyle;

        [MenuItem("Window/Blue/Item Manager")]
        public static void ShowWindow()
        {
            ItemManagerWindow window = GetWindow<ItemManagerWindow>("Item Manager");
            window.minSize = new Vector2(600, 400);
            window.Show();
        }

        private void OnEnable()
        {
            RefreshItemList();
        }

        private void OnGUI()
        {
            InitializeStyles();

            EditorGUILayout.BeginVertical();

            // ヘッダー部分
            DrawHeader();

            // 検索・フィルタ部分
            DrawSearchAndFilter();

            EditorGUILayout.Space(5);

            // アイテム一覧
            DrawItemList();

            EditorGUILayout.EndVertical();
        }

        private void InitializeStyles()
        {
            if (headerStyle == null)
            {
                headerStyle = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Bold,
                    fontSize = 12,
                    alignment = TextAnchor.MiddleLeft
                };
            }

            if (itemStyle == null)
            {
                itemStyle = new GUIStyle(GUI.skin.box)
                {
                    alignment = TextAnchor.MiddleLeft,
                    padding = new RectOffset(5, 5, 2, 2)
                };
            }

            if (alternateItemStyle == null)
            {
                alternateItemStyle = new GUIStyle(itemStyle);
                alternateItemStyle.normal.background = MakeTexture(2, 2, new Color(0.5f, 0.5f, 0.5f, 0.1f));
            }
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("Item Manager", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("更新", EditorStyles.toolbarButton, GUILayout.Width(50)))
            {
                RefreshItemList();
            }

            if (GUILayout.Button("GUID更新", EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                CacheAllGUIDs();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawSearchAndFilter()
        {
            EditorGUILayout.BeginHorizontal();

            // 検索ボックス
            GUILayout.Label("検索:", GUILayout.Width(40));
            string newSearchText = EditorGUILayout.TextField(searchText);
            if (newSearchText != searchText)
            {
                searchText = newSearchText;
                ApplyFilters();
            }

            GUILayout.Space(10);

            // カテゴリフィルタ
            GUILayout.Label("カテゴリ:", GUILayout.Width(60));

            string[] filterOptions = new string[] { "全て" }.Concat(System.Enum.GetNames(typeof(ItemType))).ToArray();
            int currentIndex = filterType.HasValue ? (int)filterType.Value + 1 : 0;

            int newIndex = EditorGUILayout.Popup(currentIndex, filterOptions, GUILayout.Width(120));
            if (newIndex != currentIndex)
            {
                filterType = newIndex == 0 ? (ItemType?)null : (ItemType)(newIndex - 1);
                ApplyFilters();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawItemList()
        {
            // テーブルヘッダー
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("アイコン", headerStyle, GUILayout.Width(60));
            GUILayout.Label("名前", headerStyle, GUILayout.Width(150));
            GUILayout.Label("カテゴリ", headerStyle, GUILayout.Width(100));
            GUILayout.Label("スタック可", headerStyle, GUILayout.Width(80));
            GUILayout.Label("GUID", headerStyle, GUILayout.Width(100));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            // スクロール可能なアイテムリスト
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            if (filteredItems.Count == 0)
            {
                EditorGUILayout.HelpBox("アイテムが見つかりません", MessageType.Info);
            }
            else
            {
                for (int i = 0; i < filteredItems.Count; i++)
                {
                    DrawItemRow(filteredItems[i], i % 2 == 0);
                }
            }

            EditorGUILayout.EndScrollView();

            // フッター情報
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label($"全{allItems.Count}件中 {filteredItems.Count}件を表示", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawItemRow(ItemData item, bool isEven)
        {
            EditorGUILayout.BeginHorizontal(isEven ? itemStyle : alternateItemStyle, GUILayout.Height(40));

            // アイコン
            if (item.Icon != null)
            {
                Rect iconRect = GUILayoutUtility.GetRect(40, 40, GUILayout.Width(60));
                GUI.DrawTexture(iconRect, item.Icon.texture, ScaleMode.ScaleToFit);
            }
            else
            {
                GUILayout.Label("N/A", GUILayout.Width(60));
            }

            // 名前（クリッカブル）
            if (GUILayout.Button(item.Name, EditorStyles.label, GUILayout.Width(150)))
            {
                EditorGUIUtility.PingObject(item);
                Selection.activeObject = item;
            }

            // カテゴリ
            GUILayout.Label(GetJapaneseItemType(item.Type), GUILayout.Width(100));

            // スタック可否
            GUILayout.Label(item.IsStackable ? "○" : "×", GUILayout.Width(80));

            // GUID（短縮版）
            string guid = item.ItemID;
            string shortGuid = string.IsNullOrEmpty(guid) ? "N/A" : guid.Substring(0, Mathf.Min(8, guid.Length)) + "...";
            if (GUILayout.Button(shortGuid, EditorStyles.miniButton, GUILayout.Width(100)))
            {
                EditorGUIUtility.systemCopyBuffer = guid;
                Debug.Log($"GUID copied: {guid}");
            }

            GUILayout.FlexibleSpace();

            // 選択ボタン
            if (GUILayout.Button("選択", EditorStyles.miniButton, GUILayout.Width(50)))
            {
                EditorGUIUtility.PingObject(item);
                Selection.activeObject = item;
            }

            EditorGUILayout.EndHorizontal();
        }

        private void RefreshItemList()
        {
            allItems.Clear();

            // プロジェクト内のすべてのItemDataを検索
            string[] guids = AssetDatabase.FindAssets("t:ItemData");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
                if (item != null)
                {
                    allItems.Add(item);
                }
            }

            // 名前順にソート
            allItems = allItems.OrderBy(item => item.Name).ToList();

            ApplyFilters();
        }

        private void ApplyFilters()
        {
            filteredItems = allItems.Where(item =>
            {
                // 検索テキストフィルタ
                bool matchesSearch = string.IsNullOrEmpty(searchText) ||
                                     item.Name.ToLower().Contains(searchText.ToLower());

                // カテゴリフィルタ
                bool matchesType = !filterType.HasValue || item.Type == filterType.Value;

                return matchesSearch && matchesType;
            }).ToList();

            Repaint();
        }

        private string GetJapaneseItemType(ItemType type)
        {
            switch (type)
            {
                case ItemType.Consumable: return "消費アイテム";
                case ItemType.Weapon: return "武器";
                case ItemType.Tool: return "道具";
                case ItemType.Ammo: return "弾薬";
                case ItemType.Material: return "素材";
                case ItemType.QuestItem: return "クエストアイテム";
                case ItemType.Misc: return "その他";
                default: return type.ToString();
            }
        }

        private Texture2D MakeTexture(int width, int height, Color color)
        {
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }

            Texture2D texture = new Texture2D(width, height);
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private void CacheAllGUIDs()
        {
            int cachedCount = 0;
            foreach (ItemData item in allItems)
            {
                // ItemIDプロパティにアクセスすることで自動的にGUIDがキャッシュされる
                string guid = item.ItemID;
                if (!string.IsNullOrEmpty(guid))
                {
                    cachedCount++;
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"すべてのアイテムのGUIDをキャッシュしました ({cachedCount}件)");
            EditorUtility.DisplayDialog("GUID更新完了", $"{cachedCount}件のアイテムのGUIDをキャッシュしました。\nビルド版でも正しく動作します。", "OK");
        }
    }
}
