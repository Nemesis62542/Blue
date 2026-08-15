using System;
using UnityEngine;

namespace Blue.World
{
    /// <summary>
    /// リージョン1つ分の地形の性格。
    /// </summary>
    // 「どこも同じノイズ」だと、泳いでいて場所の区別が付かない。ここを領域ごとに変えて、
    // 近づく前に別の場所だと分かる状態を作る。
    //
    // 水深そのものは断面プロファイルが決めるので、ここが持つのは断面からの差分だけ。
    // 領域で水深帯まで上書きすると、水深と生態系の対応が崩れて散布フィルタが意味を失う。
    [Serializable]
    public class StageRegionProfile
    {
        [Tooltip("エディタ上の識別名。ベイク結果には影響しない")]
        public string name = "Region";

        [Tooltip("プレビューでの表示色。ベイク結果には影響しない")]
        public Color previewColor = new Color(0.6f, 0.6f, 0.6f);

        [Header("Relief / 起伏")]
        [Tooltip("尾根・海丘の特徴サイズ(m)。小さいほど密に入り組む")]
        [Min(1f)]
        public float ridgeScale = 400f;

        [Tooltip("尾根による起伏の大きさ(m)")]
        public float ridgeHeight = 40f;

        [Tooltip("細かい起伏の特徴サイズ(m)")]
        [Min(1f)]
        public float detailScale = 60f;

        [Tooltip("細かい起伏の大きさ(m)")]
        public float detailHeight = 6f;

        [Header("Offset")]
        [Tooltip("この領域全体の水深オフセット(m)。正で深く、負で浅くなる。窪地や高台を作る")]
        public float depthBias;

        [Header("Erosion")]
        [Tooltip("この領域の安息角(度)。小さいほど崩れて緩やかになる")]
        [Range(10f, 60f)]
        public float talusAngle = 38f;
    }
}
