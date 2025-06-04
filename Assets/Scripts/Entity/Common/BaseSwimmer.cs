using UnityEngine;

namespace Blue.Entity.Common
{
    public class BaseSwimmer : MonoBehaviour
    {
        [Header("Swim Settings")]
        [SerializeField] private float moveSpeed = 3.0f;
        [SerializeField] private float rotationSpeed = 5.0f;
        [SerializeField] private float waypointDistance = 1.0f;

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
        [SerializeField] private Vector3 roamArea = new Vector3(5f, 2f, 5f);

        private Vector3 targetWaypoint;
        private float currentSpeed;
        private float tParam = 0f;

        protected virtual void Start()
        {
            roamCenter = transform.position;
            SetRandomWaypoint();
        }

        protected virtual void Update()
        {
            Swim();
            TryPushBack();
        }

        private void Swim()
        {
            if (Vector3.Distance(transform.position, targetWaypoint) < waypointDistance)
            {
                SetRandomWaypoint();
            }

            Vector3 direction = (targetWaypoint - transform.position).normalized;

            Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
            bool avoided = ApplyObstacleAvoidance(ref targetRotation);

            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

            currentSpeed = Mathf.Lerp(currentSpeed, moveSpeed, tParam);
            tParam += Time.deltaTime;

            transform.position += transform.forward * currentSpeed * Time.deltaTime;
        }

        private void SetRandomWaypoint()
        {
            tParam = 0f;
            Vector3 offset = new Vector3(
                Random.Range(-roamArea.x, roamArea.x),
                Random.Range(-roamArea.y, roamArea.y),
                Random.Range(-roamArea.z, roamArea.z)
            );
            targetWaypoint = roamCenter + offset;
        }

        private bool ApplyObstacleAvoidance(ref Quaternion rotation)
        {
            if (!useAvoidance) return false;

            RaycastHit hit;
            Vector3 forward = transform.forward;
            Vector3 right = transform.right;
            Vector3 up = Vector3.up;
            Vector3 down = Vector3.down;

            if (Physics.Raycast(transform.position, down + forward * 0.1f, out hit, avoidDistance, avoidanceMask))
            {
                Vector3 euler = rotation.eulerAngles;
                euler.x += avoidStrength * Time.deltaTime;
                rotation = Quaternion.Euler(euler);
                return true;
            }
            if (Physics.Raycast(transform.position, up + forward * 0.1f, out hit, avoidDistance, avoidanceMask))
            {
                Vector3 euler = rotation.eulerAngles;
                euler.x -= avoidStrength * Time.deltaTime;
                rotation = Quaternion.Euler(euler);
                return true;
            }

            if (Physics.Raycast(transform.position, forward, out hit, avoidDistance, avoidanceMask))
            {
                Vector3 avoidDir = Vector3.Reflect(forward, hit.normal);
                rotation = Quaternion.LookRotation(avoidDir);
                currentSpeed = Mathf.Max(currentSpeed * 0.5f, 0.01f);
                return true;
            }

            if (Physics.Raycast(transform.position, forward + right * 0.35f, out hit, avoidDistance, avoidanceMask))
            {
                rotation = Quaternion.Euler(0, transform.eulerAngles.y - avoidStrength, 0);
                return true;
            }

            if (Physics.Raycast(transform.position, forward - right * 0.35f, out hit, avoidDistance, avoidanceMask))
            {
                rotation = Quaternion.Euler(0, transform.eulerAngles.y + avoidStrength, 0);
                return true;
            }

            return false;
        }

        private void TryPushBack()
        {
            RaycastHit hit;
            Vector3 forward = transform.forward;

            if (Physics.Raycast(transform.position, forward, out hit, pushDistance, avoidanceMask))
            {
                float distanceRatio = (pushDistance - hit.distance) / pushDistance;
                transform.position -= forward * pushForce * distanceRatio * Time.deltaTime;
            }
        }

        public void SetRoamCenter(Vector3 center)
        {
            roamCenter = center;
        }

        public void ForceNewWaypoint()
        {
            SetRandomWaypoint();
        }
    }
}