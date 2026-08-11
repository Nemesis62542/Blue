using UnityEngine;

namespace Blue.World.Scatter
{
    /// <summary>
    /// タイルシーンに置かれ、そのタイルの散布データを保持する。
    /// </summary>
    // チャンクをタイルシーンから参照させることで、タイルのロード/アンロードに
    // 追随して散布データも載り降りする。マニフェスト側に全チャンクを持たせると
    // 未ロードのタイルぶんまで常駐してしまう。
    [DisallowMultipleComponent]
    public class StageTileScatter : MonoBehaviour
    {
        [SerializeField] private ScatterChunk chunk;

        public ScatterChunk Chunk => chunk;

        private void OnEnable()
        {
            ScatterRenderer.Register(this);
        }

        private void OnDisable()
        {
            ScatterRenderer.Unregister(this);
        }

        /// <summary>ベイカーから設定する。ランタイムからは呼ばない。</summary>
        public void Setup(ScatterChunk value)
        {
            chunk = value;
        }
    }
}
