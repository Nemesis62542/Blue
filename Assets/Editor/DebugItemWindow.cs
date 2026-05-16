using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Blue.Editor
{
    /// <summary>
    /// デバッグ用アイテム付与ウィンドウ
    /// 既存クラスへの直接参照を避け、Reflectionを使用して実装
    /// フィールドシーン（PlayerController）とGarageシーン（StrageController）の両方に対応
    /// </summary>
    public class DebugItemWindow : EditorWindow
    {
        private enum InventoryTarget
        {
            Player,
            Storage
        }

        private List<ScriptableObject> cachedItems = new List<ScriptableObject>();
        private Vector2 scrollPosition;
        private string searchFilter = "";
        private Dictionary<ScriptableObject, int> itemQuantities = new Dictionary<ScriptableObject, int>();
        private InventoryTarget currentTarget = InventoryTarget.Player;
        private string currentSceneType = "不明";

        [MenuItem("Blue/Debug/Item Window")]
        public static void ShowWindow()
        {
            DebugItemWindow window = GetWindow<DebugItemWindow>("Debug Item");
            window.minSize = new Vector2(400, 300);
        }

        private void OnEnable()
        {
            RefreshItemList();
        }

        private void OnFocus()
        {
            RefreshItemList();
        }

        private void RefreshItemList()
        {
            cachedItems.Clear();
            itemQuantities.Clear();

            // AssetDatabaseから全てのItemDataを検索（型名で検索）
            string[] guids = AssetDatabase.FindAssets("t:ItemData");

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ScriptableObject item = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (item != null)
                {
                    cachedItems.Add(item);
                    itemQuantities[item] = 1;
                }
            }

            // 名前順にソート
            cachedItems = cachedItems.OrderBy(item => item.name).ToList();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(5);

            // ヘッダー
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60)))
                {
                    RefreshItemList();
                }

                GUILayout.FlexibleSpace();

                EditorGUILayout.LabelField($"Items: {cachedItems.Count}", GUILayout.Width(80));
            }

            // 検索フィルター
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Search:", GUILayout.Width(50));
                searchFilter = EditorGUILayout.TextField(searchFilter);
                if (GUILayout.Button("Clear", GUILayout.Width(50)))
                {
                    searchFilter = "";
                    GUI.FocusControl(null);
                }
            }

            EditorGUILayout.Space(5);

            // プレイモード確認
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("プレイモードでのみアイテムを付与できます", MessageType.Info);
            }
            else
            {
                // シーンタイプを判定して表示
                UpdateSceneType();

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField($"シーン: {currentSceneType}", GUILayout.Width(150));

                    // Garageシーンの場合はターゲット選択を表示
                    if (currentSceneType == "Garage")
                    {
                        EditorGUILayout.LabelField("付与先:", GUILayout.Width(50));
                        currentTarget = (InventoryTarget)EditorGUILayout.EnumPopup(currentTarget, GUILayout.Width(100));
                    }
                }
            }

            // アイテムリスト
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            foreach (ScriptableObject item in cachedItems)
            {
                if (item == null) continue;

                // フィルター適用
                if (!string.IsNullOrEmpty(searchFilter))
                {
                    if (!item.name.ToLower().Contains(searchFilter.ToLower()))
                        continue;
                }

                DrawItemRow(item);
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(5);

            // 一括付与ボタン
            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.enabled = Application.isPlaying;

                if (GUILayout.Button("全アイテムを1個ずつ付与", GUILayout.Height(30)))
                {
                    AddAllItems(1);
                }

                if (GUILayout.Button("全アイテムを99個ずつ付与", GUILayout.Height(30)))
                {
                    AddAllItems(99);
                }

                GUI.enabled = true;
            }

            // クリアボタン
            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.enabled = Application.isPlaying;

                if (GUILayout.Button("アイテムをクリア", GUILayout.Height(30)))
                {
                    if (EditorUtility.DisplayDialog("確認", "インベントリの全アイテムを削除しますか？", "削除", "キャンセル"))
                    {
                        ClearInventory();
                    }
                }

                GUI.enabled = true;
            }
        }

        private void DrawItemRow(ScriptableObject item)
        {
            using (new EditorGUILayout.HorizontalScope("box"))
            {
                // アイコン表示
                Sprite iconProperty = GetPropertyValue<Sprite>(item, "Icon");
                if (iconProperty != null)
                {
                    Texture2D texture = AssetPreview.GetAssetPreview(iconProperty);
                    if (texture != null)
                    {
                        GUILayout.Label(texture, GUILayout.Width(32), GUILayout.Height(32));
                    }
                    else
                    {
                        GUILayout.Label("", GUILayout.Width(32), GUILayout.Height(32));
                    }
                }
                else
                {
                    GUILayout.Label("", GUILayout.Width(32), GUILayout.Height(32));
                }

                // アイテム名
                EditorGUILayout.LabelField(item.name, GUILayout.MinWidth(100));

                // タイプ表示
                object typeValue = GetPropertyValue<object>(item, "Type");
                if (typeValue != null)
                {
                    EditorGUILayout.LabelField(typeValue.ToString(), GUILayout.Width(80));
                }

                // 数量入力
                if (!itemQuantities.ContainsKey(item))
                    itemQuantities[item] = 1;

                itemQuantities[item] = EditorGUILayout.IntField(itemQuantities[item], GUILayout.Width(50));
                itemQuantities[item] = Mathf.Clamp(itemQuantities[item], 1, 9999);

                // 付与ボタン
                GUI.enabled = Application.isPlaying;
                if (GUILayout.Button("Add", GUILayout.Width(50)))
                {
                    AddItemToInventory(item, itemQuantities[item]);
                }
                GUI.enabled = true;
            }
        }

        private void AddItemToInventory(ScriptableObject itemData, int quantity)
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[DebugItemWindow] プレイモードでのみ使用可能です");
                return;
            }

            object inventory = FindTargetInventory();
            if (inventory == null)
            {
                Debug.LogError("[DebugItemWindow] インベントリが見つかりません");
                return;
            }

            // AddItemメソッドを呼び出し
            MethodInfo addItemMethod = inventory.GetType().GetMethod("AddItem",
                BindingFlags.Public | BindingFlags.Instance,
                null,
                new Type[] { itemData.GetType(), typeof(int) },
                null);

            if (addItemMethod == null)
            {
                Debug.LogError("[DebugItemWindow] AddItemメソッドが見つかりません");
                return;
            }

            addItemMethod.Invoke(inventory, new object[] { itemData, quantity });

            string itemName = GetPropertyValue<string>(itemData, "Name") ?? itemData.name;
            string targetName = currentTarget == InventoryTarget.Player ? "プレイヤー" : "倉庫";
            Debug.Log($"[DebugItemWindow] {itemName} x{quantity} を{targetName}に付与しました");
        }

        private void AddAllItems(int quantity)
        {
            foreach (ScriptableObject item in cachedItems)
            {
                if (item != null)
                {
                    AddItemToInventory(item, quantity);
                }
            }
        }

        private void ClearInventory()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[DebugItemWindow] プレイモードでのみ使用可能です");
                return;
            }

            object inventory = FindTargetInventory();
            if (inventory == null)
            {
                Debug.LogError("[DebugItemWindow] インベントリが見つかりません");
                return;
            }

            // inventoryItemsフィールドを取得してクリア
            FieldInfo itemsField = inventory.GetType().GetField("inventoryItems",
                BindingFlags.NonPublic | BindingFlags.Instance);

            if (itemsField != null)
            {
                object itemsList = itemsField.GetValue(inventory);
                if (itemsList != null)
                {
                    MethodInfo clearMethod = itemsList.GetType().GetMethod("Clear");
                    if (clearMethod != null)
                    {
                        clearMethod.Invoke(itemsList, null);

                        // OnValueChangedイベントを発火（eventのバッキングフィールドを取得）
                        FieldInfo eventField = inventory.GetType().GetField("OnValueChanged",
                            BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
                        if (eventField != null)
                        {
                            Action onValueChanged = eventField.GetValue(inventory) as Action;
                            onValueChanged?.Invoke();
                        }

                        string targetName = currentTarget == InventoryTarget.Player ? "プレイヤー" : "倉庫";
                        Debug.Log($"[DebugItemWindow] {targetName}のインベントリをクリアしました");
                    }
                }
            }
            else
            {
                Debug.LogError("[DebugItemWindow] inventoryItemsフィールドが見つかりません");
            }
        }

        private void UpdateSceneType()
        {
            if (FindControllerByName("PlayerController") != null)
            {
                currentSceneType = "Field";
            }
            else if (FindControllerByName("StrageController") != null)
            {
                currentSceneType = "Garage";
            }
            else
            {
                currentSceneType = "不明";
            }
        }

        private object FindTargetInventory()
        {
            // まずPlayerControllerを探す（フィールドシーン）
            MonoBehaviour playerController = FindControllerByName("PlayerController");
            if (playerController != null)
            {
                return GetPropertyValue<object>(playerController, "Inventory");
            }

            // 次にStrageControllerを探す（Garageシーン）
            MonoBehaviour strageController = FindControllerByName("StrageController");
            if (strageController != null)
            {
                // ターゲットに応じてインベントリを取得
                string fieldName = currentTarget == InventoryTarget.Player
                    ? "playerInventoryModel"
                    : "strageInventoryModel";

                return GetPropertyValue<object>(strageController, fieldName);
            }

            return null;
        }

        private MonoBehaviour FindControllerByName(string controllerName)
        {
            // 全てのMonoBehaviourから指定の名前のコントローラーを探す
            MonoBehaviour[] allBehaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);

            foreach (MonoBehaviour behaviour in allBehaviours)
            {
                if (behaviour.GetType().Name == controllerName)
                {
                    return behaviour;
                }
            }

            return null;
        }

        private T GetPropertyValue<T>(object obj, string propertyName)
        {
            if (obj == null) return default;

            Type type = obj.GetType();

            // プロパティを検索
            PropertyInfo property = type.GetProperty(propertyName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (property != null)
            {
                object value = property.GetValue(obj);
                if (value is T typedValue)
                    return typedValue;
            }

            // フィールドを検索
            FieldInfo field = type.GetField(propertyName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null)
            {
                object value = field.GetValue(obj);
                if (value is T typedValue)
                    return typedValue;
            }

            return default;
        }
    }
}
