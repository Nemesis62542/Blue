using System.Collections.Generic;
using Blue.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Blue.Editor.World
{
    /// <summary>
    /// タイルシーンの Build Settings 登録と、確認用の一括オープン。
    ///
    /// SceneManager.LoadSceneAsync はパス指定でも Build Settings に載っているシーンしか
    /// 読めないため、ベイク後の登録が必須になる。
    /// （将来 Addressables に移す場合、置き換わるのはここと StageLoader の読み込み部分だけ）
    /// </summary>
    public static class StageSceneTools
    {
        #region Build Settings

        [MenuItem("Blue/World/Register Stage Scenes In Build Settings")]
        public static void RegisterSelected()
        {
            StageTileManifest manifest = Selection.activeObject as StageTileManifest;
            if (manifest == null)
            {
                EditorUtility.DisplayDialog("Stage Scene Tools",
                    "StageTileManifest アセットを選択してから実行してください。", "OK");
                return;
            }

            int added = Register(manifest);
            Debug.Log($"[StageSceneTools] '{manifest.StageId}' のタイルシーンを Build Settings に登録しました（新規 {added} 件 / 全 {manifest.Tiles.Length} 件）。", manifest);
        }

        [MenuItem("Blue/World/Register Stage Scenes In Build Settings", true)]
        private static bool RegisterSelectedValidate() => Selection.activeObject is StageTileManifest;

        /// <summary>
        /// マニフェストのタイルシーンを Build Settings に登録する。既存の登録は保持する。
        /// </summary>
        public static int Register(StageTileManifest manifest)
        {
            if (manifest == null || manifest.Tiles.Length == 0)
            {
                return 0;
            }

            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            HashSet<string> existing = new HashSet<string>();
            foreach (EditorBuildSettingsScene scene in scenes)
            {
                existing.Add(scene.path);
            }

            int added = 0;
            foreach (StageTileEntry entry in manifest.Tiles)
            {
                if (existing.Contains(entry.scenePath))
                {
                    continue;
                }

                scenes.Add(new EditorBuildSettingsScene(entry.scenePath, true));
                existing.Add(entry.scenePath);
                added++;
            }

            if (added > 0)
            {
                EditorBuildSettings.scenes = scenes.ToArray();
            }

            return added;
        }

        /// <summary>
        /// マニフェストのタイルシーンを Build Settings から外す。
        /// </summary>
        public static int Unregister(StageTileManifest manifest)
        {
            if (manifest == null || manifest.Tiles.Length == 0)
            {
                return 0;
            }

            HashSet<string> targets = new HashSet<string>();
            foreach (StageTileEntry entry in manifest.Tiles)
            {
                targets.Add(entry.scenePath);
            }

            List<EditorBuildSettingsScene> kept = new List<EditorBuildSettingsScene>();
            int removed = 0;
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (targets.Contains(scene.path))
                {
                    removed++;
                    continue;
                }

                kept.Add(scene);
            }

            if (removed > 0)
            {
                EditorBuildSettings.scenes = kept.ToArray();
            }

            return removed;
        }

        #endregion

        #region Inspection

        /// <summary>
        /// 全タイルを加算で開く。ベイクした地形の全体像を目視確認するためのもの。
        /// 64枚の Terrain が同時に載るので、確認が終わったら閉じること。
        /// </summary>
        [MenuItem("Blue/World/Open All Stage Tiles")]
        public static void OpenAllSelected()
        {
            StageTileManifest manifest = Selection.activeObject as StageTileManifest;
            if (manifest == null)
            {
                EditorUtility.DisplayDialog("Stage Scene Tools",
                    "StageTileManifest アセットを選択してから実行してください。", "OK");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            if (!EditorUtility.DisplayDialog("Open All Stage Tiles",
                    $"'{manifest.StageId}' の {manifest.Tiles.Length} 枚のタイルシーンを加算で開きます。\n" +
                    "全 Terrain が同時に載るため、確認用途にのみ使ってください。",
                    "開く", "キャンセル"))
            {
                return;
            }

            try
            {
                for (int i = 0; i < manifest.Tiles.Length; i++)
                {
                    StageTileEntry entry = manifest.Tiles[i];
                    EditorUtility.DisplayProgressBar("Open All Stage Tiles",
                        $"{i + 1}/{manifest.Tiles.Length}: {entry.scenePath}",
                        (float)i / manifest.Tiles.Length);

                    if (SceneManager.GetSceneByPath(entry.scenePath).isLoaded)
                    {
                        continue;
                    }

                    EditorSceneManager.OpenScene(entry.scenePath, OpenSceneMode.Additive);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            Debug.Log($"[StageSceneTools] '{manifest.StageId}' の全タイルを開きました（{manifest.Tiles.Length} 枚）。", manifest);
        }

        [MenuItem("Blue/World/Open All Stage Tiles", true)]
        private static bool OpenAllSelectedValidate() => Selection.activeObject is StageTileManifest;

        /// <summary>
        /// 開いているタイルシーンを閉じる。
        /// </summary>
        [MenuItem("Blue/World/Close All Stage Tiles")]
        public static void CloseAllSelected()
        {
            StageTileManifest manifest = Selection.activeObject as StageTileManifest;
            if (manifest == null)
            {
                EditorUtility.DisplayDialog("Stage Scene Tools",
                    "StageTileManifest アセットを選択してから実行してください。", "OK");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            int closed = 0;
            foreach (StageTileEntry entry in manifest.Tiles)
            {
                Scene scene = SceneManager.GetSceneByPath(entry.scenePath);
                if (!scene.isLoaded)
                {
                    continue;
                }

                // 最後の1枚は閉じられないので残す
                if (SceneManager.loadedSceneCount <= 1)
                {
                    break;
                }

                EditorSceneManager.CloseScene(scene, true);
                closed++;
            }

            Debug.Log($"[StageSceneTools] タイルシーンを {closed} 枚閉じました。", manifest);
        }

        [MenuItem("Blue/World/Close All Stage Tiles", true)]
        private static bool CloseAllSelectedValidate() => Selection.activeObject is StageTileManifest;

        #endregion
    }
}
