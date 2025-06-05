using System;
using Blue.Entity.Common;
using UnityEngine;

namespace Blue.Entity
{
    public class CuttleFishSwimmer : BaseSwimmer
    {
        [Header("CuttleFish Swim Settings")]
        [SerializeField] private float threatRotateSpeed = 5f;
        [SerializeField] private float inkEscapeDistance = 5f;
        [SerializeField] private float escapeDuration = 2.5f;
        [SerializeField] private float escapeMoveSpeed = 3.0f;
        [SerializeField] private bool rotateAwayFromThreat = true; // 5. 威嚇時の回転方向

        [Header("Idle Swim Settings")]
        [SerializeField] private float minPauseTime = 2f;
        [SerializeField] private float maxPauseTime = 5f;
        [SerializeField] private float minSwimTime = 3f;
        [SerializeField] private float maxSwimTime = 6f;

        public Action<bool> OnSwimStateChanged;

        private Transform threatTarget;
        private bool isIntimidating = false;
        private bool isEscaping = false;
        private Vector3 escapeDestination;
        private Action onEscapeComplete;

        private float escapeTimer = 0f;
        private float stateTimer = 0f;
        private float stateDuration = 0f;
        private bool isPaused = false;

        protected override void Start()
        {
            base.Start();
            isPaused = true;
            stateTimer = 0f;
            stateDuration = UnityEngine.Random.Range(minPauseTime, maxPauseTime);
            OnSwimStateChanged?.Invoke(false);
        }

        protected override void Update()
        {
            bool callBaseUpdate = false;

            if (isEscaping)
            {
                EscapeMove();
                callBaseUpdate = true;
            }
            else if (isIntimidating && threatTarget != null)
            {
                RotateTowardThreat();
            }
            else
            {
                callBaseUpdate = UpdateDimSwim();
            }

            if (callBaseUpdate)
            {
                base.Update();
            }
        }

        private bool UpdateDimSwim()
        {
            stateTimer += Time.deltaTime;

            if (stateTimer >= stateDuration)
            {
                isPaused = !isPaused;
                stateTimer = 0f;
                stateDuration = isPaused ? UnityEngine.Random.Range(minPauseTime, maxPauseTime) : UnityEngine.Random.Range(minSwimTime, maxSwimTime);

                if (!isPaused)
                {
                    SetRandomWaypoint();
                }

                OnSwimStateChanged?.Invoke(!isPaused);
            }

            return !isPaused;
        }

        private void RotateTowardThreat()
        {
            if (threatTarget == null) return;

            Vector3 targetPos = threatTarget.position;
            targetPos.y = transform.position.y;
            Vector3 dir = rotateAwayFromThreat
                ? (transform.position - targetPos).normalized
                : (targetPos - transform.position).normalized;

            if (dir.sqrMagnitude > 0.01f)
            {
                Quaternion rot = Quaternion.LookRotation(dir);
                transform.rotation = Quaternion.Slerp(transform.rotation, rot, threatRotateSpeed * Time.deltaTime);
            }
        }

        private void EscapeMove()
        {
            Vector3 dir = (escapeDestination - transform.position).normalized;
            Quaternion rot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, rot, 5f * Time.deltaTime);

            transform.position += transform.forward * escapeMoveSpeed * Time.deltaTime;

            escapeTimer += Time.deltaTime;
            if (escapeTimer >= escapeDuration)
            {
                isEscaping = false;
                threatTarget = null;
                escapeTimer = 0f;
                onEscapeComplete?.Invoke();
                SetNextSwimState();
                OnSwimStateChanged?.Invoke(false);
            }
        }

        private void SetNextSwimState()
        {
            isPaused = true;
            stateTimer = 0f;
            stateDuration = UnityEngine.Random.Range(minPauseTime, maxPauseTime);
            OnSwimStateChanged?.Invoke(false);
        }

        public void EnterIntimidate(Transform target)
        {
            threatTarget = target;
            isIntimidating = true;
            OnSwimStateChanged?.Invoke(false);
        }

        public void ExitIntimidate()
        {
            threatTarget = null;
            isIntimidating = false;
            OnSwimStateChanged?.Invoke(true);
        }

        public void SpitInk(Transform threat, Action onComplete = null)
        {
            isIntimidating = false;
            isEscaping = true;
            threatTarget = threat;
            escapeTimer = 0f;
            Vector3 dir = (transform.position - threat.position).normalized;

            RaycastHit hit;
            Vector3 candidateDestination = transform.position + dir * inkEscapeDistance;
            if (Physics.Raycast(transform.position, dir, out hit, inkEscapeDistance))
            {
                escapeDestination = hit.point - dir * 0.5f;
            }
            else
            {
                escapeDestination = candidateDestination;
            }

            onEscapeComplete = onComplete;
            OnSwimStateChanged?.Invoke(true);
        }
    }
}