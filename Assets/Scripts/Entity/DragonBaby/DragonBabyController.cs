using Blue.Entity.Common;
using Blue.Interface;
using Blue.UI.Common;
using Cysharp.Threading.Tasks;
using System;
using UnityEngine;

namespace Blue.Entity
{
    public class DragonBabyController : BaseEntityController<DragonBabyModel, DragonBabyView>, IScannable
    {
        [SerializeField] private GroundCrawler crawler;
        [SerializeField] private Vector2 waitTimeRange = new Vector2(2f, 5f);

        public Renderer[] TargetRenderers => new Renderer[] { view.Renderer };
        public Status Status => model.Status;
        public ScanData ScanData => new ScanData(model.Status.Name, ScanData.Threat.Safety, IsCapturable);
        public EntityData EntityData => model.Data;

        public bool IsCapturable => true;

        private const string ANIMATOR_MOVE_PARAM = "Move";

        protected override void Awake()
        {
            model = new DragonBabyModel(data);
        }

        private async void Start()
        {
            await WaitAndMoveLoop(this.GetCancellationTokenOnDestroy());
        }

        private async UniTask WaitAndMoveLoop(System.Threading.CancellationToken ct)
        {
            if (crawler == null) return;

            while (!ct.IsCancellationRequested)
            {
                // ランダムな秒数待機
                float waitTime = UnityEngine.Random.Range(waitTimeRange.x, waitTimeRange.y);
                await UniTask.Delay(TimeSpan.FromSeconds(waitTime), cancellationToken: ct);

                // 次の目的地を設定
                crawler.SetRandomWaypoint();
                view.SetAnimatorBool(ANIMATOR_MOVE_PARAM, true);

                // Arrivalステートになるまで待機
                await UniTask.WaitUntil(() => crawler.State == State.Arrival, cancellationToken: ct);
                view.SetAnimatorBool(ANIMATOR_MOVE_PARAM, false);
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