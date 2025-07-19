using Blue.Interface;
using Blue.Player;
using Blue.UI.Common;
using UnityEngine;

namespace Blue.Entity
{
    public class MecaSharkController : BaseEntityController<MecaSharkModel, MecaSharkView>, IScannable
    {
        private enum BossState
        {
            Circling,
            Charging,
        }

        [SerializeField] private float circleRadius = 10.0f;
        [SerializeField] private float circleSpeed = 2.0f;
        [SerializeField] private float chargeSpeed = 20.0f;

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
            StartBattle();
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
            Vector3 target_pos = player.position + offset;

            Vector3 prev_pos = transform.position;
            transform.position = Vector3.Lerp(transform.position, target_pos, Time.deltaTime * 2.0f);

            Vector3 move_dir = transform.position - prev_pos;
            if (move_dir.sqrMagnitude > 0.001f)
            {
                Quaternion target_rot = Quaternion.LookRotation(move_dir.normalized);
                transform.rotation = Quaternion.Slerp(transform.rotation, target_rot, Time.deltaTime * 5.0f);
            }

            stateTimer += Time.deltaTime;
            if (stateTimer >= 5.0f)
            {
                Vector3 to_center = (player.position - transform.position).normalized;
                chargeTarget = player.position + to_center * circleRadius;

                state = BossState.Charging;
                view.LockOn(true);
                stateTimer = 0f;
            }
        }

        private void HandleCharging()
        {
            Vector3 to_target = chargeTarget - transform.position;
            Vector3 move_dir = to_target.normalized;

            transform.position += move_dir * chargeSpeed * Time.deltaTime;

            Quaternion target_rot = Quaternion.LookRotation(move_dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, target_rot, Time.deltaTime * 5.0f);

            if (to_target.sqrMagnitude < 1.0f)
            {
                Vector3 flat_dir = transform.position - player.position;
                flat_dir.y = 0f;

                if (flat_dir.sqrMagnitude > 0.001f)
                {
                    angle = Mathf.Atan2(flat_dir.z, flat_dir.x);
                }

                state = BossState.Circling;
                view.LockOn(false);
                stateTimer = 0f;
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