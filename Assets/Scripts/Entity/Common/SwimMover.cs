using System;
using UnityEngine;

namespace Blue.Entity.Common
{
    [System.Serializable]
    public class SwimMover
    {
        [SerializeField] private Transform targetTransform;
        [SerializeField] private float moveSpeed = 3.0f;
        [SerializeField] private float arrivalDistance = 0.5f;
        [SerializeField] private float slowDownDistance = 2.0f;

        private Vector3 destination;
        private Action onMoveComplete;
        private bool isMoving = false;

        public Vector3 Destination => destination;
        public bool IsMoving => isMoving;

        public void Initialize(Transform target)
        {
            targetTransform = target;
        }

        public void MoveTo(Vector3 new_destination, Action on_complete = null)
        {
            destination = new_destination;
            isMoving = true;
            onMoveComplete = on_complete;
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

            Vector3 current_position = targetTransform.position;
            Vector3 direction = (destination - current_position).normalized;
            float distance_to_destination = Vector3.Distance(current_position, destination);
            if (distance_to_destination <= arrivalDistance)
            {
                Stop();
                onMoveComplete?.Invoke();
                onMoveComplete = null;
                return;
            }

            float speed_rate = 1.0f;
            if (distance_to_destination < slowDownDistance)
            {
                float ratio = distance_to_destination / slowDownDistance;
                speed_rate = Mathf.Clamp01(ratio * ratio);
            }
            float current_speed = moveSpeed * speed_rate;

            Vector3 move = direction * current_speed * Time.deltaTime;
            targetTransform.position += move;
        }

        public void MoveToRandomPosition(Vector3 center, float radius, Action on_complete = null)
        {
            Vector3 destination = GetRandomDestination(center, radius);
            MoveTo(destination, on_complete);
        }

        private Vector3 GetRandomDestination(Vector3 center, float radius)
        {
            Vector2 random_circle = UnityEngine.Random.insideUnitCircle * radius;
            Vector3 random_offset = new Vector3(random_circle.x, 0f, random_circle.y);
            return center + random_offset;
        }
    }
}