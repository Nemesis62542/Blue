using UnityEngine;

namespace Blue.Entity.Common
{
    /// <summary>
    /// 移動せず、対象の方向（または反対方向）を向き続ける。威嚇や警戒に使う
    /// </summary>
    public class FaceTargetBehaviour : SwimBehaviour
    {
        private readonly Transform target;
        private readonly bool faceAway;

        public FaceTargetBehaviour(Transform target, bool faceAway)
        {
            this.target = target;
            this.faceAway = faceAway;
        }

        public override void OnEnter(BaseSwimmer swimmer)
        {
            swimmer.Halt();
        }

        public override void Tick(BaseSwimmer swimmer, float deltaTime)
        {
            if (target == null)
            {
                swimmer.Halt();
                return;
            }

            // 水平成分だけを見る。相手を見上げる／見下ろす姿勢は取らせない
            Vector3 toTarget = target.position - swimmer.transform.position;
            toTarget.y = 0f;

            if (toTarget.sqrMagnitude < 0.0001f) return;

            swimmer.FaceTowards(faceAway ? -toTarget : toTarget);
        }
    }
}
