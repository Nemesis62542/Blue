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
        [SerializeField] private float avoidStrength = 75.0f;
        [SerializeField] private LayerMask avoidanceMask = ~0;

        [Header("Push Settings")]
        [SerializeField] private float pushDistance = 1.0f;
        [SerializeField] private float pushForce = 2.0f;

        [Header("Roaming Settings")]
        [SerializeField] private Vector3 roamCenter = Vector3.zero;
        [SerializeField] private Vector2 roamArea = new Vector2(5f, 5f);

        private Rigidbody rb;
        private Vector3 targetWaypoint;
        private State state = State.None;
        
        public State State => state;

        private void Start()
        {
            rb = GetComponent<Rigidbody>();
            rb.constraints = RigidbodyConstraints.FreezeRotation;

            roamCenter = transform.position;
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
            bool hasGroundBelow = Physics.Raycast(rb.position, -transform.up, out RaycastHit hitBelow, groundCheckDistance, groundMask);

            // 前方の地面情報
            bool hasGroundForward = Physics.Raycast(rb.position, transform.forward, out RaycastHit hitForward, groundCheckDistance, groundMask);

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

            Quaternion targetRotation = Quaternion.LookRotation(direction, transform.up);

            // 障害物回避の適用
            if (useAvoidance)
            {
                targetRotation = ApplyObstacleAvoidance(targetRotation);
            }

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

        private Quaternion ApplyObstacleAvoidance(Quaternion rotation)
        {
            Vector3 forward = transform.forward;
            Vector3 right = transform.right;

            // 正面の障害物
            if (Physics.Raycast(transform.position, forward, out RaycastHit hit, avoidDistance, avoidanceMask))
            {
                Vector3 avoidDir = Vector3.Reflect(forward, hit.normal);
                avoidDir = Vector3.ProjectOnPlane(avoidDir, transform.up).normalized;
                return Quaternion.LookRotation(avoidDir, transform.up);
            }

            // 右前の障害物
            if (Physics.Raycast(transform.position, forward + right * 0.35f, out hit, avoidDistance, avoidanceMask))
            {
                Vector3 euler = rotation.eulerAngles;
                euler.y -= avoidStrength * Time.fixedDeltaTime;
                return Quaternion.Euler(euler);
            }

            // 左前の障害物
            if (Physics.Raycast(transform.position, forward - right * 0.35f, out hit, avoidDistance, avoidanceMask))
            {
                Vector3 euler = rotation.eulerAngles;
                euler.y += avoidStrength * Time.fixedDeltaTime;
                return Quaternion.Euler(euler);
            }

            return rotation;
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
            if (Physics.Raycast(rb.position, -transform.up, out RaycastHit hit, groundCheckDistance, groundMask))
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

            // 目標速度
            Vector3 targetVelocity = transform.forward * moveSpeed;

            // 差分を力として加える
            Vector3 velocityDiff = targetVelocity - currentForwardVelocity;
            rb.AddForce(velocityDiff * moveForce, ForceMode.Force);
        }

        private void HandlePushBack()
        {
            if (Physics.Raycast(rb.position, transform.forward, out RaycastHit hit, pushDistance, avoidanceMask))
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
                if (Physics.SphereCast(candidateWaypoint + Vector3.up * 10f, groundCheckRadius, Vector3.down, out RaycastHit hit, 20f, groundMask))
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
