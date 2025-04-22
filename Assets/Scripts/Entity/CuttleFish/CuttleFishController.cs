using Blue.Interface;
using UnityEngine;

namespace Blue.Entity
{
    public class CuttleFishController : BaseEntityController<CuttleFishModel, CuttleFishView>, IScannable
    {
        [SerializeField] private float threatSizeThreshold = 1.0f;
        [SerializeField] private float rotationSpeed = 5f;
        [SerializeField] private float inkTriggerDistance = 1.5f;
        [SerializeField] private float inkTriggerTime = 10.0f;
        private ILivingEntity threateningEntity;
        private float intimidateTimer = 0f;

        public string DisplayName => model.Status.Name;
        public Renderer[] TargetRenderers => new Renderer[] { view.Renderer };

        protected override void Awake()
        {
            model = new CuttleFishModel();
            model.Initialize(data);
        }

        private void Update()
        {
            if (model.CurrentState == CuttleFishModel.CuttleFishState.Intimidate && threateningEntity is MonoBehaviour target)
            {
                Vector3 targetPosition = target.transform.position;
                targetPosition.y = transform.position.y;
                Vector3 direction = (targetPosition - transform.position).normalized;
                if (direction.sqrMagnitude > 0.01f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(direction);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 5f * Time.deltaTime);
                }

                CheckSpitInkTrigger(target);
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

            switch (state)
            {
                case CuttleFishModel.CuttleFishState.Dim:
                    view.SetEmissionColorDim(0.2f);
                    break;
                case CuttleFishModel.CuttleFishState.Bright:
                    view.SetEmissionColorBright(0.2f);
                    break;
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

            // 必要なら、逃走トリガー・速度アップなどをここに入れる
        }

        public void OnScanStart()
        {
            Debug.Log("A");
        }

        public void OnScanEnd()
        {
            Debug.Log("B");
        }
    }
}
