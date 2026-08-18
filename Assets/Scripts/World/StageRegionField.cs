using UnityEngine;

namespace Blue.World
{
    /// <summary>
    /// ステージをリージョン（セル）に分割し、任意の位置がどのリージョンに属するかを返す。
    /// </summary>
    // ジッタを掛けた格子点による Voronoi 分割。境界を歪ませるため、距離を測る前に
    // 座標そのものをノイズでずらす。これをしないとセル境界が直線になり、
    // 上から見たときに人工的な多角形の継ぎ目として見えてしまう。
    //
    // 隣接セルとの距離差から重みを返すので、呼び元は境界をまたぐ場所で
    // パラメータを補間できる。ここで補間しておかないと、領域の切り替わりが
    // 崖のような段差になる。
    //
    // シードとパラメータが同じなら結果は完全に決定的。ベイクとプレビューで
    // 同じ分割になることを保証している。
    public sealed class StageRegionField
    {
        #region Fields

        private readonly Vector2[] sites;
        private readonly int cellsPerAxis;
        private readonly float cellSize;
        private readonly float blendWidth;
        private readonly float warpAmount;
        private readonly float warpFrequency;
        private readonly float warpOffsetX;
        private readonly float warpOffsetZ;
        private readonly int searchRings;

        #endregion

        #region Properties

        public int CellsPerAxis => cellsPerAxis;

        public int CellCount => cellsPerAxis * cellsPerAxis;

        /// <summary>SampleDistances が返しうる最大要素数。呼び元のバッファ長になる</summary>
        public int MaxNeighbours => (searchRings * 2 + 1) * (searchRings * 2 + 1);

        #endregion

        #region Constructor

        /// <param name="worldSize">ステージ一辺の長さ(m)</param>
        /// <param name="jitter">セル中心の揺らし量 0-1。0で正方格子</param>
        /// <param name="blendWidth">境界のブレンド幅(m)</param>
        /// <param name="warpAmount">境界を歪ませる量(m)</param>
        /// <param name="maxBlendWidth">
        /// 呼び元が ToWeights に渡しうる最大の幅(m)。近傍の探索範囲をここから決める
        /// </param>
        public StageRegionField(
            int cellsPerAxis, float worldSize, float jitter, float blendWidth, float warpAmount, int seed,
            float maxBlendWidth)
        {
            this.cellsPerAxis = Mathf.Max(1, cellsPerAxis);
            this.blendWidth = Mathf.Max(0f, blendWidth);
            this.warpAmount = Mathf.Max(0f, warpAmount);

            cellSize = worldSize / this.cellsPerAxis;

            // ToWeights は最近傍から maxBlendWidth 以内のセル全てに重みを与える。
            // 探索を 3x3 に固定すると、幅がセル1つ分を超えたときに範囲外のセルが
            // 重みを持つべき場所で無視され、格子に沿った段差が出る。
            // 最近傍までの距離とジッタの分を +2 リングの余裕として見込む。
            searchRings = Mathf.CeilToInt(Mathf.Max(0f, maxBlendWidth) / cellSize) + 2;

            // 歪みの波長はセル1つ分程度にする。これより細かいと境界がギザギザになり、
            // これより粗いと分割全体が平行移動するだけで形が変わらない
            warpFrequency = 1f / Mathf.Max(1f, cellSize * 0.8f);

            System.Random random = new System.Random(seed);
            warpOffsetX = (float)random.NextDouble() * 500f + 17.3f;
            warpOffsetZ = (float)random.NextDouble() * 500f + 63.1f;

            sites = new Vector2[CellCount];
            float clampedJitter = Mathf.Clamp01(jitter) * 0.5f;

            for (int cellZ = 0; cellZ < this.cellsPerAxis; cellZ++)
            {
                for (int cellX = 0; cellX < this.cellsPerAxis; cellX++)
                {
                    float offsetX = ((float)random.NextDouble() - 0.5f) * 2f * clampedJitter;
                    float offsetZ = ((float)random.NextDouble() - 0.5f) * 2f * clampedJitter;

                    sites[cellZ * this.cellsPerAxis + cellX] = new Vector2(
                        (cellX + 0.5f + offsetX) * cellSize,
                        (cellZ + 0.5f + offsetZ) * cellSize);
                }
            }
        }

