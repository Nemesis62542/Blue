using System;
using Blue.Entity.Common;
using Blue.Interface;
using Blue.Item;
using Blue.UI.Common;
using UniRx;
using UnityEngine;

namespace Blue.Entity
{
    public class CoelacanthController : BaseEntityController<CoelacanthModel, CoelacanthView>, IScannable, ICapturable
    {
        public Renderer[] TargetRenderers => new Renderer[] { view.Renderer };
        public ScanData ScanData => new ScanData(model.Status.Name, ScanData.Threat.Safety, IsCapturable);
        public IObservable<Unit> OnScanDataChanged => Observable.Never<Unit>();
        public Status Status => model.Status;
        public EntityData EntityData => model.Data;

        public bool IsCapturable => true;

        protected override void Awake()
        {
            model = new CoelacanthModel(data);
        }

    }
}