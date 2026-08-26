using System;
using System.Collections;
using Blue.Entity.Common;
using Blue.Interface;
using Blue.UI.Common;
using UniRx;
using UnityEngine;

namespace Blue.Entity
{
    public class CuttleFishController : BaseEntityController<CuttleFishModel, CuttleFishView>, IScannable
    {
        [SerializeField] private float inkTriggerDistance = 1.5f;
        [SerializeField] private float inkTriggerTime = 10.0f;

        [SerializeField] private BaseSwimmer swimmer;

        [Header("Idle Swim Settings")]
        [SerializeField] private Vector2 pauseTimeRange = new Vector2(10f, 15f);
        [SerializeField] private Vector2 swimTimeRange = new Vector2(1f, 2f);

        [Header("Escape Settings")]
        [SerializeField] private float escapeLegDistance = 5.0f; // 継ぎ足す 1 区間の距離。総移動距離は duration × 速度で決まる
        [SerializeField] private float escapeDuration = 2.5f;
        [SerializeField] private float escapeSpeedScale = 2.0f;
        [SerializeField] private bool rotateAwayFromThreat = false; // 威嚇は相手に正対する

        private ILivingEntity threateningEntity;
        private float intimidateTimer = 0f;
        private bool isSpitting = false;
        private int threatColliderCount;

        public Renderer[] TargetRenderers => new Renderer[] { view.Renderer };
        public ScanData ScanData => new ScanData(model.Status.Name, ScanData.Threat.Safety, true);
        public IObservable<Unit> OnScanDataChanged => Observable.Never<Unit>();
        public EntityData EntityData => model.Data;

        protected override void Awake()
        {
            model = new CuttleFishModel(data);
            swimmer.OnMovingChanged += view.SetAnimatorSwim;
        }

        private void Start()
        {
            EnterIdleCycle();
        }

        private void EnterIdleCycle()
        {
            swimmer.SetBehaviour(new IdleCycleBehaviour(pauseTimeRange, swimTimeRange));
        }

        private void Update()
        {
            if (threateningEntity == null) return;

            // 捕獲などで相手が消えると OnTriggerExit が来ず、威嚇したまま固まる
            if (!(threateningEntity is MonoBehaviour target) || target == null)
            {
                ReleaseThreat();
                return;
            }

            if (model.CurrentState == CuttleFishModel.CuttleFishState.Intimidate)
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
                return; // 同一フレームで時間条件にも掛かると二重に墨を吐く
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
            // 分割したヒットボックスは所有者へ解決される。同一個体からは複数回入ってくる
            if (!EntityHit.TryResolve(other, out ILivingEntity entity)) return;

            if (entity == threateningEntity)
            {
                threatColliderCount++;
                return;
            }

            if (threateningEntity != null || isSpitting) return;

            // 自分より大きい相手だけを脅威とみなす
            if (entity.Size <= model.Size) return;

            threateningEntity = entity;
            threatColliderCount = 1;

            SetState(CuttleFishModel.CuttleFishState.Intimidate);
            view.SetAnimatorIntimidate(true);

            // ヒットボックスを分割したエンティティでは other はボーン側なので、
            // 向き直る先には所有者の Transform を使う
            Transform threat = entity is MonoBehaviour behaviour ? behaviour.transform : other.transform;
            swimmer.SetBehaviour(new FaceTargetBehaviour(threat, rotateAwayFromThreat));

            intimidateTimer = 0f;
        }

        private void OnTriggerExit(Collider other)
        {
            if (threateningEntity == null) return;

            // 分割したヒットボックスは所有者へ解決されるので、同一個体として数えられる
            if (!EntityHit.TryResolve(other, out ILivingEntity entity)) return;
            if (entity != threateningEntity) return;

            threatColliderCount--;

            // まだ同じ個体の別のヒットボックスが範囲内に残っている
            if (threatColliderCount > 0) return;

            ReleaseThreat();
        }

        private void ReleaseThreat()
        {
            threateningEntity = null;
            threatColliderCount = 0;
            view.SetAnimatorIntimidate(false);

            // 逃走中は行動を奪わない。逃げ始めた瞬間に自分の検知範囲から相手が外れるため、
            // ここで徘徊に戻すと数メートルで逃走が打ち切られる
            if (isSpitting) return;

            SetState(CuttleFishModel.CuttleFishState.Dim);
            EnterIdleCycle();
        }

        private IEnumerator SpitInkRoutine(Transform threat)
        {
            isSpitting = true;
            SetState(CuttleFishModel.CuttleFishState.Bright);
            view.SetAnimatorIntimidate(false);
            view.SetAnimatorSwim(true);
            view.PlayInkEffect();

            swimmer.SetBehaviour(FleeBehaviour.AwayFrom(
                swimmer,
                threat.position,
                escapeLegDistance,
                escapeDuration,
                escapeSpeedScale,
                () =>
                {
                    SetState(CuttleFishModel.CuttleFishState.Dim);
                    isSpitting = false;
                    intimidateTimer = 0f;

                    // 逃げ切る前に相手が範囲外へ出ていると OnTriggerExit が走らないため、
                    // ここでも脅威を解除しておかないと二度と威嚇できなくなる
                    threateningEntity = null;
                    threatColliderCount = 0;

                    EnterIdleCycle();
                }));

            yield return null;
        }

    }
}
