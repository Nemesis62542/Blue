using System.Collections.Generic;
using UnityEngine;

namespace Blue.UI
{
    public class ScannerView : MonoBehaviour
    {
        [SerializeField] private ScanUIElement scanUIPrefab;
        [SerializeField] private UIController uiController;
        [SerializeField] private float defaultDisplayDuration = 3.0f;

        public void ShowScanUI(Transform target, string displayName, float duration = -1f)
        {
            ScanUIElement element = Instantiate(scanUIPrefab, transform);
            float show_time = duration > 0f ? duration : defaultDisplayDuration;
            element.Initialize(target, displayName, show_time);
        }

        private void Update()
        {
            ScanUIElement[] elements = GetComponentsInChildren<ScanUIElement>(true);
            for (int i = 0; i < elements.Length; i++)
            {
                ScanUIElement element = elements[i];
                Vector3 viewport_position = Camera.main.WorldToViewportPoint(element.Target.position);

                if (viewport_position.z < 0 || viewport_position.x < 0 || viewport_position.x > 1 || viewport_position.y < 0 || viewport_position.y > 1)
                {
                    if (element.gameObject.activeSelf) element.gameObject.SetActive(false);
                }
                else if (!element.gameObject.activeSelf)
                {
                    element.gameObject.SetActive(true);
                }

                Vector3 screen_position = new Vector3(
                    viewport_position.x * UnityEngine.Screen.width,
                    viewport_position.y * UnityEngine.Screen.height,
                    0
                );

                element.transform.position = screen_position;
                element.IncreaseElapsed();
            }
        }
    }
}