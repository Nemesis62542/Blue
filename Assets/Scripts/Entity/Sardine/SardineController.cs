using System;
using Blue.Interface;
using Blue.UI.Common;
using UniRx;
using UnityEngine;

namespace Blue.Entity
{
    public class SardineController : BaseEntityController<SardineModel, SardineView>, IScannable, ICapturable
    {
        public Renderer[] TargetRenderers => new Renderer[] { view.Renderer };
        public Status Status => model.Status;
        public ScanData ScanData => new ScanData(model.Status.Name, ScanData.Threat.Safety, IsCapturable);
        public IObservable<Unit> OnScanDataChanged => Observable.Never<Unit>();
        public EntityData EntityData => model.Data;
        public bool IsCapturable => true;

        protected override void Awake()
        {
            model = new SardineModel(data);
        }

    }
}