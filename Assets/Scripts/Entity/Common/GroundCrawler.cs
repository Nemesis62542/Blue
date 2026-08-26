using UnityEngine;

namespace Blue.Entity.Common
{
    public enum State
    {
        None,    // なし
        Idle,    // 待機中
        Move,    // 移動中
        Arrival  // 目的地に到着
    }

    [RequireComponent(typeof(Rigidbody))]
    public class GroundCrawler : MonoBehaviour
    {
        [Header("Crawl Settings")]
        [SerializeField] private float moveSpeed = 2.0f;
        [SerializeField] private float moveForce = 10.0f;
        [SerializeField] private float rotationSpeed = 5.0f;
        [SerializeField] private float waypointDistance = 1.0f;

        [Header("Ground Detection")]
        [SerializeField] private float groundCheckDistance = 2.0f;
        [SerializeField] private float groundCheckRadius = 0.5f;
        [SerializeField] private float heightOffset = 0.1f;
        [SerializeField] private float groundAlignForce = 10.0f;
        [SerializeField] private LayerMask groundMask = ~0;

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

        [Header("Roaming Settings")]
        [SerializeField] private bool roamCenterFromSpawnPosition = true;
        [SerializeField] private Vector3 roamCenter = Vector3.zero;
        [SerializeField] private Vector2 roamArea = new Vector2(5f, 5f);

        // 地面に沿った候補方向の角度（前方からのオフセット）。
        // 遊泳と違い上下に逃げられないので、扇は水平面だけに張る
        private static readonly float[] ProbeAngles =
        {
            0f, 25f, -25f, 50f, -50f, 75f, -75f, 105f, -105f, 135f, -135f
        };

        private Rigidbody rb;
        private Vector3 targetWaypoint;
        private State state = State.None;
        private Vector3 lastAvoidDirection;
        private float avoidSpeedFactor = 1f;
        private bool roamCenterOverridden;

        public State State => state;

        private void Start()
        {
            rb = GetComponent<Rigidbody>();
            rb.constraints = RigidbodyConstraints.FreezeRotation;

            // Instantiate 直後に SetRoamCenter で縄張りを指定された場合は、そちらを優先する
            if (roamCenterFromSpawnPosition && !roamCenterOverridden)
            {
                roamCenter = transform.position;
            }

            state = State.Idle;
        }

        private void FixedUpdate()
        {
            HandleRotation();
            HandleMovement();
            HandlePushBack();
        }

        #region Rotation
        private void HandleRotation()
        {
            // 地面への姿勢調整
            RotateToAlignWithGround();

            // 進行方向への回転
            if (state == State.Move) RotateTowardsTarget();
        }

