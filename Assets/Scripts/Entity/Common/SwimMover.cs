using UnityEngine;

namespace Blue.Entity.Common
{
    [System.Serializable]
    public class SwimMover
    {
        [SerializeField] private Transform targetTransform; // 移動対象（生物のTransform）
        [SerializeField] private float moveSpeed = 3.0f;      // 泳ぐ速度
        [SerializeField] private float arrivalDistance = 0.5f; // 到達とみなす距離

        private Vector3 destination;
        private bool isMoving = false;

        public bool IsMoving => isMoving;

        public void Initialize(Transform target)
        {
            targetTransform = target;
        }

        public void MoveTo(Vector3 new_destination)
        {
            destination = new_destination;
            isMoving = true;
        }

        public void Stop()
        {
            isMoving = false;
        }

        public void UpdateMove()
        {
            if (!isMoving || targetTransform == null)
            {
                return;
            }

            Vector3 direction = (destination - targetTransform.position).normalized;
            Vector3 move = direction * moveSpeed * Time.deltaTime;
            targetTransform.position += move;

            float distance_to_destination = Vector3.Distance(targetTransform.position, destination);
            if (distance_to_destination <= arrivalDistance)
            {
                Stop();
            }
        }
    }
}