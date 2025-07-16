using Blue.Entity.Common;
using Blue.Interface;
using Blue.Item;
using Blue.UI.Common;
using UnityEngine;

namespace Blue.Entity
{
    public class CoelacanthController : BaseEntityController<CoelacanthModel, CoelacanthView>, IScannable, ICapturable
    {
        public Renderer[] TargetRenderers => new Renderer[] { view.Renderer };
        public ScanData ScanData => new ScanData(model.Status.Name, ScanData.Threat.Safety);
        public Status Status => model.Status;
        public EntityData EntityData => model.Data;

        protected override void Awake()
        {
            model = new CoelacanthModel(data);
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