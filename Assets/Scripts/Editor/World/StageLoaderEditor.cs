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

            int missing = CountUnregistered(manifest, out List<string> samples);

            if (missing > 0)
            {
                EditorGUILayout.HelpBox(
                    $"タイルシーン {manifest.Tiles.Length} 枚のうち {missing} 枚が Build Settings に未登録です。\n" +
                    "このままでは実行時に1枚もロードされません。\n\n" +
                    $"例: {string.Join("\n     ", samples)}",
                    MessageType.Error);

                if (GUILayout.Button("Register Scenes In Build Settings", GUILayout.Height(28)))
                {
                    int added = StageSceneTools.Register(manifest);
                    Debug.Log($"[StageLoader] タイルシーンを Build Settings に登録しました（新規 {added} 件）。", manifest);
                }
            }
            else
            {
                EditorGUILayout.HelpBox(
                    $"タイルシーン {manifest.Tiles.Length} 枚すべてが Build Settings に登録済みです。",
                    MessageType.Info);
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
        /// Build Settings に載っていないタイルシーンの数を数える。
        /// </summary>
        // 無効化された(enabled=false)エントリはビルドに含まれないが、
        // エディタの Play では読めるので登録済みとして扱う。
        private static int CountUnregistered(StageTileManifest manifest, out List<string> samples)
        {
            HashSet<string> registered = new HashSet<string>();
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                registered.Add(scene.path);
            }

            samples = new List<string>();
            int missing = 0;

            foreach (StageTileEntry entry in manifest.Tiles)
            {
                if (registered.Contains(entry.scenePath))
                {
                    continue;
                }

                missing++;
                if (samples.Count < 3)
                {
                    samples.Add(entry.scenePath);
                }
            }

            return missing;
        }
    }
}
