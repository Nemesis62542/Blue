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
        public string EntityName => model.Status.Name;

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

        public bool TryCapture(ItemData item)
        {
            if (data == null || item == null)
            {
                Debug.LogWarning("EntityData or ItemData is null");
                return false;
            }

            int capture_level = item.GetAttributeValue(ItemAttribute.Level);
            float entity_size = data.Size;

            if (capture_level < entity_size)
            {
                Debug.Log("捕獲レベル不足");
                return false;
            }

            OnCaptured();
            return true;
        }

        public ItemData GetCapturedItem()
        {
            return data != null ? data.CapturedItem : null;
        }

        protected virtual void OnCaptured()
        {
            Debug.Log($"{EntityName} を捕獲！");
            Destroy(gameObject);
        }
    }
}