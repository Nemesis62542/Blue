using Blue.Attack;
using Blue.Interface;
using Blue.UI.Common;
using UnityEngine;

namespace Blue.Entity
{
    public class MecaSharkController : BaseEntityController<MecaSharkModel, MecaSharkView>, IScannable
    {
        private enum BossState
        {
            Circling,
            PreparingToCharge,
            Charging,
        }

        [SerializeField] private float circleRadius = 10.0f;
        [SerializeField] private float circleSpeed = 2.0f;
        [SerializeField] private float chargeSpeed = 20.0f;
        [SerializeField] private float chargePrepTime = 1.0f;
        [SerializeField] private AttackHitBox attackHitbox;
        [SerializeField] private Vector3 centerPoint;

        private BossState state = BossState.Circling;
        private float stateTimer = 0f;
        private float angle = 0f;
        private Transform player;
        private Vector3 chargeTarget;

        public Renderer[] TargetRenderers => new Renderer[] { view.Renderer };
        public ScanData ScanData => new ScanData(model.Status.Name, ScanData.Threat.Danger);
        public Status Status => model.Status;
        public EntityData EntityData => model.Data;

        protected override void Awake()
        {
            model = new MecaSharkModel(data);
            attackHitbox.Initialize(this, model.Status.AttackPower);
            centerPoint = transform.position;
        }

        public void StartBattle()
        {
            player = GameObject.FindGameObjectWithTag("Player").transform;
        }

        private void Update()
        {
            switch (state)
            {
                case BossState.Circling:
                    HandleCircling();
                    break;
                case BossState.PreparingToCharge:
                    HandleChargePreparation();
                    break;
                case BossState.Charging:
                    HandleCharging();
                    break;
            }
        }

        private void HandleCircling()
        {
            angle += circleSpeed * Time.deltaTime;
            if (angle >= 360f) angle -= 360f;

            Vector3 offset = new Vector3(
                Mathf.Cos(angle) * circleRadius,
                0,
                Mathf.Sin(angle) * circleRadius
            );
            Vector3 targetPos = centerPoint + offset;
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 2.0f);

            Vector3 tangentDir = new Vector3(
                -Mathf.Sin(angle),
                0,
                Mathf.Cos(angle)
            ).normalized;

            Quaternion targetRot = Quaternion.LookRotation(tangentDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 5.0f);
            ApplyStateTimer();
            if (stateTimer >= 5.0f)
            {
                Vector3 toPlayer = (player.position - transform.position).normalized;
                chargeTarget = centerPoint + toPlayer * circleRadius;

                state = BossState.PreparingToCharge;
                stateTimer = 0f;
            }
        }

        private void ApplyStateTimer()
        {
            if (player == null) return;
            stateTimer += Time.deltaTime;
        }

        private void HandleChargePreparation()
        {
            Vector3 toPlayer = (player.position - transform.position).normalized;
            Quaternion targetRot = Quaternion.LookRotation(toPlayer);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 5.0f);

            ApplyStateTimer();
            if (stateTimer >= chargePrepTime)
            {
                state = BossState.Charging;
                stateTimer = 0f;
                view.LockOn(true);
                attackHitbox.StartAttack();
            }
        }

        private void HandleCharging()
        {
            Vector3 toTarget = chargeTarget - transform.position;
            Vector3 moveDir = toTarget.normalized;

            transform.position += moveDir * chargeSpeed * Time.deltaTime;

            Quaternion targetRot = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 5.0f);

            if (toTarget.sqrMagnitude < 1.0f)
            {
                Vector3 flatDir = transform.position - centerPoint;
                flatDir.y = 0f;

                if (flatDir.sqrMagnitude > 0.001f)
                {
                    angle = Mathf.Atan2(flatDir.z, flatDir.x);
                }

                state = BossState.Circling;
                stateTimer = 0f;
                view.LockOn(false);
                attackHitbox.EndAttack();
            }
        }

        public void OnScanEnd()
        {
            view.DisableHighlight();
        }

        public void OnScanStart()
        {
            view.EnableHighlight();
        }
    }
}
