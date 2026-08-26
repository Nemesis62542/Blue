using System;
using Blue.Interface;
using Blue.UI.Common;
using UniRx;
using UnityEngine;

namespace Blue.Entity
{
    public class BloodySharkController : BaseEntityController<BloodySharkModel, BloodySharkView>, IScannable, ICapturable
    {
        public Renderer[] TargetRenderers => new Renderer[] { view.Renderer };
        public ScanData ScanData => new ScanData(model.Status.Name, ScanData.Threat.Safety, IsCapturable);
        public IObservable<Unit> OnScanDataChanged => Observable.Never<Unit>();
        public Status Status => model.Status;
        public EntityData EntityData => model.Data;

        public bool IsCapturable => true;

        protected override void Awake()
        {
            model = new BloodySharkModel(data);
            
            view.SetAnimatorSwim(true);
        }
    }
}