using Blue.Interface;
using UnityEngine;

namespace Blue.Entity
{
    public class CuttleFishController : BaseEntityController<CuttleFishModel, CuttleFishView>
    {
        [SerializeField] private float threatSizeThreshold = 1.0f;
        [SerializeField] private float rotationSpeed = 5f;
        private ILivingEntity threateningEntity;

        protected override void Awake()
        {
            model = new CuttleFishModel();
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
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
                }
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
    }
}
