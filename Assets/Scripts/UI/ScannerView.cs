using System.Collections.Generic;
using Blue.Interface;
using UnityEngine;

namespace Blue.UI
{
    public class ScannerView : MonoBehaviour
    {
        [SerializeField] private ScanUIElement scanUIPrefab;

        private Dictionary<IScannable, ScanUIElement> details = new Dictionary<IScannable, ScanUIElement>();
        private Camera mainCamera;
        private Queue<ScanUIElement> pool = new Queue<ScanUIElement>();

        public bool IsShowedDetail(IScannable scannable)
        {
            if (details.TryGetValue(scannable, out ScanUIElement element))
            {
                return element.IsShowedDetail;
            }
            return false;
        }

        private ScanUIElement GetOrCreateElement()
        {
            if (pool.Count > 0)
            {
                ScanUIElement element = pool.Dequeue();
                element.gameObject.SetActive(true);
                return element;
            }
            return Instantiate(scanUIPrefab, transform);
        }

        public void SetDetailUI(Transform target, IScannable scannable)
        {
            if (!details.ContainsKey(scannable))
            {
                ScanUIElement element = GetOrCreateElement();
                element.Initialize(target, scannable.ScanData);
                details[scannable] = element;
            }
        }

        public void ToggleLookingUI(IScannable scannable, bool is_looking)
        {
            if (scannable != null && details.TryGetValue(scannable, out ScanUIElement element))
            {
                element.ToggleLookingUI(is_looking);
            }
        }

        public void UpdateDetailUI(IScannable scannable, bool is_within_distance)
        {
            if (details.TryGetValue(scannable, out ScanUIElement element))
            {
                bool is_visible = IsVisibleInViewport(element.Target.position);
                bool should_be_visible = is_within_distance && is_visible;

                if (element.gameObject.activeSelf != should_be_visible)
                {
                    element.gameObject.SetActive(should_be_visible);
                }

                if (should_be_visible)
                {
                    Vector3 screen_position = mainCamera.WorldToScreenPoint(element.Target.position);
                    element.transform.position = screen_position;
                }
            }
        }

        private bool IsVisibleInViewport(Vector3 world_position)
        {
            Vector3 viewport_position = mainCamera.WorldToViewportPoint(world_position);
            return viewport_position.z > 0 &&
                   viewport_position.x >= 0 && viewport_position.x <= 1 &&
                   viewport_position.y >= 0 && viewport_position.y <= 1;
        }

        public void UpdateScanProgress(IScannable scannable, float progress)
        {
            if (details.TryGetValue(scannable, out ScanUIElement element))
            {
                element.UpdateScanProgress(progress);
            }
        }

        public void ReflashScanUI()
        {
            foreach (ScanUIElement element in details.Values)
            {
                element.gameObject.SetActive(false);
                pool.Enqueue(element);
            }
            details.Clear();
        }

        private void Start()
        {
            mainCamera = Camera.main;
        }

        public void ShowDetail(IScannable scannable)
        {
            if (details.TryGetValue(scannable, out ScanUIElement element))
            {
                element.ShowDetail();
            }
        }
    }
}