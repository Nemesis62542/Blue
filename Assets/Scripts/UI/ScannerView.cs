using System;
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

        public bool IsShowedDetail(IScannable scannable)
        {
            if (details.TryGetValue(scannable, out ScanUIElement element))
            {
                return element.IsShowedDetail;
            }
            else return false;
        }

        public void SetDetailUI(Transform target, IScannable scannable)
        {
            if (details.ContainsKey(scannable))
            {
                Destroy(details[scannable].gameObject);
                details.Remove(scannable);
            }

            ScanUIElement element = Instantiate(scanUIPrefab, transform);
            element.Initialize(target, scannable.DisplayName);
            details.Add(scannable, element);
        }

        public void ToggleLookingUI(IScannable scannable, bool is_looking)
        {
            if (scannable == null) return;
            if (details.TryGetValue(scannable, out ScanUIElement element))
            {
                element.ToggleLookingUI(is_looking);
            }
        }

        public void UpdateDetailUI(IScannable scannable, bool is_within_distance)
        {
            if (details.TryGetValue(scannable, out ScanUIElement element))
            {
                Vector3 viewport_position = mainCamera.WorldToViewportPoint(element.Target.position);
                bool is_visible = viewport_position.z > 0 &&
                                         viewport_position.x >= 0 && viewport_position.x <= 1 &&
                                         viewport_position.y >= 0 && viewport_position.y <= 1;

                bool should_be_visible = is_within_distance && is_visible;

                if (element.gameObject.activeSelf != should_be_visible)
                {
                    element.gameObject.SetActive(should_be_visible);
                }

                if (should_be_visible)
                {
                    Vector3 screen_position = new Vector3(
                        viewport_position.x * UnityEngine.Screen.width,
                        viewport_position.y * UnityEngine.Screen.height,
                        0
                    );

                    element.transform.position = screen_position;
                }
            }
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
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
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