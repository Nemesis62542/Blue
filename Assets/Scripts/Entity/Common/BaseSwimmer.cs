using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Blue.Entity.Common
{
    public class BaseSwimmer : MonoBehaviour
    {
        [Header("Swim Settings")]
        [SerializeField] private float moveSpeed = 3.0f;
        [SerializeField] private float rotationSpeed = 5.0f;
        [SerializeField] private float turnSmoothing = 4.0f; // 小さいほど滑らか。方向の切り替わりを均す段の速さ
        [SerializeField] private float waypointDistance = 1.0f;
        [SerializeField, Range(0f, 89f)] private float maxPitchAngle = 60f;
        [SerializeField] private bool maintainCruise = true; // 目的地の切り替えで減速しない（Halt による明示的な停止は別）

        [Header("Motion Style")]
        [SerializeField, Range(0f, 1f)] private float wanderStrength = 0.15f;
        [SerializeField] private float wanderFrequency = 0.3f;
        [SerializeField, Range(0f, 80f)] private float maxBankAngle = 25f;
        [SerializeField, Range(0f, 45f)] private float maxPitchOffset = 12f;
        [SerializeField] private float bankResponse = 3.0f;

        // moveSpeed / rotationSpeed を上限として、目的地ごとに実際の値を引き直す。
        // 等速・等旋回で泳ぎ続けると、単体で見ても機械的に見えるため
        [Header("Motion Variation")]
        [SerializeField, Range(0f, 0.9f)] private float speedVariation = 0.4f;
        [SerializeField, Range(0f, 0.9f)] private float turnVariation = 0.4f;
        [SerializeField] private float accelerationRate = 1.0f;
        [SerializeField] private float decelerationRate = 0.4f; // 加速より遅くして惰性を出す
        [SerializeField, Range(1f, 4f)] private float lowSpeedTurnBonus = 1.5f;

        [Header("Avoidance Settings")]
        [SerializeField] private bool useAvoidance = true;
        [SerializeField] private float avoidDistance = 1.0f;
        [SerializeField] private float avoidProbeRadius = 0.25f;
        [SerializeField] private LayerMask avoidanceMask = ~0;
        [SerializeField, Range(0f, 1f)] private float avoidAlignmentWeight = 0.5f;
        [SerializeField, Range(0f, 1f)] private float avoidHysteresis = 0.25f;
        [SerializeField, Range(0f, 1f)] private float avoidMinSpeedFactor = 0.25f;

        [Header("Push Settings")]
        [SerializeField] private float pushDistance = 1.0f;
        [SerializeField] private float pushForce = 2.0f;

        [Header("Separation Settings")]
        [SerializeField] private bool useSeparation = true;
        [SerializeField] private float separationRadius = 1.5f;
        [SerializeField] private float separationWeight = 1.0f;
        [SerializeField] private LayerMask separationMask = ~0;
        [SerializeField] private float separationInterval = 0.1f; // 探索の間隔。毎フレームは要らない

        [Header("Roaming Settings")]
        [SerializeField] private bool roamCenterFromSpawnPosition = true;
        [SerializeField] private Vector3 roamCenter = Vector3.zero;
        [SerializeField] private Vector3 roamArea = new Vector3(5f, 2f, 5f);

        // 縄張りの中心そのものをゆっくり移動させる。水槽など動いては困る場所があるので既定は off
        [Header("Migration Settings")]
        [SerializeField] private bool useMigration = false;
        [SerializeField] private Vector3 migrationRange = new Vector3(20f, 5f, 20f);
        [SerializeField] private float migrationSpeed = 0.5f;
        [SerializeField] private Vector2 migrationIntervalRange = new Vector2(15f, 30f);

        [Header("Waypoint Settings")]
        [SerializeField] private int waypointAttempts = 12;
        [SerializeField] private float waypointClearance = 0.5f;
        [SerializeField] private bool requireClearPath = true;
        [SerializeField] private float stuckToleranceGrowth = 0.5f;
        [SerializeField, Range(0f, 180f)] private float roamHeadingLimit = 120f;

#if UNITY_EDITOR
        [Header("Debug Gizmos")]
        [SerializeField] private bool showGizmos = true;
        [SerializeField] private bool alwaysShowGizmos = false; // 非選択時も描画（俯瞰用の簡易表示）
        [SerializeField] private bool showRoamArea = true;
        [SerializeField] private bool showWaypoint = true;
        [SerializeField] private bool showReachability = true;
        [SerializeField] private bool showAvoidanceRays = true;
        [SerializeField] private bool showSeparation = true;
        [SerializeField] private bool showTrail = true;
        [SerializeField] private bool showLabels = true;
        [SerializeField, Range(1f, 60f)] private float trailSeconds = 12f;
        [SerializeField] private float labelHeightOffset = 0.6f;
#endif

        // 進捗が縮んだと見なす最小量。数値誤差で「前進した」と誤判定しないための下限
        private const float ProgressEpsilon = 0.05f;

        // 扇状スキャンの固定候補に加える、衝突面から導出する候補の数（滑る方向・離れる方向）
        private const int DerivedProbeCount = 2;

        // 前方を軸にした円錐状の候補方向（ローカル座標）。水平だけでなく上下にも逃げ道を持たせる
        private static readonly Vector3[] ProbeDirections = BuildProbeDirections();

        private Vector3 destination;
        private Vector3 faceDirection;
        private float currentSpeed;
        private float speedScale = 1f;
        private float targetSpeed;
        private float targetTurnSpeed;
        private float currentTurnSpeed;
        private Vector3 smoothedHeading;
        private float bestDistanceToWaypoint;
        private float stuckTime;
        private Vector3 lastAvoidDirection;
        private SwimBehaviour behaviour;
        private SwimMode mode = SwimMode.Idle;
        private int destinationVersion;
        private float currentBank;
        private float currentPitchOffset;
        private Quaternion motionRotation = Quaternion.identity;
        private float wanderSeed;
        private bool roamCenterOverridden;
        private Vector3 steeringAccumulator;
        private Vector3 separationForce;
        private float separationTimer;
        private readonly Collider[] neighbourBuffer = new Collider[16];
        private readonly RaycastHit[] probeBuffer = new RaycastHit[8];
        private Collider[] ownColliders;
        private Vector3 homeCenter;
        private Vector3 migrationTarget;
        private float migrationTimer;

        /// <summary>
        /// 現在の駆動状態
        /// </summary>
        public SwimMode Mode => mode;

        /// <summary>
        /// 目的地へ向かって移動中か
        /// </summary>
        public bool IsMoving => mode == SwimMode.Move;

        /// <summary>
        /// 縄張りの中心
        /// </summary>
        public Vector3 RoamCenter => roamCenter;

        // 見た目に乗せる傾き（バンク・機首上げ下げ）を含まない、移動に使う向き。
        // transform.forward を使うと、姿勢のための傾きが進行方向へ混ざってしまう
        private Vector3 MotionForward => motionRotation * Vector3.forward;

        /// <summary>
        /// 移動状態が変化したときに通知する。遊泳アニメの切り替えに使う
        /// </summary>
        public event Action<bool> OnMovingChanged;

        private static Vector3[] BuildProbeDirections()
        {
            float[] ringAngles = { 30f, 60f, 90f };
            float[] ringOffsets = { 0f, 30f, 0f }; // リングごとに方位をずらして隙間を減らす
            const int AzimuthCount = 6;

            List<Vector3> directions = new List<Vector3>(1 + ringAngles.Length * AzimuthCount)
            {
                Vector3.forward
            };

            for (int ring = 0; ring < ringAngles.Length; ring++)
            {
                float polar = ringAngles[ring] * Mathf.Deg2Rad;
                float sin = Mathf.Sin(polar);
                float cos = Mathf.Cos(polar);

                for (int i = 0; i < AzimuthCount; i++)
                {
                    float azimuth = (i * 360f / AzimuthCount + ringOffsets[ring]) * Mathf.Deg2Rad;
                    directions.Add(new Vector3(sin * Mathf.Cos(azimuth), sin * Mathf.Sin(azimuth), cos));
                }
            }

            return directions.ToArray();
        }

        protected virtual void Start()
        {
            // Instantiate 直後に SetRoamCenter で縄張りを指定された場合は、そちらを優先する。
            // 無条件に上書きするとスポーン側からの指定が必ず消える
            if (roamCenterFromSpawnPosition && !roamCenterOverridden)
            {
                roamCenter = transform.position;
            }

            motionRotation = transform.rotation;

            // 回遊の基準。roamCenter は動くが、こちらは動かさない
            homeCenter = roamCenter;
            migrationTarget = roamCenter;

            // 個体ごとに異なるゆらぎを与える。同じ地点へ向かう群れが同期して見えないように
            wanderSeed = UnityEngine.Random.value * 1000f;

            RerollMotionParameters();

            smoothedHeading = MotionForward;
            currentTurnSpeed = targetTurnSpeed;

            CacheOwnColliders();

            // 行動を明示されていない個体は徘徊させる
            if (behaviour == null) SetBehaviour(new RoamBehaviour());
        }

        protected virtual void Update()
        {
            UpdateMigration();

            // 分離も行動と同じ注入口を通す。BaseSwimmer 自身が最初の利用者になることで、
            // 群れがこの口に乗る前に動作が確かめられる
            UpdateSeparation();
            AddSteering(separationForce);

            behaviour?.Tick(this, Time.deltaTime);

            switch (mode)
            {
                case SwimMode.Move:
                    UpdateMove();
                    TryPushBack();
                    break;

                case SwimMode.Face:
                    UpdateFace();
                    break;
            }

            // 使わなかった分も含めて毎フレーム捨てる。持ち越すと力が際限なく溜まる
            steeringAccumulator = Vector3.zero;
        }

        #region Movement API

        /// <summary>
        /// 行動を差し替える
        /// </summary>
        public void SetBehaviour(SwimBehaviour next)
        {
            if (behaviour == next) return;

            behaviour?.OnExit(this);
            behaviour = next;
            behaviour?.OnEnter(this);
        }

        /// <summary>
        /// 目的地を設定し、移動を開始する
        /// </summary>
        public void MoveTo(Vector3 point)
        {
            destination = point;
            destinationVersion++;

            // 区間ごとに速度と旋回性能を引き直す。これをしないと単体で見たときに
            // 常に同じ速さ・同じ曲がり方になり、生き物に見えない
            RerollMotionParameters();

            // 泳ぎを止めない生物は目的地の切り替えで速度を落とさない。
            // 落とすと数メートルごとに停止と再加速を繰り返す動きになる
            if (!maintainCruise || mode != SwimMode.Move)
            {
                currentSpeed = 0f;
            }

            stuckTime = 0f;
            bestDistanceToWaypoint = Vector3.Distance(transform.position, destination);

            SetMode(SwimMode.Move);
        }

        /// <summary>
        /// その場で指定方向を向く
        /// </summary>
        public void FaceTowards(Vector3 direction)
        {
            if (direction.sqrMagnitude < 0.0001f) return;

            faceDirection = direction.normalized;
            SetMode(SwimMode.Face);
        }

        /// <summary>
        /// 停止する
        /// </summary>
        public void Halt()
        {
            currentSpeed = 0f;
            SetMode(SwimMode.Idle);
        }

        // 速いほど大きく回り、遅いほど小回りが利く。
        // 減速側ではなく低速側に上乗せする形にしてあるので、全速時の旋回性能は
        // rotationSpeed のまま変わらない。周回の安全条件を悪化させないため
        private float GetTurnRate()
        {
            float speedRatio = moveSpeed > Mathf.Epsilon ? Mathf.Clamp01(currentSpeed / moveSpeed) : 0f;

            return currentTurnSpeed * Mathf.Lerp(lowSpeedTurnBonus, 1f, speedRatio);
        }

        // 最も曲がれない組み合わせ（最大速度 × 最遅旋回）での旋回半径。
        // 到達判定がこれを下回ると、その差の帯に入ったとき目標の周りを回り続ける
        private float WorstCaseTurnRadius()
        {
            float slowestTurn = rotationSpeed * (1f - turnVariation);

            return slowestTurn > Mathf.Epsilon ? moveSpeed / slowestTurn : float.PositiveInfinity;
        }

        // 区間ごとの実効速度と旋回速度を抽選する。上限は moveSpeed / rotationSpeed
        private void RerollMotionParameters()
        {
            targetSpeed = moveSpeed * UnityEngine.Random.Range(1f - speedVariation, 1f);
            targetTurnSpeed = rotationSpeed * UnityEngine.Random.Range(1f - turnVariation, 1f);
        }

        /// <summary>
        /// このフレームの操舵力を加える。毎フレーム呼び直す必要がある
        /// </summary>
        // 目的地では表現できない「毎フレーム変化する力」の入口。
        // 分離のほか、群れの整列・結合・逃走もここに合流させる
        public void AddSteering(Vector3 force)
        {
            steeringAccumulator += force;
        }

        /// <summary>
        /// 移動速度の倍率。逃走などの一時的な加速に使う
        /// </summary>
        public void SetSpeedScale(float scale)
        {
            speedScale = Mathf.Max(0f, scale);
        }

        /// <summary>
        /// 縄張りの中心を設定する
        /// </summary>
        public void SetRoamCenter(Vector3 center)
        {
            roamCenter = center;
            roamCenterOverridden = true;

            // 回遊の基準ごと移す。指定された位置を中心に泳ぎ回ってほしいはずなので
            homeCenter = center;
            migrationTarget = center;
        }

        private void SetMode(SwimMode next)
        {
            if (mode == next) return;

            bool wasMoving = IsMoving;
            mode = next;

            if (wasMoving != IsMoving) OnMovingChanged?.Invoke(IsMoving);
        }

        #endregion

#if UNITY_EDITOR
        // 派生クラスが Update をスキップしても軌跡が途切れないよう LateUpdate で記録する
        private void LateUpdate()
        {
            RecordTrail();
        }

        private void OnValidate()
        {
            if (moveSpeed <= 0f || rotationSpeed <= Mathf.Epsilon) return;

            float turnRadius = WorstCaseTurnRadius();
            if (turnRadius <= waypointDistance) return;

            Debug.LogWarning(
                $"[BaseSwimmer] {name}: waypointDistance({waypointDistance:F2}) が旋回半径({turnRadius:F2}) 以下です。" +
                $" {waypointDistance:F2}〜{turnRadius:F2}m の軌道に入ると目標の周りを回り続けます。" +
                $" スタック回復で最終的に抜けますが不自然に見えるため、waypointDistance を {turnRadius:F2} より大きくするか" +
                $" rotationSpeed を上げてください。", this);
        }
#endif

        private void UpdateFace()
        {
            // その場で向きを変えるだけなので、傾きは水平へ戻す
            float response = bankResponse * Time.deltaTime;
            currentBank = Mathf.Lerp(currentBank, 0f, response);
            currentPitchOffset = Mathf.Lerp(currentPitchOffset, 0f, response);

            smoothedHeading = SmoothDirection(smoothedHeading, ClampPitch(faceDirection), turnSmoothing);

            // 意図して相手に向き直る動作なので、旋回性能は抽選値ではなく上限を使う
            Quaternion targetRotation = Quaternion.LookRotation(smoothedHeading, Vector3.up);
            motionRotation = Quaternion.Slerp(motionRotation, targetRotation,
                SmoothingFactor(rotationSpeed, Time.deltaTime));

            transform.rotation = motionRotation * Quaternion.Euler(currentPitchOffset, 0f, currentBank);
        }

        // フレームレートに依存しない指数平滑の係数。rate * deltaTime を直に渡すと
        // dt の揺れがそのまま補間量の揺れになり、カクつきとして見える
        private static float SmoothingFactor(float rate, float deltaTime)
        {
            return 1f - Mathf.Exp(-rate * deltaTime);
        }

        private static Vector3 SmoothDirection(Vector3 current, Vector3 target, float rate)
        {
            if (current.sqrMagnitude < 0.0001f) return target;

            return Vector3.Slerp(current, target, SmoothingFactor(rate, Time.deltaTime)).normalized;
        }

        private void UpdateMove()
        {
            if (HasReachedDestination())
            {
                int version = destinationVersion;
                behaviour?.OnDestinationReached(this);

                // 行動側が停止や旋回に切り替えたなら、この場での移動処理は不要
                if (mode != SwimMode.Move) return;

                // 次の目的地が入らなければ到達通知が毎フレーム鳴り続けるので止める
                if (destinationVersion == version)
                {
                    Halt();
                    return;
                }
            }

            Vector3 desired = destination - transform.position;
            desired = desired.sqrMagnitude > 0.0001f ? desired.normalized : MotionForward;

            // 障害物より先にゆらぎと操舵力を乗せる。回避はその結果を見たうえで上書きできる
            desired = ApplyWander(desired);
            desired = ApplySteering(desired);

            // 回避は「目標回転を上書きする」のではなく「進みたい方向そのものを差し替える」。
            // こうしないと補正が Slerp で二重に減衰し、dt^2 に比例して消えてしまう
            Vector3 heading = ClampPitch(EvaluateAvoidance(desired, out float speedFactor));

            // 目的地の切り替わりや回避候補の乗り換えで方向は跳ぶ。姿勢に落とす前に一段均すと、
            // 姿勢側の追従と二段になって旋回の入り・抜けが S 字になる
            smoothedHeading = SmoothDirection(smoothedHeading, heading, turnSmoothing);

            // 旋回速度も区間ごとに瞬間で切り替えると段差になるため、寄せていく
            currentTurnSpeed = Mathf.MoveTowards(currentTurnSpeed, targetTurnSpeed,
                rotationSpeed * turnSmoothing * Time.deltaTime);

            // 進行方向は motionRotation が持ち、見せかけの傾きは transform だけに乗せる
            Quaternion targetRotation = Quaternion.LookRotation(smoothedHeading, Vector3.up);
            motionRotation = Quaternion.Slerp(motionRotation, targetRotation,
                SmoothingFactor(GetTurnRate(), Time.deltaTime));

            transform.rotation = motionRotation * Quaternion.Euler(
                UpdatePitchOffset(smoothedHeading), 0f, UpdateBank(smoothedHeading));

            // 加速と減速でレートを分ける。減速を遅くすると惰性が出る
            float goalSpeed = targetSpeed * speedFactor * speedScale;
            float rate = goalSpeed > currentSpeed ? accelerationRate : decelerationRate;
            currentSpeed = Mathf.MoveTowards(currentSpeed, goalSpeed, moveSpeed * rate * Time.deltaTime);

            transform.position += MotionForward * currentSpeed * Time.deltaTime;
        }

        // 縄張りの中心を少しずつ移す。中心が固定だと、どれだけ動きを作り込んでも
        // 一生同じ箱の中を往復し続けることになる
        private void UpdateMigration()
        {
            if (!useMigration) return;

            migrationTimer -= Time.deltaTime;

            if (migrationTimer <= 0f)
            {
                migrationTimer = UnityEngine.Random.Range(migrationIntervalRange.x, migrationIntervalRange.y);
                migrationTarget = FindMigrationTarget();
            }

            // 瞬間移動させず寄せていく。目的地は中心から抽選されるので、
            // 中心が動けば行き先も自然に移っていく
            roamCenter = Vector3.MoveTowards(roamCenter, migrationTarget, migrationSpeed * Time.deltaTime);
        }

        private Vector3 FindMigrationTarget()
        {
            for (int attempt = 0; attempt < waypointAttempts; attempt++)
            {
                Vector3 candidate = homeCenter + new Vector3(
                    UnityEngine.Random.Range(-migrationRange.x, migrationRange.x),
                    UnityEngine.Random.Range(-migrationRange.y, migrationRange.y),
                    UnityEngine.Random.Range(-migrationRange.z, migrationRange.z)
                );

                // 縄張りごと地形に埋まると、そこで抽選される目的地が軒並み無効になる
                if (Physics.CheckSphere(candidate, waypointClearance, avoidanceMask, QueryTriggerInteraction.Ignore))
                {
                    continue;
                }

                return candidate;
            }

            return homeCenter;
        }

        // 近くの個体から離れる力を求める。毎フレームは探索せず、間隔を空けて結果を使い回す
        private void UpdateSeparation()
        {
            if (!useSeparation || separationWeight <= 0f || separationRadius <= 0f)
            {
                separationForce = Vector3.zero;
                return;
            }

            separationTimer -= Time.deltaTime;
            if (separationTimer > 0f) return;

            separationTimer = separationInterval;

            Vector3 position = transform.position;
            int count = Physics.OverlapSphereNonAlloc(position, separationRadius, neighbourBuffer,
                separationMask, QueryTriggerInteraction.Ignore);

            Vector3 force = Vector3.zero;

            for (int i = 0; i < count; i++)
            {
                Collider neighbour = neighbourBuffer[i];

                // 移動用ボリュームは子に付くため、親を辿って所有者へ解決する。
                // コライダー自身から探すと、分割された生物が丸ごと検出できなくなる
                BaseSwimmer other = neighbour.GetComponentInParent<BaseSwimmer>();
                if (other == null || other == this) continue;

                // 力の向きはコライダーではなく個体の位置で決める
                Vector3 offset = position - other.transform.position;
                float distance = offset.magnitude;
                if (distance < 0.0001f) continue;

                // 近いほど強く効かせる。距離で減衰させないと、半径ぎりぎりの相手も
                // 真横にいる相手も同じ強さになってしまう
                force += offset / distance * (1f - distance / separationRadius);
            }

            separationForce = force * separationWeight;
        }

        // 目的地への方向に操舵力を合成する。障害物回避はこのあとに掛かるので、
        // 優先順位は 壁 > 個体 > 目的地 になる
        private Vector3 ApplySteering(Vector3 direction)
        {
            if (steeringAccumulator.sqrMagnitude < 0.0001f) return direction;

            Vector3 combined = direction + steeringAccumulator;

            return combined.sqrMagnitude > 0.0001f ? combined.normalized : direction;
        }

        // 目標方向に低周波のゆらぎを乗せる。ゆらぎが無いと点と点を直線で結ぶ動きになり、
        // 生き物というより移動する物体に見えてしまう
        private Vector3 ApplyWander(Vector3 direction)
        {
            if (wanderStrength <= 0f) return direction;

            float time = Time.time * wanderFrequency;
            float yaw = Mathf.PerlinNoise(wanderSeed, time) - 0.5f;
            float pitch = Mathf.PerlinNoise(wanderSeed + 37.7f, time) - 0.5f;

            Vector3 offset = transform.right * yaw + transform.up * pitch;

            return (direction + offset * (wanderStrength * 2f)).normalized;
        }

        // 昇り降りの分だけ余分に機首を上げ下げする。経路と体の向きをわずかにずらすことで、
        // 上下移動が「進行方向を向いているだけ」ではなく姿勢として見える。
        // transform にしか乗せないので、進行方向そのものには影響しない
        private float UpdatePitchOffset(Vector3 heading)
        {
            if (maxPitchOffset <= 0f) return 0f;

            // 上昇時は機首上げ ＝ ローカル X 軸まわりの負回転
            float target = -Mathf.Clamp(heading.y, -1f, 1f) * maxPitchOffset;
            currentPitchOffset = Mathf.Lerp(currentPitchOffset, target, bankResponse * Time.deltaTime);

            return currentPitchOffset;
        }

        // 旋回方向へ機体を傾ける。水平面での向きのズレを傾き量に写す。
        // 前方軸まわりの回転なので進行方向そのものには影響しない
        private float UpdateBank(Vector3 heading)
        {
            if (maxBankAngle <= 0f) return 0f;

            Vector3 flatForward = Vector3.ProjectOnPlane(MotionForward, Vector3.up);
            Vector3 flatHeading = Vector3.ProjectOnPlane(heading, Vector3.up);

            float targetBank = 0f;

            if (flatForward.sqrMagnitude > 0.0001f && flatHeading.sqrMagnitude > 0.0001f)
            {
                float yawError = Vector3.SignedAngle(flatForward, flatHeading, Vector3.up);

                // 右へ曲がるときは右舷が下がる ＝ ローカル Z 軸まわりの負回転
                targetBank = -Mathf.Clamp(yawError / 90f, -1f, 1f) * maxBankAngle;
            }

            currentBank = Mathf.Lerp(currentBank, targetBank, bankResponse * Time.deltaTime);

            return currentBank;
        }

        // 水平成分に対する仰角を制限する。真上・真下を向くと LookRotation が
        // ワールド up と縮退してロールが暴れるため、その手前で頭打ちにする
        private Vector3 ClampPitch(Vector3 direction)
        {
            Vector3 flat = new Vector3(direction.x, 0f, direction.z);
            if (flat.sqrMagnitude < 0.0001f) return MotionForward;

            float maxRise = flat.magnitude * Mathf.Tan(maxPitchAngle * Mathf.Deg2Rad);
            float rise = Mathf.Clamp(direction.y, -maxRise, maxRise);

            return new Vector3(direction.x, rise, direction.z).normalized;
        }

        // 目標に近づけている間は判定半径を据え置き、進捗が止まった時間に比例して広げる。
        // 旋回半径が waypointDistance を上回る軌道や、押し戻しで前進が相殺される停滞から
        // 必ず抜けられるようにするための保証。上限を設けると保証が壊れるので設けない。
        private bool HasReachedDestination()
        {
            float distance = Vector3.Distance(transform.position, destination);

            if (distance < bestDistanceToWaypoint - ProgressEpsilon)
            {
                bestDistanceToWaypoint = distance;
                stuckTime = 0f;
            }
            else
            {
                stuckTime += Time.deltaTime;
            }

            return distance < waypointDistance + stuckTime * stuckToleranceGrowth;
        }

        /// <summary>
        /// 縄張りの中から到達可能な地点を 1 つ選ぶ
        /// </summary>
        public Vector3 FindRoamPoint()
        {
            Vector3 origin = transform.position;

            // まず進行方向寄りの候補を探す。全方位から一様に選ぶと真後ろが等確率で出て、
            // 数メートルおきに折り返す不自然な動きになる
            if (roamHeadingLimit < 180f && TryFindRoamPoint(origin, roamHeadingLimit, out Vector3 ahead))
            {
                return ahead;
            }

            // 前方が塞がっている場合まで折り返しを禁じると袋小路で動けなくなるため、
            // 見つからなければ制限を外して探し直す
            if (TryFindRoamPoint(origin, 180f, out Vector3 anywhere))
            {
                return anywhere;
            }

            return FindFallbackWaypoint(origin);
        }

        private bool TryFindRoamPoint(Vector3 origin, float headingLimit, out Vector3 result)
        {
            for (int attempt = 0; attempt < waypointAttempts; attempt++)
            {
                Vector3 candidate = roamCenter + new Vector3(
                    UnityEngine.Random.Range(-roamArea.x, roamArea.x),
                    UnityEngine.Random.Range(-roamArea.y, roamArea.y),
                    UnityEngine.Random.Range(-roamArea.z, roamArea.z)
                );

                if (headingLimit < 180f &&
                    Vector3.Angle(MotionForward, candidate - origin) > headingLimit)
                {
                    continue;
                }

                // 地形やオブジェクトの内部に湧いた候補は永久に到達できない
                if (Physics.CheckSphere(candidate, waypointClearance, avoidanceMask, QueryTriggerInteraction.Ignore))
                {
                    continue;
                }

                // 壁の向こう側の候補も同様。回避は経路探索ではないので回り込めない
                if (requireClearPath &&
                    Physics.Linecast(origin, candidate, avoidanceMask, QueryTriggerInteraction.Ignore))
                {
                    continue;
                }

                result = candidate;
                return true;
            }

            result = origin;
            return false;
        }

        // 有効な候補が尽きた場合。前方の短い点を徘徊範囲に丸めて返し、
        // 次の到達時に改めて抽選し直させる（停止はさせない）
        private Vector3 FindFallbackWaypoint(Vector3 origin)
        {
            float reach = Mathf.Max(waypointDistance * 2f, 0.01f);
            Vector3 candidate = ClampToRoamArea(origin + MotionForward * reach);

            // 範囲の端で外を向いていると、丸めた結果が到達判定内に落ちて毎フレーム
            // 抽選し直す状態になりうる。その場合は範囲の内側へ向け直す
            if (Vector3.Distance(origin, candidate) >= reach * 0.5f) return candidate;

            Vector3 inward = roamCenter - origin;
            if (inward.sqrMagnitude < 0.0001f) inward = MotionForward;

            return ClampToRoamArea(origin + inward.normalized * reach);
        }

        private Vector3 ClampToRoamArea(Vector3 point)
        {
            Vector3 local = point - roamCenter;
            local.x = Mathf.Clamp(local.x, -roamArea.x, roamArea.x);
            local.y = Mathf.Clamp(local.y, -roamArea.y, roamArea.y);
            local.z = Mathf.Clamp(local.z, -roamArea.z, roamArea.z);

            return roamCenter + local;
        }

        // 進みたい方向に対し、実際に進める方向を返す。あわせて速度倍率を出す。
        // 単発 if の優先順位で 1 つの補正を選ぶのではなく、全候補を採点して選ぶため
        // 「手前の判定に消費されて奥の障害物を見ない」という取りこぼしが起きない
        private Vector3 EvaluateAvoidance(Vector3 desired, out float speedFactor)
        {
            speedFactor = 1f;
            if (!useAvoidance) return desired;

            Vector3 position = transform.position;

            // 前方が空いていれば探索しない。通常遊泳時のコストを 1 クエリに抑えるための門番
            if (!Probe(position, MotionForward, avoidDistance, out RaycastHit blocker))
            {
                lastAvoidDirection = Vector3.zero;
                return desired;
            }

            // 障害物が近いほど減速する。毎フレーム倍率として掛け直すので、
            // 以前のように次フレームの加速処理で上書きされて消えることがない
            float blockage = 1f - Mathf.Clamp01(blocker.distance / avoidDistance);
            speedFactor = Mathf.Lerp(1f, avoidMinSpeedFactor, blockage);

            Vector3 surfaceNormal = blocker.normal;
            float bestScore = float.NegativeInfinity;
            Vector3 bestDirection = desired;

            for (int i = 0; i < ProbeDirections.Length + DerivedProbeCount; i++)
            {
                Vector3 direction = GetProbeDirection(i, desired, surfaceNormal);
                if (direction.sqrMagnitude < 0.0001f) continue;

                direction.Normalize();

                // 空きの大きさを主、目標方向との一致を従とする。
                // 履歴項は左右どちらに逃げるかが毎フレーム入れ替わって相殺するのを防ぐ
                float score = MeasureClearance(position, direction)
                              + avoidAlignmentWeight * ToUnitRange(Vector3.Dot(direction, desired))
                              + avoidHysteresis * ToUnitRange(Vector3.Dot(direction, lastAvoidDirection));

                if (score <= bestScore) continue;

                bestScore = score;
                bestDirection = direction;
            }

            lastAvoidDirection = bestDirection;
            return bestDirection;
        }

        private Vector3 GetProbeDirection(int index, Vector3 desired, Vector3 surfaceNormal)
        {
            if (index < ProbeDirections.Length)
            {
                return motionRotation * ProbeDirections[index];
            }

            // 衝突面から導出する候補。壁は「跳ね返る」のではなく「沿って滑る」のが自然
            if (surfaceNormal.sqrMagnitude < 0.0001f) return Vector3.zero;

            return index == ProbeDirections.Length
                ? Vector3.ProjectOnPlane(desired, surfaceNormal)
                : surfaceNormal;
        }

        private float MeasureClearance(Vector3 position, Vector3 direction)
        {
            if (Probe(position, direction, avoidDistance, out RaycastHit hit))
            {
                return Mathf.Clamp01(hit.distance / avoidDistance);
            }

            return 1f;
        }

        // 自分のコライダーを除いた最寄りのヒットを返す。
        // 移動用ボリュームは自分の内側にあり、探索の原点がその中に入るため、
        // 素直に撃つと常に「正面が塞がっている」と判定されて減速し続ける
        private bool Probe(Vector3 origin, Vector3 direction, float distance, out RaycastHit nearest)
        {
            int count = Physics.SphereCastNonAlloc(origin, avoidProbeRadius, direction, probeBuffer,
                distance, avoidanceMask, QueryTriggerInteraction.Ignore);

            nearest = default;
            bool found = false;

            for (int i = 0; i < count; i++)
            {
                if (IsOwnCollider(probeBuffer[i].collider)) continue;
                if (found && probeBuffer[i].distance >= nearest.distance) continue;

                nearest = probeBuffer[i];
                found = true;
            }

            return found;
        }

        private bool IsOwnCollider(Collider candidate)
        {
            if (ownColliders == null) return false;

            for (int i = 0; i < ownColliders.Length; i++)
            {
                if (ownColliders[i] == candidate) return true;
            }

            return false;
        }

        // トリガーは全クエリで除外しているので、非トリガーだけ覚えておけば足りる
        private void CacheOwnColliders()
        {
            List<Collider> solids = new List<Collider>();

            foreach (Collider collider in GetComponentsInChildren<Collider>(true))
            {
                if (!collider.isTrigger) solids.Add(collider);
            }

            ownColliders = solids.ToArray();
        }

        // 内積 [-1,1] を重み付けに使える [0,1] へ
        private static float ToUnitRange(float dot)
        {
            return (dot + 1f) * 0.5f;
        }

        private void TryPushBack()
        {
            Vector3 forward = MotionForward;

            if (!Probe(transform.position, forward, pushDistance, out RaycastHit hit))
            {
                return;
            }

            float distanceRatio = (pushDistance - hit.distance) / pushDistance;

            // 前進速度を超えて押し戻すと、向きを変えられないまま後退し続けて固着する。
            // 前進を打ち消すところまでは許し、逆行はさせない
            float pushSpeed = Mathf.Min(pushForce * distanceRatio, currentSpeed);

            transform.position -= forward * pushSpeed * Time.deltaTime;
        }

        #region Debug Gizmos

#if UNITY_EDITOR
        // 文脈情報は淡く、異常・注目対象は濃く、という濃度差で読み取れるようにする
        private static readonly Color ColorRoamArea = new Color(0.35f, 0.60f, 0.85f, 0.30f);
        private static readonly Color ColorWaypoint = new Color(0.35f, 0.95f, 0.45f, 0.90f);
        private static readonly Color ColorArrival = new Color(0.35f, 0.95f, 0.45f, 0.35f);
        private static readonly Color ColorRayClear = new Color(0.30f, 0.65f, 0.60f, 0.45f);
        private static readonly Color ColorRayHit = new Color(1.00f, 0.25f, 0.20f, 1.00f);
        private static readonly Color ColorPush = new Color(0.90f, 0.35f, 0.90f, 0.70f);
        private static readonly Color ColorTrail = new Color(1.00f, 0.85f, 0.30f, 0.90f);
        private static readonly Color ColorTurnOk = new Color(0.40f, 0.80f, 1.00f, 0.55f);
        private static readonly Color ColorWarning = new Color(1.00f, 0.45f, 0.10f, 0.95f);
        private static readonly Color ColorStuck = new Color(1.00f, 0.75f, 0.15f, 0.65f);
        private static readonly Color ColorChosen = new Color(0.45f, 1.00f, 0.95f, 1.00f);
        private static readonly Color ColorSeparation = new Color(0.80f, 0.45f, 1.00f, 0.30f);
        private static readonly Color ColorMigration = new Color(0.40f, 0.90f, 0.70f, 0.25f);

        private const float TrailSampleInterval = 0.05f;
        private const float MarkerRadius = 0.08f;

        private static GUIStyle labelStyle;
        private static Texture2D labelBackground;

        private readonly Queue<Vector3> trail = new Queue<Vector3>();
        private float trailTimer;

        private void RecordTrail()
        {
            if (!showGizmos || !showTrail)
            {
                if (trail.Count > 0) trail.Clear();
                return;
            }

            trailTimer += Time.deltaTime;
            if (trailTimer < TrailSampleInterval) return;
            trailTimer = 0f;

            trail.Enqueue(transform.position);

            int maxSamples = Mathf.Max(2, Mathf.CeilToInt(trailSeconds / TrailSampleInterval));
            while (trail.Count > maxSamples)
            {
                trail.Dequeue();
            }
        }

        private void OnDrawGizmos()
        {
            if (!showGizmos) return;

            bool selected = IsSelectedInHierarchy();
            if (!selected && !alwaysShowGizmos) return;

            Vector3 position = transform.position;

            // 再生前は Start がまだ走っていないので、実行時に決まる中心を先読みして描く
            Vector3 center = Application.isPlaying || !roamCenterFromSpawnPosition ? roamCenter : position;

            if (showRoamArea) DrawRoamArea(center);
            if (showTrail) DrawTrail();

            // 停止中・旋回中は destination が古い値なので描かない
            if (showWaypoint && Application.isPlaying && IsMoving) DrawWaypoint(position);

            // 密度の高い情報は選択時のみ。大量配置したシーンが埋まらないようにする
            if (!selected) return;

            if (showReachability) DrawReachability(position);
            if (showAvoidanceRays) DrawAvoidanceRays(position);
            if (showSeparation) DrawSeparation(position);
            if (showLabels) DrawLabels(position);
        }

        private bool IsSelectedInHierarchy()
        {
            // ルートを選択した状態で子の Swimmer を見たいことがあるため祖先も辿る
            Transform current = Selection.activeTransform;
            while (current != null)
            {
                if (current == transform) return true;
                current = current.parent;
            }
            return false;
        }

        private void DrawRoamArea(Vector3 center)
        {
            // roamArea は Random.Range(-x, x) で使う「半径」なので、辺の長さは 2 倍になる
            Gizmos.color = ColorRoamArea;
            Gizmos.DrawWireCube(center, roamArea * 2f);
            Gizmos.DrawLine(center - Vector3.up * 0.2f, center + Vector3.up * 0.2f);

            if (!useMigration) return;

            // 縄張りが動ける範囲と、いま向かっている先
            Vector3 home = Application.isPlaying ? homeCenter : center;

            Gizmos.color = ColorMigration;
            Gizmos.DrawWireCube(home, migrationRange * 2f);

            if (!Application.isPlaying) return;

            Gizmos.DrawLine(center, migrationTarget);
            Gizmos.DrawWireSphere(migrationTarget, MarkerRadius * 2f);
        }

        private void DrawTrail()
        {
            if (trail.Count < 2) return;

            int index = 0;
            int count = trail.Count;
            Vector3 previous = Vector3.zero;

            foreach (Vector3 point in trail)
            {
                if (index > 0)
                {
                    // 古いほど薄くして進行方向が読めるようにする
                    float age = (float)index / count;
                    Gizmos.color = new Color(ColorTrail.r, ColorTrail.g, ColorTrail.b, ColorTrail.a * age);
                    Gizmos.DrawLine(previous, point);
                }

                previous = point;
                index++;
            }
        }

        private void DrawWaypoint(Vector3 position)
        {
            Gizmos.color = ColorWaypoint;
            Gizmos.DrawLine(position, destination);
            Gizmos.DrawWireSphere(destination, MarkerRadius * 2f);

            // 抽選時に空きを要求した半径
            Gizmos.color = new Color(ColorWaypoint.r, ColorWaypoint.g, ColorWaypoint.b, 0.25f);
            Gizmos.DrawWireSphere(destination, waypointClearance);

            // 到達判定の球
            Gizmos.color = ColorArrival;
            Gizmos.DrawWireSphere(destination, waypointDistance);

            // 進捗が止まっている間に広がっている分
            float tolerance = waypointDistance + stuckTime * stuckToleranceGrowth;
            if (tolerance > waypointDistance + 0.01f)
            {
                Gizmos.color = ColorStuck;
                Gizmos.DrawWireSphere(destination, tolerance);
            }
        }

        private void DrawReachability(Vector3 position)
        {
            if (rotationSpeed <= Mathf.Epsilon) return;

            // Slerp による比例制御の角速度は k*θ なので、旋回半径の上限は 速度 / 旋回速度。
            // 区間ごとに値を引き直すため、最も曲がれない組み合わせで評価する
            float turnRadius = WorstCaseTurnRadius();
            if (float.IsInfinity(turnRadius)) return;

            bool unreachable = turnRadius > waypointDistance;

            Vector3 ringCenter = Application.isPlaying ? destination : position;

            Handles.color = unreachable ? ColorWarning : ColorTurnOk;
            Handles.DrawWireDisc(ringCenter, Vector3.up, turnRadius);

            if (unreachable)
            {
                // 到達不能帯（waypointDistance 〜 turnRadius）を放射線でハッチングして帯だと分かるようにする
                Handles.color = new Color(ColorWarning.r, ColorWarning.g, ColorWarning.b, 0.35f);
                for (int i = 0; i < 12; i++)
                {
                    float rad = i * Mathf.PI * 2f / 12f;
                    Vector3 dir = new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad));
                    Handles.DrawLine(ringCenter + dir * waypointDistance, ringCenter + dir * turnRadius);
                }
            }

            // 自機が今この場で切れる最小旋回円（右旋回側）
            Vector3 flatRight = Vector3.ProjectOnPlane(transform.right, Vector3.up);
            if (flatRight.sqrMagnitude > 0.0001f)
            {
                Handles.color = new Color(ColorTurnOk.r, ColorTurnOk.g, ColorTurnOk.b, 0.30f);
                Handles.DrawWireDisc(position + flatRight.normalized * turnRadius, Vector3.up, turnRadius);
            }
        }

        private void DrawAvoidanceRays(Vector3 position)
        {
            // 押し戻しは回避とは独立に毎フレーム走るので、回避が無効でも表示する
            Gizmos.color = ColorPush;
            Gizmos.DrawRay(position, MotionForward * pushDistance);

            if (!useAvoidance) return;

            Vector3 forward = MotionForward;

            // 扇状スキャンを起動するかを決める門番プローブ
            bool blocked = Probe(position, forward, avoidDistance, out RaycastHit blocker);

            Gizmos.color = blocked ? ColorRayHit : ColorRayClear;
            Gizmos.DrawRay(position, forward * avoidDistance);
            Gizmos.DrawWireSphere(position + forward * avoidDistance, avoidProbeRadius);

            if (!blocked)
            {
                // 空いている間は候補探索そのものが走らない（1 クエリのみ）
                return;
            }

            Gizmos.DrawWireSphere(blocker.point, MarkerRadius);
            Gizmos.color = ColorWarning;
            Gizmos.DrawRay(blocker.point, blocker.normal * 0.4f);

            Vector3 desired = Application.isPlaying ? destination - position : forward;
            desired = desired.sqrMagnitude > 0.0001f ? desired.normalized : forward;

            // 候補方向を空き具合で着色する。赤いほど詰まっている
            for (int i = 0; i < ProbeDirections.Length + DerivedProbeCount; i++)
            {
                Vector3 direction = GetProbeDirection(i, desired, blocker.normal);
                if (direction.sqrMagnitude < 0.0001f) continue;

                direction.Normalize();
                float clearance = MeasureClearance(position, direction);

                Gizmos.color = Color.Lerp(ColorRayHit, ColorRayClear, clearance);
                Gizmos.DrawRay(position, direction * avoidDistance * clearance);
            }

            // 実際に採用された方向（前フレームの結果）
            if (lastAvoidDirection.sqrMagnitude < 0.0001f) return;

            Gizmos.color = ColorChosen;
            Gizmos.DrawRay(position, lastAvoidDirection.normalized * avoidDistance * 1.3f);
            Gizmos.DrawWireSphere(position + lastAvoidDirection.normalized * avoidDistance * 1.3f, MarkerRadius * 1.5f);
        }

        private void DrawSeparation(Vector3 position)
        {
            if (!useSeparation) return;

            Gizmos.color = ColorSeparation;
            Gizmos.DrawWireSphere(position, separationRadius);

            if (!Application.isPlaying) return;

            // 検出した相手を線で結ぶ。誰に反応しているのかを見えるようにする
            int count = Physics.OverlapSphereNonAlloc(position, separationRadius, neighbourBuffer,
                separationMask, QueryTriggerInteraction.Ignore);

            for (int i = 0; i < count; i++)
            {
                Collider neighbour = neighbourBuffer[i];
                if (neighbour.transform == transform) continue;
                if (!neighbour.TryGetComponent(out BaseSwimmer other) || other == this) continue;

                Gizmos.DrawLine(position, neighbour.transform.position);
            }

            if (separationForce.sqrMagnitude < 0.0001f) return;

            // 実際に効いている分離力
            Gizmos.color = ColorChosen;
            Gizmos.DrawRay(position, separationForce);
        }

        private void DrawLabels(Vector3 position)
        {
            float turnRadius = WorstCaseTurnRadius();
            bool canTurn = !float.IsInfinity(turnRadius);

            string text = $"<b>{name}</b>";

            if (Application.isPlaying)
            {
                float tolerance = waypointDistance + stuckTime * stuckToleranceGrowth;

                // 区間ごとの抽選結果が見えるよう、実速度・目標・上限を並べる
                text += $"\n{mode}　速度 {currentSpeed:F2} → {targetSpeed:F2}（上限 {moveSpeed:F2}）";
                text += $"\n旋回 {currentTurnSpeed:F2}（上限 {rotationSpeed:F2}）";

                if (IsMoving)
                {
                    text += $"\n目標まで {Vector3.Distance(position, destination):F2}m（到達 {tolerance:F2}m）";
                }

                if (lastAvoidDirection.sqrMagnitude > 0.0001f)
                {
                    float deviation = Vector3.Angle(MotionForward, lastAvoidDirection);
                    text += $"\n<color=#70FFF0>回避中 偏角 {deviation:F0}°</color>";
                }

                if (stuckTime > 0.5f)
                {
                    text += $"\n<color=#FFC030>停滞 {stuckTime:F1}s / 最接近 {bestDistanceToWaypoint:F2}m</color>";
                }
            }

            if (!canTurn)
            {
                text += "\n<color=#FF8844><b>rotationSpeed が 0 で旋回不能</b></color>";
            }
            else
            {
                text += $"\n旋回半径 最悪 {turnRadius:F2}m";

                if (turnRadius > waypointDistance)
                {
                    text += $"\n<color=#FF8844><b>到達不能帯 {waypointDistance:F2}〜{turnRadius:F2}m</b></color>";
                    text += $"\n<color=#FF8844>要 waypointDistance > {turnRadius:F2}</color>";
                }
            }

            Handles.Label(position + Vector3.up * labelHeightOffset, text, GetLabelStyle());
        }

        private static GUIStyle GetLabelStyle()
        {
            if (labelBackground == null)
            {
                labelBackground = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
                labelBackground.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.78f));
                labelBackground.Apply();
                labelStyle = null;
            }

            if (labelStyle == null)
            {
                labelStyle = new GUIStyle
                {
                    fontSize = 11,
                    richText = true,
                    alignment = TextAnchor.UpperLeft,
                    padding = new RectOffset(6, 6, 4, 4),
                };
                labelStyle.normal.textColor = Color.white;
                labelStyle.normal.background = labelBackground;
            }

            return labelStyle;
        }
#endif

        #endregion
    }
}
