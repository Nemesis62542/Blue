using System.Collections.Generic;
using Blue.Interface;
using NUnit.Framework;
using UnityEngine;

namespace Blue.UI
{
    public class ScannerView : MonoBehaviour
    {
        [SerializeField] private ScanUIElement scanUIPrefab;
        [SerializeField] private new Camera camera;

        private Dictionary<IScannable, ScanUIElement> details = new Dictionary<IScannable, ScanUIElement>();
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

        public void UpdateDetailUI(IScannable scannable)
        {
            if (details.TryGetValue(scannable, out ScanUIElement element))
            {
                bool is_visible = IsVisibleInViewport(element.Target.position);

                if (element.gameObject.activeSelf != is_visible)
                {
                    element.gameObject.SetActive(is_visible);
                }

                if (is_visible)
                {
                    Vector3 world_position = element.Target.position;
                    Vector3 screen_position = camera.WorldToScreenPoint(world_position);

                    Vector2 local_position;
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        element.transform.parent as RectTransform,
                        screen_position,
                        camera,
                        out local_position
                    );

                    element.transform.localPosition = new Vector3(local_position.x, local_position.y, 0f);

                    bool is_center = IsCenter(element.Target.position);
                    if (is_center) element.ShowDetail();
                    else element.HideDetail();
                    element.ToggleLookingUI(is_center);
                }
            }
        }

        private bool IsCenter(Vector3 position)
        {
            Vector3 viewportPos = camera.WorldToViewportPoint(position);

            return
                viewportPos.z > 0f &&
                Mathf.Abs(viewportPos.x - 0.5f) < 0.1f &&
                Mathf.Abs(viewportPos.y - 0.5f) < 0.1f;
        }

        private bool IsVisibleInViewport(Vector3 world_position)
        {
            Vector3 viewport_position = camera.WorldToViewportPoint(world_position);
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
    }
}