        #endregion

        #region Sampling

        /// <summary>
        /// 指定位置の所属リージョンと、隣接リージョンへのブレンド重みを求める。
        /// </summary>
        /// <param name="primaryWeight">主リージョンの重み。境界で0.5、内部で1に近づく</param>
        public void Sample(float x, float z, out int primaryCell, out int secondaryCell, out float primaryWeight)
        {
            Sample(x, z, out primaryCell, out secondaryCell, out primaryWeight, out _);
        }

        /// <summary>
        /// 指定位置の所属リージョンと、隣接リージョンへのブレンド重みを求める。
        /// </summary>
        /// <param name="primaryWeight">主リージョンの重み。境界で0.5、内部で1に近づく</param>
        /// <param name="edgeDistance">
        /// 最近傍と次点の距離差(m)。境界で0。呼び元が別のブレンド幅で重みを引き直すのに使う
        /// </param>
        // 高さのオフセットは起伏の振幅差より段差として目立つため、同じ幅でぼかすと
        // 境界が線として見える。重みの計算式を呼び元に開いて、項目ごとに幅を変えられるようにする。
        public void Sample(
            float x, float z,
            out int primaryCell, out int secondaryCell, out float primaryWeight, out float edgeDistance)
        {
            Warp(x, z, out float warpedX, out float warpedZ);

            int centerX = Mathf.Clamp(Mathf.FloorToInt(warpedX / cellSize), 0, cellsPerAxis - 1);
            int centerZ = Mathf.Clamp(Mathf.FloorToInt(warpedZ / cellSize), 0, cellsPerAxis - 1);

            float nearest = float.MaxValue;
            float secondNearest = float.MaxValue;
            int nearestIndex = centerZ * cellsPerAxis + centerX;
            int secondIndex = -1;

            // 探索範囲はブレンド幅から決めている。最近傍だけなら 3x3 で足りるが、
            // 重みを持ちうるセルまで含めないと ToWeights の結果が格子に沿って飛ぶ
            for (int offsetZ = -searchRings; offsetZ <= searchRings; offsetZ++)
            {
                int cellZ = centerZ + offsetZ;
                if (cellZ < 0 || cellZ >= cellsPerAxis)
                {
                    continue;
                }

                for (int offsetX = -searchRings; offsetX <= searchRings; offsetX++)
                {
                    int cellX = centerX + offsetX;
                    if (cellX < 0 || cellX >= cellsPerAxis)
                    {
                        continue;
                    }

                    int index = cellZ * cellsPerAxis + cellX;
                    Vector2 site = sites[index];
                    float dx = warpedX - site.x;
                    float dz = warpedZ - site.y;
                    float distance = Mathf.Sqrt(dx * dx + dz * dz);

                    if (distance < nearest)
                    {
                        secondNearest = nearest;
                        secondIndex = nearestIndex;
                        nearest = distance;
                        nearestIndex = index;
                    }
                    else if (distance < secondNearest)
                    {
                        secondNearest = distance;
                        secondIndex = index;
                    }
                }
            }

            primaryCell = nearestIndex;
            secondaryCell = secondIndex >= 0 ? secondIndex : nearestIndex;
            edgeDistance = secondIndex >= 0 ? secondNearest - nearest : float.MaxValue;

            if (blendWidth <= 0f || secondIndex < 0)
            {
                primaryWeight = 1f;
                return;
            }

            primaryWeight = WeightFor(edgeDistance, blendWidth);
        }

        /// <summary>境界からの距離差を主リージョンの重みに変換する</summary>
        public static float WeightFor(float edgeDistance, float width)
        {
            if (width <= 0f)
            {
                return 1f;
            }

            return Mathf.SmoothStep(0.5f, 1f, Mathf.Clamp01(edgeDistance / width));
        }

