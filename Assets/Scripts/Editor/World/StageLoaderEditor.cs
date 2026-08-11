using System.Collections.Generic;
using Blue.World;
using UnityEditor;
using UnityEngine;

namespace Blue.Editor.World
{
    /// <summary>
    /// StageLoader のカスタムインスペクター。
    /// </summary>
    // タイルシーンが Build Settings に登録されていないと SceneManager.LoadSceneAsync は
    // null を返し、ロードが全て失敗する。それでも Play 自体は成立してしまい、
    // 「エラーに気づかないまま無効な計測をする」事故が起きるため、Play 前に検出する。
    [CustomEditor(typeof(StageLoader))]
    public class StageLoaderEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            StageLoader loader = (StageLoader)target;
            StageTileManifest manifest = loader.Manifest;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Preflight", EditorStyles.boldLabel);

            if (manifest == null)
            {
                EditorGUILayout.HelpBox("manifest が未設定です。ベイクで生成された StageTileManifest を割り当ててください。", MessageType.Error);
                return;
            }

            Inspect(manifest, out int missing, out int disabled, out List<string> samples);

            if (missing > 0)
            {
                EditorGUILayout.HelpBox(
                    $"タイルシーン {manifest.Tiles.Length} 枚のうち {missing} 枚が Build Settings に未登録です。\n" +
                    "このままでは実行時に1枚もロードされません。\n\n" +
                    $"例: {string.Join("\n     ", samples)}",
                    MessageType.Error);
            }
            else if (disabled > 0)
            {
                // 無効エントリはエディタの Play では読めてしまうため、ビルドして初めて失敗する
                EditorGUILayout.HelpBox(
                    $"タイルシーン {manifest.Tiles.Length} 枚のうち {disabled} 枚が Build Settings で無効(チェックが外れている)です。\n" +
                    "エディタの Play では読めますが、ビルドには含まれないため実行時にロードが失敗します。\n\n" +
                    $"例: {string.Join("\n     ", samples)}",
                    MessageType.Warning);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    $"タイルシーン {manifest.Tiles.Length} 枚すべてが Build Settings に登録され、有効になっています。",
                    MessageType.Info);
            }

            if (missing > 0 || disabled > 0)
            {
                if (GUILayout.Button("Register / Enable Scenes In Build Settings", GUILayout.Height(28)))
                {
                    int changed = StageSceneTools.Register(manifest);
                    Debug.Log($"[StageLoader] タイルシーンを Build Settings に登録・有効化しました（{changed} 件）。", manifest);
                }
            }

            if (Application.isPlaying)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Runtime", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(
                    $"ロード済み: {loader.LoadedTileCount} 枚 / 進行中: {loader.PendingLoadCount} 件\n" +
                    $"記録: {loader.Records.Count} 件",
                    MessageType.None);
                Repaint();
            }
        }

        /// <summary>
        /// タイルシーンの Build Settings 登録状況を調べる。
        /// </summary>
        // 未登録と「登録済みだが無効」を区別する。無効エントリはエディタの Play では
        // 読めてしまうため、区別しないとビルドして初めて失敗する。
        private static void Inspect(StageTileManifest manifest, out int missing, out int disabled,
                                    out List<string> samples)
        {
            Dictionary<string, bool> registered = new Dictionary<string, bool>();
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                registered[scene.path] = scene.enabled;
            }

            samples = new List<string>();
            missing = 0;
            disabled = 0;

            foreach (StageTileEntry entry in manifest.Tiles)
            {
                if (!registered.TryGetValue(entry.scenePath, out bool enabled))
                {
                    missing++;
                }
                else if (!enabled)
                {
                    disabled++;
                }
                else
                {
                    continue;
                }

                if (samples.Count < 3)
                {
                    samples.Add(entry.scenePath);
                }
            }
        }
    }
}
