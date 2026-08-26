using UnityEngine;

namespace Blue.Entity.Common
{
    /// <summary>
    /// ボーンを目標姿勢へバネ・ダンパで追従させる計算
    /// </summary>
    // 一次の指数補間（Slerp）は原理的に目標を行き過ぎないため、揺れ戻りが出ず動きが硬くなる。
    // 角速度を状態として持つ二次系にすることで、行き過ぎと揺れ戻りを作る。
    // メインチェーンと分岐で同じ挙動にしたいので、ここに集約している。
    internal static class BoneChainSpring
    {
        /// <summary>遅れ時間の下限（秒）</summary>
        public const float MinLagTime = 0.01f;

        /// <summary>遅れ時間の上限（秒）。これを超えると事実上停止して見える</summary>
        public const float MaxLagTime = 1f;

        // zeta を 0 にすると永久に揺れ続けるため下限を設ける
        private const float MinDampingRatio = 0.1f;

        // 1 サブステップあたりに許す omega * 刻み幅。これを超えると明示積分が発散する
        private const float MaxOmegaStep = 0.5f;

        // 分割数の上限。極端に低いフレームレートでは頭打ちになる
        private const int MaxSubSteps = 8;

        // これ以下の角速度は回転軸が不定になるので無視する
        private const float MinAngularStep = 1e-6f;

        /// <summary>
        /// 現在の姿勢を目標姿勢へ1ステップ近づける
        /// </summary>
        /// <param name="current">現在のワールド回転</param>
        /// <param name="target">目標のワールド回転</param>
        /// <param name="angularVelocity">角速度の状態。呼び出し側がボーンごとに保持する</param>
        /// <param name="lagTime">遅れ時間（秒）</param>
        /// <param name="overshoot">行き過ぎ量（0=臨界減衰、大きいほど揺れが残る）</param>
        /// <param name="deltaTime">デルタタイム</param>
        public static Quaternion Step(Quaternion current, Quaternion target, ref Vector3 angularVelocity,
            float lagTime, float overshoot, float deltaTime)
        {
            float clampedLag = Mathf.Clamp(lagTime, MinLagTime, MaxLagTime);

            // omega は固有角振動数、zeta は減衰比
            float omega = 1f / clampedLag;
            float zeta = Mathf.Max(1f - overshoot, MinDampingRatio);

            // 明示積分なので omega * 刻み幅 が大きいと発散する。omega を下げて回避すると
            // 1 フレームあたりの進み方が固定されてしまい、フレームレートによって追従の
            // 速さが変わる（lagTime を秒で指定した意味が無くなる）。刻みを分割して積む。
            int subSteps = Mathf.Clamp(Mathf.CeilToInt(omega * deltaTime / MaxOmegaStep), 1, MaxSubSteps);
            float subDeltaTime = deltaTime / subSteps;

            for (int i = 0; i < subSteps; i++)
            {
                // 目標までのズレを回転ベクトル（ラジアン）として取り出す
                Quaternion difference = Quaternion.Normalize(target * Quaternion.Inverse(current));
                difference.ToAngleAxis(out float differenceAngle, out Vector3 differenceAxis);
                if (differenceAngle > 180f)
                {
                    differenceAngle -= 360f;
                }
                Vector3 error = differenceAxis * (differenceAngle * Mathf.Deg2Rad);

                // 準陰的オイラー：速度を先に更新してから積分する
                angularVelocity += (omega * omega * error - 2f * zeta * omega * angularVelocity) * subDeltaTime;

                float angularStep = angularVelocity.magnitude * subDeltaTime;
                if (angularStep > MinAngularStep)
                {
                    current = Quaternion.AngleAxis(angularStep * Mathf.Rad2Deg, angularVelocity.normalized) * current;
                }
            }

            return current;
        }
    }
}
