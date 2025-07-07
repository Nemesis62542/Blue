using Blue.Interface;
using Blue.UI.Common;
using UnityEngine;

namespace Blue.Entity
{
    public class BlueStripeFishController : BaseEntityController<BlueStripeFishModel, BlueStripeFishView>, IScannable
    {
        public Renderer[] TargetRenderers => new Renderer[] { view.Renderer };

        public Status Status => model.Status;

        public ScanData ScanData => new ScanData(model.Status.Name, ScanData.Threat.Safety);

        protected override void Awake()
        {
            model = new BlueStripeFishModel(data);
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