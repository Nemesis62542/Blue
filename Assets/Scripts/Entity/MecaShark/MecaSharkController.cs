using Blue.Interface;
using Blue.UI.Common;
using UnityEngine;

namespace Blue.Entity
{
    public class MecaSharkController : BaseEntityController<MecaSharkModel, MecaSharkView>, IScannable
    {
        public Renderer[] TargetRenderers => new Renderer[] { view.Renderer };
        public ScanData ScanData => new ScanData(model.Status.Name, ScanData.Threat.Danger);
        public Status Status => model.Status;
        public EntityData EntityData => model.Data;

        protected override void Awake()
        {
            model = new MecaSharkModel(data);
        }

        private void Update()
        {
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