using UnityEngine;
using UnityEditor;
using Blue.Save;
using Blue.Upgrade;
using Blue.UI.Garage.Customize;

namespace Blue.Editor
{
    public class UpgradeDebugWindow : EditorWindow
    {
        private int oxygenLevel;
        private int depthLevel;
        private int oxygenMaxLevel;
        private int depthMaxLevel;

        private GUIStyle headerStyle;
        private GUIStyle boxStyle;

        [MenuItem("Window/Blue/Upgrade Debug")]
        public static void ShowWindow()
        {
            UpgradeDebugWindow window = GetWindow<UpgradeDebugWindow>("Upgrade Debug");
            window.minSize = new Vector2(300, 200);
            window.Show();
        }

        private void OnEnable()
        {
            LoadUpgradeData();
            LoadCurrentLevels();
        }

        private void LoadUpgradeData()
        {
            string[] guids = AssetDatabase.FindAssets("t:UpgradeData");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                UpgradeData data = AssetDatabase.LoadAssetAtPath<UpgradeData>(path);
                if (data == null) continue;

                switch (data.UpgradeType)
                {
                    case UpgradeType.Oxygen:
                        oxygenMaxLevel = data.MaxLevel;
                        break;
                    case UpgradeType.Depth:
                        depthMaxLevel = data.MaxLevel;
                        break;
                }
            }
        }

        private void OnGUI()
        {
            InitializeStyles();

            EditorGUILayout.BeginVertical();

            DrawHeader();

            EditorGUILayout.Space(10);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("ゲーム実行中にのみ使用できます", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            DrawLevelControls();

            EditorGUILayout.Space(10);

            DrawActionButtons();

            EditorGUILayout.EndVertical();
        }

        private void InitializeStyles()
        {
            if (headerStyle == null)
            {
                headerStyle = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Bold,
                    fontSize = 14,
                    alignment = TextAnchor.MiddleCenter
                };
            }

            if (boxStyle == null)
            {
                boxStyle = new GUIStyle(GUI.skin.box)
                {
                    padding = new RectOffset(10, 10, 10, 10)
                };
            }
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("Upgrade Debug", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("更新", EditorStyles.toolbarButton, GUILayout.Width(50)))
            {
                LoadCurrentLevels();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawLevelControls()
        {
            EditorGUILayout.BeginVertical(boxStyle);

            EditorGUILayout.LabelField("アップグレードレベル設定", headerStyle);
            EditorGUILayout.Space(5);

            // 酸素レベル
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"酸素 Lv.{oxygenLevel + 1}:", GUILayout.Width(80));
            oxygenLevel = EditorGUILayout.IntSlider(oxygenLevel, 0, oxygenMaxLevel);
            if (GUILayout.Button("適用", GUILayout.Width(50)))
            {
                ApplyOxygenLevel();
            }
            EditorGUILayout.EndHorizontal();

            // 深度レベル
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"深度 Lv.{depthLevel + 1}:", GUILayout.Width(80));
            depthLevel = EditorGUILayout.IntSlider(depthLevel, 0, depthMaxLevel);
            if (GUILayout.Button("適用", GUILayout.Width(50)))
            {
                ApplyDepthLevel();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawActionButtons()
        {
            EditorGUILayout.BeginVertical(boxStyle);

            if (GUILayout.Button("レベルを適用", GUILayout.Height(30)))
            {
                ApplyLevels();
            }

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("全てリセット (Lv.1)"))
            {
                oxygenLevel = 0;
                depthLevel = 0;
                ApplyLevels();
            }

            if (GUILayout.Button("全て最大"))
            {
                oxygenLevel = oxygenMaxLevel;
                depthLevel = depthMaxLevel;
                ApplyLevels();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void LoadCurrentLevels()
        {
            if (!Application.isPlaying) return;

            SaveData saveData = SaveManager.CurrentSaveData;
            if (saveData?.upgrades != null)
            {
                oxygenLevel = saveData.upgrades.oxygenLevel;
                depthLevel = saveData.upgrades.depthLevel;
            }
        }

        private void ApplyOxygenLevel()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[UpgradeDebug] ゲーム実行中にのみ使用できます");
                return;
            }

            SaveData saveData = SaveManager.CurrentSaveData;
            if (saveData?.upgrades == null)
            {
                Debug.LogError("[UpgradeDebug] SaveDataが見つかりません");
                return;
            }

            int oldLevel = saveData.upgrades.oxygenLevel;
            saveData.upgrades.oxygenLevel = oxygenLevel;
            SaveManager.Save();

            Debug.Log($"[UpgradeDebug] 酸素レベルを変更しました: Lv.{oldLevel + 1} → Lv.{oxygenLevel + 1}");
            RefreshUI();
        }

        private void ApplyDepthLevel()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[UpgradeDebug] ゲーム実行中にのみ使用できます");
                return;
            }

            SaveData saveData = SaveManager.CurrentSaveData;
            if (saveData?.upgrades == null)
            {
                Debug.LogError("[UpgradeDebug] SaveDataが見つかりません");
                return;
            }

            int oldLevel = saveData.upgrades.depthLevel;
            saveData.upgrades.depthLevel = depthLevel;
            SaveManager.Save();

            Debug.Log($"[UpgradeDebug] 深度レベルを変更しました: Lv.{oldLevel + 1} → Lv.{depthLevel + 1}");
            RefreshUI();
        }

        private void ApplyLevels()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[UpgradeDebug] ゲーム実行中にのみ使用できます");
                return;
            }

            SaveData saveData = SaveManager.CurrentSaveData;
            if (saveData?.upgrades == null)
            {
                Debug.LogError("[UpgradeDebug] SaveDataが見つかりません");
                return;
            }

            int oldOxygen = saveData.upgrades.oxygenLevel;
            int oldDepth = saveData.upgrades.depthLevel;

            saveData.upgrades.oxygenLevel = oxygenLevel;
            saveData.upgrades.depthLevel = depthLevel;

            SaveManager.Save();

            Debug.Log($"[UpgradeDebug] レベルを変更しました - 酸素: Lv.{oldOxygen + 1} → Lv.{oxygenLevel + 1}, 深度: Lv.{oldDepth + 1} → Lv.{depthLevel + 1}");

            RefreshUI();
        }

        private void RefreshUI()
        {
            CustomizeScreenView customizeView = FindObjectOfType<CustomizeScreenView>();
            if (customizeView != null)
            {
                customizeView.RefreshDisplay();
                Debug.Log("[UpgradeDebug] UIを更新しました");
            }
        }
    }
}
