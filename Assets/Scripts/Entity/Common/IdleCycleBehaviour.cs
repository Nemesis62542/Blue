using UnityEngine;

namespace Blue.Entity.Common
{
    /// <summary>
    /// 静止と遊泳を交互に繰り返す。イカやタコのように泳ぎ続けない生物の既定行動
    /// </summary>
    public class IdleCycleBehaviour : SwimBehaviour
    {
        private readonly Vector2 pauseTimeRange;
        private readonly Vector2 swimTimeRange;

        private bool isPaused;
        private float timer;
        private float duration;

        public IdleCycleBehaviour(Vector2 pauseTimeRange, Vector2 swimTimeRange)
        {
            this.pauseTimeRange = pauseTimeRange;
            this.swimTimeRange = swimTimeRange;
        }

        public override void OnEnter(BaseSwimmer swimmer)
        {
            // 静止から始める。遭遇時にいきなり泳ぎ出さないほうが落ち着いて見える
            isPaused = true;
            timer = 0f;
            duration = Random.Range(pauseTimeRange.x, pauseTimeRange.y);
            swimmer.Halt();
        }

        public override void Tick(BaseSwimmer swimmer, float deltaTime)
        {
            timer += deltaTime;
            if (timer < duration) return;

            isPaused = !isPaused;
            timer = 0f;

            if (isPaused)
            {
                duration = Random.Range(pauseTimeRange.x, pauseTimeRange.y);
                swimmer.Halt();
                return;
            }

            duration = Random.Range(swimTimeRange.x, swimTimeRange.y);
            swimmer.MoveTo(swimmer.FindRoamPoint());
        }

        public override void OnDestinationReached(BaseSwimmer swimmer)
        {
            // 遊泳時間が残っていれば泳ぎ続ける
            if (isPaused) return;

            swimmer.MoveTo(swimmer.FindRoamPoint());
        }
    }
}
