using System.Collections.Generic;
using UnityEngine;

namespace Blue.Aquarium
{
    /// <summary>
    /// セルとワールド座標の変換、および回転を含む占有セルの計算
    /// </summary>
    public static class AquariumGrid
    {
        public const float CELL_SIZE = 1.0f;
        public const int ROTATION_STEPS = 4; // 90度刻み

        /// <summary>
        /// 回転を反映した占有セル数を求める
        /// </summary>
        public static Vector2Int RotateFootprint(Vector2Int footprint, int rotation_step)
        {
            // 90度・270度では縦横が入れ替わる
            bool is_swapped = Mathf.Abs(rotation_step) % 2 == 1;

            return is_swapped ? new Vector2Int(footprint.y, footprint.x) : footprint;
        }

        /// <summary>
        /// 最小セルと回転から、占有するセルを列挙する
        /// </summary>
        public static IEnumerable<Vector2Int> EnumerateCells(Vector2Int origin, Vector2Int footprint, int rotation_step)
        {
            Vector2Int rotated = RotateFootprint(footprint, rotation_step);

            for (int x = 0; x < rotated.x; x++)
            {
                for (int y = 0; y < rotated.y; y++)
                {
                    yield return new Vector2Int(origin.x + x, origin.y + y);
                }
            }
        }

        /// <summary>
        /// 占有範囲の中心のワールド座標を求める
        /// </summary>
        public static Vector3 CellToWorld(Vector2Int origin, Vector2Int footprint, int rotation_step)
        {
            Vector2Int rotated = RotateFootprint(footprint, rotation_step);

            float center_x = (origin.x + rotated.x * 0.5f) * CELL_SIZE;
            float center_z = (origin.y + rotated.y * 0.5f) * CELL_SIZE;

            return new Vector3(center_x, 0f, center_z);
        }

        /// <summary>
        /// ワールド座標を含むセルを求める
        /// </summary>
        public static Vector2Int WorldToCell(Vector3 position)
        {
            return new Vector2Int(
                Mathf.FloorToInt(position.x / CELL_SIZE),
                Mathf.FloorToInt(position.z / CELL_SIZE)
            );
        }

        /// <summary>
        /// 回転段階をY軸回転に変換する
        /// </summary>
        public static Quaternion StepToRotation(int rotation_step)
        {
            return Quaternion.Euler(0f, NormalizeStep(rotation_step) * 90f, 0f);
        }

        /// <summary>
        /// 回転段階を 0〜3 に丸める
        /// </summary>
        public static int NormalizeStep(int rotation_step)
        {
            int step = rotation_step % ROTATION_STEPS;

            return step < 0 ? step + ROTATION_STEPS : step;
        }
    }
}
