using Blue.World.Scatter;
using UnityEditor;
using UnityEngine;

namespace Blue.Editor.World
{
    /// <summary>
    /// ScatterRenderer のカスタムインスペクター。
    /// </summary>
    // renderDistance はフォグで見えなくなる距離に合わせて決めるものなので、
    // 実際に描画されているタイル数とインスタンス数を見ながら詰められるようにする。
    [CustomEditor(typeof(ScatterRenderer))]
    public class ScatterRendererEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            ScatterRenderer renderer = (ScatterRenderer)target;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Live", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                $"描画中のタイル: {renderer.DrawnTileCount} 枚\n" +
                $"描画中のインスタンス: {renderer.DrawnInstanceCount} 個体\n" +
                $"行列キャッシュ保持: {renderer.CachedTileCount} 枚",
                MessageType.None);

            EditorGUILayout.HelpBox(
                "renderDistance はフォグで見えなくなる距離より少し広い程度に設定します。" +
                "広すぎると見えない散布物を描画し続け、狭すぎるとポップインが見えます。",
                MessageType.None);

            Repaint();
        }
    }
}
