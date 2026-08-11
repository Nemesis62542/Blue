using System;
using UnityEngine;

namespace Blue.World.Scatter
{
    /// <summary>
    /// タイル1枚分の散布データ。散布ベイクの出力物。
    /// </summary>
    // 【このクラスを単独ファイルに置く理由】
    // Unity は ScriptableObject / MonoBehaviour のクラス名とファイル名が一致していないと
    // MonoScript を生成できず、アセット側がスクリプト参照を解決できなくなる。
    // 他の型と同じファイルに入れていたときは、生成した全チャンクが
    // "The referenced script on this Behaviour (Game Object '<null>') is missing!" になった。
    //
    // 【所有権のルール】
    // インスタンスは基点(position)が属するタイルが所有する。描画も所有タイルが丸ごと行うため、
    // 隣タイルが未ロードでも「大きな岩が半分だけ消える」ことは起きない
    // （オブジェクト単位で出るか出ないかになる）。
    // タイルサイズを超える巨大物のみ overlappingTiles に隣タイルを登録して例外扱いする。
    //
    // サンゴ・海藻・岩・落ちているアイテムはすべてこの形式に乗る。
    // 種類が増えてもベイカー側・ランタイム側の変更は不要。
    public class ScatterChunk : ScriptableObject
    {
        [SerializeField] private int tileIndex;
        [SerializeField] private ScatterGroup[] groups = Array.Empty<ScatterGroup>();
        [SerializeField] private Bounds bounds;

        [Tooltip("このタイルの散布物がはみ出している隣接タイルのインデックス（巨大物のみ）")]
        [SerializeField] private int[] overlappingTiles = Array.Empty<int>();

        public int TileIndex => tileIndex;
        public ScatterGroup[] Groups => groups;
        public Bounds Bounds => bounds;
        public int[] OverlappingTiles => overlappingTiles;

        /// <summary>ベイカーから内容を設定する。ランタイムからは呼ばない。</summary>
        public void SetContents(int tileIndex, ScatterGroup[] groups, Bounds bounds, int[] overlappingTiles)
        {
            this.tileIndex = tileIndex;
            this.groups = groups ?? Array.Empty<ScatterGroup>();
            this.bounds = bounds;
            this.overlappingTiles = overlappingTiles ?? Array.Empty<int>();
        }
    }
}
