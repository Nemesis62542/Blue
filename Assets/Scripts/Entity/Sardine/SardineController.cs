using Blue.Interface;
using Blue.UI.Common;
using UnityEngine;

namespace Blue.Entity
{
    public class SardineController : BaseEntityController<SardineModel, SardineView>, IScannable, ICapturable
    {
        public Renderer[] TargetRenderers => new Renderer[] { view.Renderer };
        public Status Status => model.Status;
        public ScanData ScanData => new ScanData(model.Status.Name, ScanData.Threat.Safety);
        public EntityData EntityData => model.Data;

        protected override void Awake()
        {
            model = new SardineModel(data);
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