        private void RotateToAlignWithGround()
        {
            // 足元の地面情報
            bool hasGroundBelow = Physics.Raycast(rb.position, -transform.up, out RaycastHit hitBelow, groundCheckDistance, groundMask, QueryTriggerInteraction.Ignore);

            // 前方の地面情報
            bool hasGroundForward = Physics.Raycast(rb.position, transform.forward, out RaycastHit hitForward, groundCheckDistance, groundMask, QueryTriggerInteraction.Ignore);

            Vector3 targetNormal = Vector3.up;

            if (hasGroundBelow && hasGroundForward)
            {
                if (hitForward.distance <= hitBelow.distance)
                {
                    targetNormal = hitForward.normal;
                }
                else
                {
                    targetNormal = hitBelow.normal;
                }
            }
            else if (hasGroundBelow)
            {
                targetNormal = hitBelow.normal;
            }
            else if (hasGroundForward)
            {
                targetNormal = hitForward.normal;
            }

            // 地面の法線に沿って姿勢を調整
            Quaternion targetRotation = Quaternion.FromToRotation(transform.up, targetNormal) * transform.rotation;
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime));
        }

        private void RotateTowardsTarget()
        {
            Vector3 direction = GetTargetDirection();
            if (direction == Vector3.zero) return;

            // 回避は目標回転を補正するのではなく、進みたい方向そのものを差し替える。
            // 補正を目標回転に足すと Slerp で二重に減衰し、fixedDeltaTime^2 に比例して消える
            direction = EvaluateAvoidance(direction, out avoidSpeedFactor);

            Quaternion targetRotation = Quaternion.LookRotation(direction, transform.up);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime));
        }

        private Vector3 GetTargetDirection()
        {
            if (Vector3.Distance(rb.position, targetWaypoint) < waypointDistance)
            {
                state = State.Arrival;
            }

            // 目標への方向（水平面に投影）
            Vector3 direction = targetWaypoint - rb.position;
            direction.y = 0; // 水平方向のみ
            direction = direction.normalized;

            return direction;
        }

        // 進みたい方向に対し、実際に進める方向を返す。あわせて速度倍率を出す。
        // 単発 if の優先順位で 1 つの補正を選ぶのではなく、全候補を採点して選ぶ
        private Vector3 EvaluateAvoidance(Vector3 desired, out float speedFactor)
        {
            speedFactor = 1f;
            if (!useAvoidance) return desired;

            Vector3 origin = rb.position;
            Vector3 up = transform.up;

            // 前方が空いていれば探索しない。通常移動時のコストを 1 クエリに抑えるための門番
            if (!Physics.SphereCast(origin, avoidProbeRadius, transform.forward, out RaycastHit blocker,
                    avoidDistance, avoidanceMask, QueryTriggerInteraction.Ignore))
            {
                lastAvoidDirection = Vector3.zero;
                return desired;
            }

            // 障害物が近いほど減速する。毎フレーム倍率として掛け直すので上書きで消えない
            float blockage = 1f - Mathf.Clamp01(blocker.distance / avoidDistance);
            speedFactor = Mathf.Lerp(1f, avoidMinSpeedFactor, blockage);

            float bestScore = float.NegativeInfinity;
            Vector3 bestDirection = desired;

            for (int i = 0; i <= ProbeAngles.Length; i++)
            {
                // 最後の 1 本は衝突面に沿って滑る方向。壁は跳ね返るのではなく沿うのが自然
                Vector3 direction = i < ProbeAngles.Length
                    ? Quaternion.AngleAxis(ProbeAngles[i], up) * transform.forward
                    : Vector3.ProjectOnPlane(desired, blocker.normal);

                direction = Vector3.ProjectOnPlane(direction, up);
                if (direction.sqrMagnitude < 0.0001f) continue;

                direction.Normalize();

                float score = MeasureClearance(origin, direction)
                              + avoidAlignmentWeight * ToUnitRange(Vector3.Dot(direction, desired))
                              + avoidHysteresis * ToUnitRange(Vector3.Dot(direction, lastAvoidDirection));

                if (score <= bestScore) continue;

                bestScore = score;
                bestDirection = direction;
            }

            lastAvoidDirection = bestDirection;
            return bestDirection;
        }

        private float MeasureClearance(Vector3 origin, Vector3 direction)
        {
            if (Physics.SphereCast(origin, avoidProbeRadius, direction, out RaycastHit hit,
                    avoidDistance, avoidanceMask, QueryTriggerInteraction.Ignore))
            {
                return Mathf.Clamp01(hit.distance / avoidDistance);
            }

            return 1f;
        }

        // 内積 [-1,1] を重み付けに使える [0,1] へ
        private static float ToUnitRange(float dot)
        {
            return (dot + 1f) * 0.5f;
        }
        #endregion

        #region Movement
        private void HandleMovement()
        {
            // 地面への張り付き
            ApplyGroundAttachment();

            // 前方への移動
            if (state == State.Move) ApplyForwardMovement();
        }

        private void ApplyGroundAttachment()
        {
            if (Physics.Raycast(rb.position, -transform.up, out RaycastHit hit, groundCheckDistance, groundMask, QueryTriggerInteraction.Ignore))
            {
                // 地面からの高さを維持する力
                Vector3 targetPosition = hit.point + hit.normal * heightOffset;
                Vector3 heightForce = (targetPosition - rb.position) * groundAlignForce;
                rb.AddForce(heightForce, ForceMode.Force);
            }
        }

        private void ApplyForwardMovement()
        {
            // 現在の前方速度
            Vector3 currentForwardVelocity = Vector3.Project(rb.linearVelocity, transform.forward);

            // 目標速度（障害物が近いほど落とす）
            Vector3 targetVelocity = transform.forward * (moveSpeed * avoidSpeedFactor);

            // 差分を力として加える
            Vector3 velocityDiff = targetVelocity - currentForwardVelocity;
            rb.AddForce(velocityDiff * moveForce, ForceMode.Force);
        }

        private void HandlePushBack()
        {
            if (Physics.SphereCast(rb.position, avoidProbeRadius, transform.forward, out RaycastHit hit,
                    pushDistance, avoidanceMask, QueryTriggerInteraction.Ignore))
            {
                float distanceRatio = (pushDistance - hit.distance) / pushDistance;
                Vector3 pushBackForce = -transform.forward * pushForce * distanceRatio;
                rb.AddForce(pushBackForce, ForceMode.VelocityChange);
            }
        }
        #endregion

        #region Waypoint
        public void SetRandomWaypoint()
        {
            int maxAttempts = 10;
            for (int i = 0; i < maxAttempts; i++)
            {
                Vector3 offset = new Vector3(
                    Random.Range(-roamArea.x, roamArea.x),
                    0f,
                    Random.Range(-roamArea.y, roamArea.y)
                );
                Vector3 candidateWaypoint = roamCenter + offset;

                // 地面の高さを検出してwaypointを地面上に配置
                if (Physics.SphereCast(candidateWaypoint + Vector3.up * 10f, groundCheckRadius, Vector3.down, out RaycastHit hit, 20f, groundMask, QueryTriggerInteraction.Ignore))
                {
                    SetWaypoint(hit.point + hit.normal * heightOffset);
                    return;
                }
            }

            // 最大試行回数を超えた場合は現在地を目標に設定
            Debug.LogWarning($"[GroundCrawler] Failed to find valid waypoint after {maxAttempts} attempts. Using current position.");
            SetWaypoint(rb.position);
        }

        public void SetWaypoint(Vector3 point)
        {
            targetWaypoint = point;

            state = State.Move; 
        }

        public void SetRoamCenter(Vector3 center)
        {
            roamCenter = center;
            roamCenterOverridden = true;
        }

        public void ForceNewWaypoint()
        {
            SetRandomWaypoint();
        }
        #endregion

        #region Gizmos
        // private void OnDrawGizmosSelected()
        // {
        //     if (!Application.isPlaying) return;

        //     // 足元の地面検出（Raycast）
        //     Gizmos.color = Color.yellow;
        //     Vector3 belowStart = rb.position;
        //     Vector3 belowEnd = belowStart + (-transform.up * groundCheckDistance);
        //     Gizmos.DrawLine(belowStart, belowEnd);
        //     if (Physics.Raycast(rb.position, -transform.up, out RaycastHit hitBelow, groundCheckDistance, groundMask))
        //     {
        //         Gizmos.DrawWireSphere(hitBelow.point, 0.2f);
        //         Gizmos.DrawRay(hitBelow.point, hitBelow.normal * 0.5f);
        //     }

        //     // 前方の地面検出（Raycast）
        //     Gizmos.color = Color.cyan;
        //     Vector3 vec = rb.transform.position + transform.forward * forwardGroundThreshold;
        //     Gizmos.DrawRay(vec, transform.forward * groundCheckDistance);
        //     if (Physics.Raycast(vec, transform.forward, out RaycastHit hitForward, groundCheckDistance, groundMask))
        //     {
        //         Gizmos.DrawWireSphere(hitForward.point, 0.2f);
        //         Gizmos.DrawRay(hitForward.point, hitForward.normal * 0.5f);
        //     }

        //     // 目標地点
        //     Gizmos.color = Color.green;
        //     Gizmos.DrawWireSphere(targetWaypoint, 0.3f);
        //     Gizmos.DrawLine(transform.position, targetWaypoint);

        //     // 障害物回避のRay
        //     Gizmos.color = Color.red;
        //     Gizmos.DrawRay(transform.position, transform.forward * avoidDistance);
        //     Gizmos.DrawRay(transform.position, (transform.forward + transform.right * 0.35f).normalized * avoidDistance);
        //     Gizmos.DrawRay(transform.position, (transform.forward - transform.right * 0.35f).normalized * avoidDistance);

        //     // プッシュバック距離
        //     Gizmos.color = Color.magenta;
        //     Gizmos.DrawRay(rb.position, transform.forward * pushDistance);
        //     Gizmos.DrawWireSphere(transform.position + transform.forward * pushDistance, 0.2f);

        //     // ローミングエリア
        //     Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        //     Vector3 roamMin = roamCenter - new Vector3(roamArea.x, 0, roamArea.y);
        //     Vector3 roamMax = roamCenter + new Vector3(roamArea.x, 0, roamArea.y);
        //     Gizmos.DrawLine(roamMin, new Vector3(roamMax.x, roamMin.y, roamMin.z));
        //     Gizmos.DrawLine(new Vector3(roamMax.x, roamMin.y, roamMin.z), roamMax);
        //     Gizmos.DrawLine(roamMax, new Vector3(roamMin.x, roamMin.y, roamMax.z));
        //     Gizmos.DrawLine(new Vector3(roamMin.x, roamMin.y, roamMax.z), roamMin);
        // }
        #endregion
    }
}
