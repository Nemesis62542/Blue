using Blue.Entity.Common;
using Blue.Interface;
using UnityEngine;

namespace Blue.Entity
{
    public class CuttleFishController : BaseEntityController<CuttleFishModel, CuttleFishView>, IScannable
    {
        [SerializeField] private float threatSizeThreshold = 1.0f;
        [SerializeField] private float inkTriggerDistance = 1.5f;
        [SerializeField] private float inkTriggerTime = 10.0f;
        [SerializeField] private float escapeDistance = 5.0f;
        [SerializeField] private float wanderRadius = 3.0f;
        [SerializeField] private float wanderInterval = 2.0f;

        [SerializeField] private SwimMover swimMover = new SwimMover();

        private ILivingEntity threateningEntity;
        private float intimidateTimer = 0f;
        private float wanderTimer = 0f;
        private bool isWandering = true;

        public string DisplayName => model.Status.Name;
        public Renderer[] TargetRenderers => new Renderer[] { view.Renderer };

        protected override void Awake()
        {
            model = new CuttleFishModel(data);

            swimMover.Initialize(transform);
        }

        private void Update()
        {
            swimMover.UpdateMove();

            if (model.CurrentState == CuttleFishModel.CuttleFishState.Intimidate && threateningEntity is MonoBehaviour target)
            {
                Vector3 target_position = target.transform.position;
                target_position.y = transform.position.y;
                Vector3 direction = (transform.position - target_position).normalized;
                if (direction.sqrMagnitude > 0.01f)
                {
                    Quaternion target_rotation = Quaternion.LookRotation(direction);
                    transform.rotation = Quaternion.Slerp(transform.rotation, target_rotation, 5f * Time.deltaTime);
                }

                CheckSpitInkTrigger(target);
            }

            if (isWandering)
            {
                if (!swimMover.IsMoving)
                {
                    wanderTimer += Time.deltaTime;

                    if (wanderTimer >= wanderInterval)
                    {
                        Vector3 randomPos = transform.position;
                        swimMover.MoveToRandomPosition(randomPos, wanderRadius, () =>
                        {
                            view.SetAnimatorSwim(false);
                        });

                        view.SetAnimatorSwim(true);
                        wanderTimer = 0f;
                    }
                }
                else
                {
                    wanderTimer = 0f;
                }
            }
        }

        private void CheckSpitInkTrigger(MonoBehaviour target)
        {
            float distance = Vector3.Distance(transform.position, target.transform.position);
            if (distance < inkTriggerDistance)
            {
                SpitInk();
            }

            intimidateTimer += Time.deltaTime;
            if (intimidateTimer >= inkTriggerTime)
            {
                SpitInk();
            }
        }

        public void SetState(CuttleFishModel.CuttleFishState state)
        {
            if (model.CurrentState == state) return;

            model.SetState(state);
            isWandering = state == CuttleFishModel.CuttleFishState.Dim;
            view.SetAnimatorSwim(false);

            switch (state)
            {
                case CuttleFishModel.CuttleFishState.Dim:
                    view.SetEmissionColorDim(0.2f);
                    break;
                case CuttleFishModel.CuttleFishState.Bright:
                case CuttleFishModel.CuttleFishState.Intimidate:
                    view.SetEmissionColorBright(0.2f);
                    break;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (threateningEntity != null) return;

            if (other.TryGetComponent(out ILivingEntity entity))
            {
                if (entity.Status.Size >= threatSizeThreshold)
                {
                    threateningEntity = entity;
                    SetState(CuttleFishModel.CuttleFishState.Intimidate);
                    view.SetAnimatorIntimidate(true);
                    if (entity.Status.Size >= threatSizeThreshold)
                    {
                        threateningEntity = entity;
                        SetState(CuttleFishModel.CuttleFishState.Intimidate);
                        view.SetAnimatorIntimidate(true);
                        intimidateTimer = 0f;
                    }
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (threateningEntity == null) return;

            if (other.TryGetComponent(out ILivingEntity entity) && entity == threateningEntity)
            {
                threateningEntity = null;
                SetState(CuttleFishModel.CuttleFishState.Dim);
                view.SetAnimatorIntimidate(false);
            }
        }
        
        public void SpitInk()
        {
            if (model.CurrentState != CuttleFishModel.CuttleFishState.Intimidate) return;

            view.PlayInkEffect();

            Vector3 backDirection = -transform.forward;
            Vector3 escapeDestination = transform.position + backDirection * escapeDistance;
            view.SetAnimatorSwim(true);
            swimMover.MoveTo(escapeDestination, () => view.SetAnimatorSwim(false));
        }

        public void OnScanStart()
        {
            view.EnableHighlight();
        }

        public void OnScanEnd()
        {
            view.DisableHighlight();
        }
    }
}
