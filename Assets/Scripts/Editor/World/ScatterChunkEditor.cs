using System.Text;
using Blue.World.Scatter;
using UnityEditor;
using UnityEngine;

namespace Blue.Editor.World
{
    /// <summary>
    /// ScatterChunk のカスタムインスペクター。
    /// </summary>
    // 既定のインスペクターだと数万要素の配列がそのまま展開されて実用にならない。
    // 配置数の要約だけを出す。
    [CustomEditor(typeof(ScatterChunk))]
    public class ScatterChunkEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            ScatterChunk chunk = (ScatterChunk)target;

            EditorGUILayout.LabelField("Summary", EditorStyles.boldLabel);

            int total = 0;
            StringBuilder builder = new StringBuilder();

            foreach (ScatterGroup group in chunk.Groups)
            {
                int count = group.instances != null ? group.instances.Length : 0;
                total += count;
                builder.AppendLine(
                    $"prototypeId {group.prototypeId}: {count} 個体" +
                    (group.instantiate ? "（GameObject 実体化）" : "（インスタンシング描画）"));
            }

            if (total == 0)
            {
                EditorGUILayout.HelpBox(
                    "このタイルには1個体も配置されていません。\n" +
                    "傾斜・水深・マスクのフィルタで全て弾かれている可能性があります。",
                    MessageType.Warning);
            }

            EditorGUILayout.HelpBox(
                $"タイル {chunk.TileIndex}\n" +
                $"合計 {total} 個体 / {chunk.Groups.Length} グループ\n\n" +
                (builder.Length > 0 ? builder.ToString().TrimEnd() : "(グループなし)"),
                MessageType.None);

            EditorGUILayout.LabelField("Bounds", chunk.Bounds.ToString());

            if (chunk.OverlappingTiles.Length > 0)
            {
                EditorGUILayout.LabelField("はみ出し先タイル", string.Join(", ", chunk.OverlappingTiles));
            }

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "これは散布ベイクの生成物です。手で編集しても再ベイクで上書きされます。",
                MessageType.None);
        }
    }
}
