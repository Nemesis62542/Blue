using System;
using UnityEngine;

namespace Blue.Entity.Common
{
    /// <summary>
    /// 脅威から離れる方向へ逃げ続け、指定時間が過ぎたら完了を通知する
    /// </summary>
    public class FleeBehaviour : SwimBehaviour
    {
        private readonly Vector3 fleeDirection;
        private readonly float legDistance;
        private readonly float duration;
        private readonly float speedScale;
        private readonly Action onComplete;

        private float elapsed;
        private bool completed;

        /// <param name="legDistance">1 区間で狙う距離。到達するたびに継ぎ足すので総移動距離ではない</param>
        public FleeBehaviour(Vector3 fleeDirection, float legDistance, float duration, float speedScale,
            Action onComplete = null)
        {
            this.fleeDirection = fleeDirection.normalized;
            this.legDistance = Mathf.Max(legDistance, 0.1f);
            this.duration = duration;
            this.speedScale = speedScale;
            this.onComplete = onComplete;
        }

        /// <summary>
        /// 脅威から離れる向きを求めて生成する
        /// </summary>
        public static FleeBehaviour AwayFrom(BaseSwimmer swimmer, Vector3 threatPosition, float legDistance,
            float duration, float speedScale, Action onComplete = null)
        {
            Vector3 direction = swimmer.transform.position - threatPosition;

            direction = direction.sqrMagnitude > 0.0001f
                ? direction.normalized
                : swimmer.transform.forward;

            return new FleeBehaviour(direction, legDistance, duration, speedScale, onComplete);
        }

        public override void OnEnter(BaseSwimmer swimmer)
        {
            elapsed = 0f;
            completed = false;

            swimmer.SetSpeedScale(speedScale);
            ExtendDestination(swimmer);
        }

        public override void OnExit(BaseSwimmer swimmer)
        {
            swimmer.SetSpeedScale(1f);
        }

        public override void Tick(BaseSwimmer swimmer, float deltaTime)
        {
            if (completed) return;

            elapsed += deltaTime;
            if (elapsed < duration) return;

            Complete(swimmer);
        }

        public override void OnDestinationReached(BaseSwimmer swimmer)
        {
            if (completed) return;

            // 到達で終わらせると数秒で泳ぎ切って止まってしまう。
            // 逃走時間が尽きるまで目的地を前へ継ぎ足し続ける
            ExtendDestination(swimmer);
        }

        // 障害物は回避に任せて素直に逃走方向を指す。
        // 塞がれていても停滞回復が働くので目的地の手前で固まることはない
        private void ExtendDestination(BaseSwimmer swimmer)
        {
            swimmer.MoveTo(swimmer.transform.position + fleeDirection * legDistance);
        }

        private void Complete(BaseSwimmer swimmer)
        {
            completed = true;
            swimmer.Halt();

            // 完了通知の中で行動が差し替えられるため、通知は最後に行う
            onComplete?.Invoke();
        }
    }
}
