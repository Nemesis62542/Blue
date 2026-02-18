using Blue.Interface;
using Blue.UI.Common;
using UnityEngine;

namespace Blue.Entity
{
    public class AmarylisJerryFishController : BaseEntityController<AmarylisJerryFishModel, AmarylisJerryFishView>, IScannable, ICapturable
    {
        public Renderer[] TargetRenderers => new Renderer[] { view.Renderer };
        public Status Status => model.Status;
        public ScanData ScanData => new ScanData(model.Status.Name, ScanData.Threat.Safety, IsCapturable);
        public EntityData EntityData => model.Data;

        public bool IsCapturable => true;

        protected override void Awake()
        {
            model = new AmarylisJerryFishModel(data);
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