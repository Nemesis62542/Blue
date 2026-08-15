using System;
using UnityEngine;

namespace Blue.World
{
    /// <summary>
    /// 造作の形状。
    /// </summary>
    public enum StageFeatureShape
    {
        /// <summary>滑らかに立ち上がるドーム。海丘・海嶺</summary>
        Dome,

        /// <summary>頂上が平らで縁が急な台地。上に構造物を置きやすい</summary>
        Plateau,

        /// <summary>尖った海山</summary>
        Cone,

        /// <summary>中心が凹み、縁が環状に盛り上がる。クレーター・ブルーホールの土台</summary>
        Crater,
    }

    /// <summary>
    /// 重なった造作の合成方法。
    /// </summary>
    // 折れ線は分岐を表せないので、分岐は造作を複数置いて繋ぐ形になる。
    // その際 Add のままだと接合部で高さが二重に足され、そこだけ盛り上がってしまう。
    public enum StageFeatureBlend
    {
        /// <summary>加算。独立した造作を重ねる既定の挙動</summary>
        Add,

        /// <summary>高い方を採用。分岐した尾根を繋ぐときに使う</summary>
        Max,

        /// <summary>低い方を採用。分岐した海溝を繋ぐときに使う</summary>
        Min,

        /// <summary>height の符号から Max / Min を自動で選ぶ。分岐を繋ぐならこれでよい</summary>
        // Max と Min は height の符号と対応していないと破綻するが、
        // 対応関係は機械的に決まるので、選ばせる必要がない。
        // 末尾に足してあるので、既存のアセットに保存された値はずれない。
        Merge,
    }

    /// <summary>
    /// 座標を指定して置く地形の造作。
    /// </summary>
    // ノイズは「全体をそれらしく荒らす」ことはできても、「ここに海丘を1つ」はできない。
    // 地図に描けるような特徴、プレイヤーが目印にする地形、Digger で彫る取っ掛かりは、
    // 全てここで明示的に置く。
    //
    // 座標はステージ中心を原点とした XZ(m)。シーン上の Terrain と同じ座標系なので、
    // シーンビューで見た位置をそのまま入力できる。
    [Serializable]
    public class StageFeature
    {
        [Tooltip("エディタ上の識別名。生成結果には影響しない")]
        public string name = "Feature";

        public bool enabled = true;

        public StageFeatureShape shape = StageFeatureShape.Dome;

        [Tooltip("重なった造作をどう合成するか。分岐を繋ぐなら Merge でよい。" +
                 "Add は加算で、独立した造作を重ねるとき用")]
        public StageFeatureBlend blend = StageFeatureBlend.Add;

        /// <summary>Merge を height の符号で解決した実際の合成モード</summary>
        public StageFeatureBlend ResolvedBlend =>
            blend != StageFeatureBlend.Merge
                ? blend
                : height < 0f
                    ? StageFeatureBlend.Min
                    : StageFeatureBlend.Max;

        [Tooltip("ステージ中心を原点とした XZ 座標(m)")]
        public Vector2 position;

        [Tooltip("影響半径(m)。伸長方向にはこれに elongation を掛けた長さになる。" +
                 "パスがある場合は折れ線からの片側の幅になる")]
        [Min(1f)]
        public float radius = 120f;

        [Tooltip("折れ線(m)。2点以上あると、楕円ではなく折れ線からの距離で形が決まる。" +
                 "うねった海嶺や蛇行する海溝はこれで作る")]
        public Vector2[] path = Array.Empty<Vector2>();

        [Tooltip("隆起量(m)。負で凹む")]
        public float height = 80f;

        [Tooltip("円からの伸び。1で円、大きいほど海嶺状に伸びる")]
        [Min(1f)]
        public float elongation = 1f;

        [Tooltip("伸びの向き(度)")]
        public float rotation;

        [Tooltip("表面の荒れ(m)。Digger で彫る取っ掛かりになる凹凸を作る")]
        public float roughness = 6f;

        [Tooltip("荒れの特徴サイズ(m)")]
        [Min(1f)]
        public float roughnessScale = 40f;

