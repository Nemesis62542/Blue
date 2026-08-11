using System;
using UnityEngine;

namespace Blue.World.Scatter
{
    /// <summary>
    /// 散布物1種類の配置ルール。
    /// </summary>
    // サンゴ・海藻・岩・落ちているアイテムのいずれもこの1つの型で表す。
    // 層を重ねてリーフを作る想定（被覆 → 塊状 → 枝状 → 扇状）なので、
    // 専用の「サンゴ群生ジェネレータ」のような仕組みは作らない。
    [Serializable]
    public class ScatterLayer
    {
        [Tooltip("エディタ上の識別名。ベイク結果には影響しない")]
        public string name = "Scatter";

        [Tooltip("ScatterPrototypeRegistry 上のプロトタイプID")]
        public int prototypeId;

        [Header("Density")]
        [Tooltip("候補点の間隔(m)。実際の配置はここから間引かれるので、これは上限密度になる")]
        [Min(0.1f)]
        public float spacing = 4f;

        [Tooltip("候補点を採用する確率 0-1。間隔を保ったまま密度だけ落としたいときに使う")]
        [Range(0f, 1f)]
        public float density = 1f;

        [Tooltip("候補点をセル内でどれだけ揺らすか 0-1。0だと格子状に並んで不自然になる")]
        [Range(0f, 1f)]
        public float jitter = 0.9f;

        [Header("Surfaces")]
        [Tooltip("1つの候補点の真下で、上から順にいくつの面を調べるか。\n" +
                 "1 = 最初に当たった面（＝地表）だけ。開けた地形はこれで足りる。\n" +
                 "閉じた洞窟の中に置きたい場合は 4 を推奨。\n" +
                 "下向きレイは「地表の上面 → 洞窟の天井 → 洞窟の床」の順に当たるため、\n" +
                 "床に届かせるには天井のぶんも数える必要がある")]
        [Range(1, 8)]
        public int maxSurfacesPerColumn = 1;

        [Header("Filter")]
        [Tooltip("配置を許可する面の傾斜(度)。\n" +
                 "0-30 は平らな床。90 付近は壁。150-180 は天井（洞窟の上面にぶら下がる種）")]
        public Vector2 slopeRange = new Vector2(0f, 30f);

        [Tooltip("配置を許可する水深(m)。水面を0として下向きが正")]
        public Vector2 depthRange = new Vector2(0f, 300f);

        [Tooltip("バイオームマスク。null なら全域が対象")]
        public Texture2D mask;

        public MaskChannel maskChannel = MaskChannel.R;

        [Tooltip("マスク値がこれ未満の場所には置かない")]
        [Range(0f, 1f)]
        public float maskThreshold = 0.5f;

        [Tooltip("マスク値を採用確率に掛ける。境界をぼかしたいときに使う")]
        public bool maskAffectsDensity = true;

        [Header("Placement")]
        public Vector2 scaleRange = new Vector2(0.8f, 1.2f);

        [Tooltip("地形の法線に沿って傾ける。岩や被覆状のものに使う。false なら常に直立")]
        public bool alignToNormal;

        [Tooltip("alignToNormal のときに法線へどれだけ寄せるか 0-1")]
        [Range(0f, 1f)]
        public float normalAlignment = 1f;

        [Tooltip("Y軸まわりにランダム回転させる")]
        public bool randomYaw = true;

        [Tooltip("配置点を法線方向にずらす(m)。めり込みや浮きの調整に使う")]
        public float surfaceOffset;

        [Header("Runtime")]
        [Tooltip("GameObject として実体化する。アイテムなど拾える物に使う")]
        public bool instantiate;

        /// <summary>このレイヤーが有効な設定になっているか</summary>
        public bool IsValid => spacing > 0f && density > 0f;
    }
}
