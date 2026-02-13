using Blue.Attack;
using Blue.Interface;
using Blue.Player;
using Blue.UI.Common;
using UnityEngine;

namespace Blue.Entity
{
    public class GrouperController : BaseEntityController<GrouperModel, GrouperView>, IScannable, ICapturable
    {
        [Header("攻撃設定")]
        [SerializeField] private float attackRange = 5f;
        [SerializeField] private float attackCooldown = 2f;
        [SerializeField] private AttackHitBox attackHitbox;

        public Renderer[] TargetRenderers => new Renderer[] { view.Renderer };
        public Status Status => model.Status;
        public ScanData ScanData => new ScanData(model.Status.Name, ScanData.Threat.Warning, true);
        public EntityData EntityData => model.Data;
        public bool IsCapturable => true;

        private float lastAttackTime;

        protected override void Awake()
        {
            model = new GrouperModel(data);
            attackHitbox.Initialize(this, model.Status.AttackPower);
        }

        private void Update()
        {
            if (PlayerController.Instance == null) return;

            Vector3 playerPosition = PlayerController.Instance.transform.position;
            Vector3 direction = playerPosition - transform.position;
            float distance = direction.magnitude;

            if (distance <= attackRange && Time.time >= lastAttackTime + attackCooldown)
            {
                if (CanSeePlayer(direction, distance))
                {
                    float targetYRotation = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
                    transform.rotation = Quaternion.Euler(transform.eulerAngles.x, targetYRotation, transform.eulerAngles.z);
                    view.SetAnimatorTrigger("Attack");
                    attackHitbox.StartAttack();
                    lastAttackTime = Time.time;
                }
            }
        }

        private void EndAttack()
        {
            attackHitbox.EndAttack();
        }

        private bool CanSeePlayer(Vector3 direction, float distance)
        {
            if (Physics.Raycast(transform.position, direction.normalized, out RaycastHit hit, distance))
            {
                return hit.collider.TryGetComponent<PlayerController>(out _);
            }
            return false;
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