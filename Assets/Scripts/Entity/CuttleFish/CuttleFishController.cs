using Blue.Entity.Common;
using Blue.Interface;
using Blue.UI.Common;
using UnityEngine;
using System.Collections;

namespace Blue.Entity
{
    public class CuttleFishController : BaseEntityController<CuttleFishModel, CuttleFishView>, IScannable
    {
        [SerializeField] private float inkTriggerDistance = 1.5f;
        [SerializeField] private float inkTriggerTime = 10.0f;

        [SerializeField] private CuttleFishSwimmer swimmer;

        private ILivingEntity threateningEntity;
        private float intimidateTimer = 0f;
        private bool isWandering = true;
        private bool isSpitting = false;

        public Renderer[] TargetRenderers => new Renderer[] { view.Renderer };
        public ScanData ScanData => new ScanData(model.Status.Name, ScanData.Threat.Safety);
        public EntityData EntityData => model.Data;

        protected override void Awake()
        {
            model = new CuttleFishModel(data);
            swimmer.OnSwimStateChanged += view.SetAnimatorSwim;
        }

        private void Update()
        {
            if (model.CurrentState == CuttleFishModel.CuttleFishState.Intimidate && threateningEntity is MonoBehaviour target)
            {
                CheckSpitInkTrigger(target);
            }
        }

        private void CheckSpitInkTrigger(MonoBehaviour target)
        {
            if (isSpitting) return;

            float distance = Vector3.Distance(transform.position, target.transform.position);
            if (distance < inkTriggerDistance)
            {
                StartCoroutine(SpitInkRoutine(target.transform));
            }

            intimidateTimer += Time.deltaTime;
            if (intimidateTimer >= inkTriggerTime)
            {
                StartCoroutine(SpitInkRoutine(target.transform));
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
                threateningEntity = entity;
                SetState(CuttleFishModel.CuttleFishState.Intimidate);
                view.SetAnimatorIntimidate(true);
                swimmer.EnterIntimidate(other.gameObject.transform);
                intimidateTimer = 0f;
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
                swimmer.ExitIntimidate();
            }
        }

        private IEnumerator SpitInkRoutine(Transform threat)
        {
            isSpitting = true;
            SetState(CuttleFishModel.CuttleFishState.Bright);
            view.SetAnimatorIntimidate(false);
            view.SetAnimatorSwim(true);
            view.PlayInkEffect();

            swimmer.SpitInk(threat, () =>
            {
                SetState(CuttleFishModel.CuttleFishState.Dim);
                isSpitting = false;
            });

            yield return null;
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
