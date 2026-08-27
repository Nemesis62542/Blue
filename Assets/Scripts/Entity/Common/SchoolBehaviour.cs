using UnityEngine;

namespace Blue.Entity.Common
{
    /// <summary>
    /// 群れとして泳ぐ。結合・整列・逃走を操舵力として流し込む
    /// </summary>
    // 分離だけは BaseSwimmer が近傍探索付きで持っている。近くの相手との押し合いは
    // 局所的でないと意味がないため。結合と整列は群れ全体の重心・平均前方で足りるので、
    // SchoolController が 1 回だけ集計した値を全員で共有する。
    // 個体ごとに近傍探索すると 150 匹で 150 クエリ／フレームになる。
    public class SchoolBehaviour : SwimBehaviour
    {
        private readonly SchoolMember member;

        public SchoolBehaviour(SchoolMember member)
        {
            this.member = member;
        }

        public override void OnEnter(BaseSwimmer swimmer)
        {
            swimmer.MoveTo(swimmer.FindRoamPoint());
        }

        public override void OnDestinationReached(BaseSwimmer swimmer)
        {
            swimmer.MoveTo(swimmer.FindRoamPoint());
        }

        public override void Tick(BaseSwimmer swimmer, float deltaTime)
        {
            SchoolController school = member.School;
            if (school == null) return;

            Vector3 position = swimmer.transform.position;
            Vector3 steering = Vector3.zero;

            // 結合: 群れの重心へ寄る
            Vector3 toCentre = school.Centroid - position;
            if (toCentre.sqrMagnitude > 0.0001f)
            {
                steering += toCentre.normalized * school._cohesionWeight;
            }

            // 整列: 群れの平均的な向きに合わせる
            if (school.AverageForward.sqrMagnitude > 0.0001f)
            {
                steering += school.AverageForward * school._alignmentWeight;
            }

            // 逃走: 脅威から離れる。結合より強く効かせないと群れが割れない
            if (school.HasThreat)
            {
                Vector3 away = position - school.ThreatPosition;
                if (away.sqrMagnitude > 0.0001f)
                {
                    steering += away.normalized * school._fleeWeight;
                }
            }

            swimmer.AddSteering(steering);
        }
    }
}