        /// <summary>
        /// 指定位置での形状値を返す。戻り値に height を掛けたものが隆起量(m)になる。
        /// </summary>
        /// <param name="influence">造作の効き具合 0-1。表面の荒れを中心ほど強くするのに使う</param>
        // 中心からの楕円距離を 0-1 に正規化してから形状関数に通す。
        // 範囲外は influence = 0 を返すので、呼び元はノイズの評価ごと省略できる。
        public float Shape(float worldX, float worldZ, out float influence)
        {
            float distance = UsesPath
                ? PathDistance(worldX, worldZ) / Mathf.Max(1f, radius)
                : EllipseDistance(worldX, worldZ);

            if (distance >= 1f)
            {
                influence = 0f;
                return 0f;
            }

            influence = Mathf.SmoothStep(1f, 0f, distance);

            switch (shape)
            {
                case StageFeatureShape.Plateau:
                    // 縁の3割だけで落とし切ることで、頂上を平らに保つ
                    return Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((1f - distance) / 0.3f));

                case StageFeatureShape.Cone:
                    return 1f - distance;

                case StageFeatureShape.Crater:
                    return CraterShape(distance);

                default:
                    return Mathf.SmoothStep(1f, 0f, distance);
            }
        }

        /// <summary>折れ線を使うか。2点未満なら楕円として扱う</summary>
        public bool UsesPath => path != null && path.Length >= 2;

        /// <summary>
        /// 造作が影響しうる矩形。生成ループの足切りに使う。
        /// </summary>
        // 折れ線の距離計算は線分数に比例するので、範囲外を先に弾かないと
        // ステージ全域でパスの全線分を舐めることになる。
        public Rect Bounds()
        {
            if (!UsesPath)
            {
                float extent = radius * Mathf.Max(1f, elongation);
                return new Rect(position.x - extent, position.y - extent, extent * 2f, extent * 2f);
            }

            Vector2 min = path[0];
            Vector2 max = path[0];

            foreach (Vector2 point in path)
            {
                min = Vector2.Min(min, point);
                max = Vector2.Max(max, point);
            }

            return new Rect(
                min.x - radius, min.y - radius,
                max.x - min.x + radius * 2f, max.y - min.y + radius * 2f);
        }

        /// <summary>中心からの楕円距離を 0-1 に正規化して返す</summary>
        private float EllipseDistance(float worldX, float worldZ)
        {
            float dx = worldX - position.x;
            float dz = worldZ - position.y;

            // 伸長方向をローカルX軸に合わせてから、その軸だけ縮めて円に戻す
            float radians = -rotation * Mathf.Deg2Rad;
            float cos = Mathf.Cos(radians);
            float sin = Mathf.Sin(radians);

            float localX = (dx * cos - dz * sin) / Mathf.Max(1f, elongation);
            float localZ = dx * sin + dz * cos;

            return Mathf.Sqrt(localX * localX + localZ * localZ) / Mathf.Max(1f, radius);
        }

        /// <summary>折れ線までの最短距離(m)</summary>
        private float PathDistance(float worldX, float worldZ)
        {
            float best = float.MaxValue;

            for (int i = 0; i < path.Length - 1; i++)
            {
                float distance = SegmentDistance(worldX, worldZ, path[i], path[i + 1]);
                if (distance < best)
                {
                    best = distance;
                }
            }

            return best;
        }

        private static float SegmentDistance(float x, float z, Vector2 a, Vector2 b)
        {
            Vector2 segment = b - a;
            float lengthSquared = segment.sqrMagnitude;

            // 潰れた線分は始点との距離で代用する
            float t = lengthSquared <= 1e-6f
                ? 0f
                : Mathf.Clamp01(Vector2.Dot(new Vector2(x - a.x, z - a.y), segment) / lengthSquared);

            Vector2 closest = a + segment * t;
            return Vector2.Distance(new Vector2(x, z), closest);
        }

        /// <summary>
        /// 中心が凹み、縁が盛り上がる形。
        /// </summary>
        // 中心の -1 から縁(距離0.75)の +0.6 まで持ち上げ、そこから外周へ落とす。
        // height を正で使うと「凹地の周りに環状の丘」になり、負で使うと丘の中央が窪む。
        private static float CraterShape(float distance)
        {
            const float RIM = 0.75f;

            if (distance < RIM)
            {
                return Mathf.Lerp(-1f, 0.6f, Mathf.SmoothStep(0f, 1f, distance / RIM));
            }

            return 0.6f * (1f - Mathf.SmoothStep(0f, 1f, (distance - RIM) / (1f - RIM)));
        }
    }
}