        /// <summary>設定されているブレンド幅(m)</summary>
        public float BlendWidth => blendWidth;

        /// <summary>
        /// 近傍セルまでの距離をまとめて返す。戻り値は有効な要素数。
        /// </summary>
        // 最近傍と次点の2つだけで混ぜると、移動に伴って「次点」が別のセルへ
        // 不連続に切り替わり、その切り替わり線（Voronoi頂点から伸びる直線）で高さが飛ぶ。
        // 3セル以上を重みで混ぜれば、セルの出入りは重み0を通るので不連続が消える。
        public int SampleDistances(float x, float z, int[] cells, float[] distances, out float minDistance)
        {
            Warp(x, z, out float warpedX, out float warpedZ);

            int centerX = Mathf.Clamp(Mathf.FloorToInt(warpedX / cellSize), 0, cellsPerAxis - 1);
            int centerZ = Mathf.Clamp(Mathf.FloorToInt(warpedZ / cellSize), 0, cellsPerAxis - 1);

            int count = 0;
            minDistance = float.MaxValue;

            for (int offsetZ = -searchRings; offsetZ <= searchRings; offsetZ++)
            {
                int cellZ = centerZ + offsetZ;
                if (cellZ < 0 || cellZ >= cellsPerAxis)
                {
                    continue;
                }

                for (int offsetX = -searchRings; offsetX <= searchRings; offsetX++)
                {
                    int cellX = centerX + offsetX;
                    if (cellX < 0 || cellX >= cellsPerAxis)
                    {
                        continue;
                    }

                    int index = cellZ * cellsPerAxis + cellX;
                    Vector2 site = sites[index];
                    float dx = warpedX - site.x;
                    float dz = warpedZ - site.y;
                    float distance = Mathf.Sqrt(dx * dx + dz * dz);

                    cells[count] = index;
                    distances[count] = distance;
                    count++;

                    if (distance < minDistance)
                    {
                        minDistance = distance;
                    }
                }
            }

            return count;
        }

        /// <summary>
        /// 距離を正規化された重みに変換する。合計は1になる。
        /// </summary>
        // 最近傍から width 以上離れたセルは重み0。境界を跨ぐとき、離れていくセルの重みは
        // 0へ滑らかに向かうので、セットから外れる瞬間にも段差が出ない。
        public static void ToWeights(
            float[] distances, int count, float minDistance, float width, float[] weights)
        {
            float safeWidth = Mathf.Max(1e-4f, width);
            float total = 0f;

            for (int i = 0; i < count; i++)
            {
                float t = (distances[i] - minDistance) / safeWidth;
                float weight = t >= 1f ? 0f : Mathf.SmoothStep(1f, 0f, t);

                weights[i] = weight;
                total += weight;
            }

            // 最近傍は必ず重み1を取るので total が0になることはない
            float inverseTotal = 1f / total;
            for (int i = 0; i < count; i++)
            {
                weights[i] *= inverseTotal;
            }
        }

        /// <summary>所属リージョンだけを返す。割り当て操作の当たり判定に使う</summary>
        public int CellAt(float x, float z)
        {
            Sample(x, z, out int primaryCell, out _, out _);
            return primaryCell;
        }

        private void Warp(float x, float z, out float warpedX, out float warpedZ)
        {
            if (warpAmount <= 0f)
            {
                warpedX = x;
                warpedZ = z;
                return;
            }

            float u = x * warpFrequency;
            float v = z * warpFrequency;

            warpedX = x + (Mathf.PerlinNoise(u + warpOffsetX, v + warpOffsetZ) - 0.5f) * warpAmount;
            warpedZ = z + (Mathf.PerlinNoise(u + warpOffsetZ + 37.1f, v + warpOffsetX + 91.7f) - 0.5f) * warpAmount;
        }

        #endregion
    }
}
