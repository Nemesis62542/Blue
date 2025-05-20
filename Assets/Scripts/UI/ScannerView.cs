using System.Collections.Generic;
using Blue.UI.Screen;
using UnityEngine;

namespace Blue.UI
{
    public class ScannerView : MonoBehaviour
    {
        [SerializeField] private ScanUIElement scanUIPrefab;
        [SerializeField] private UIController uiController;
        [SerializeField] private float defaultDisplayDuration = 3.0f;

        private readonly List<ScanUIElement> activeElements = new List<ScanUIElement>();

        public void ShowScanUI(Transform target, string displayName, float duration = -1f)
        {
            ScanUIElement element = GetAvailableElement();
            float show_time = duration > 0f ? duration : defaultDisplayDuration;
            element.Initialize(target, displayName, show_time);
        }

        private void Update()
        {
            foreach (ScanUIElement element in activeElements)
            {
                if (!element.IsActive)
                {
                    element.gameObject.SetActive(false);
                }
            }
        }

        private ScanUIElement GetAvailableElement()
        {
            foreach (ScanUIElement element in activeElements)
            {
                if (!element.IsActive)
                {
                    return element;
                }
            }

            ScanUIElement new_element = Instantiate(scanUIPrefab, transform);
            activeElements.Add(new_element);
            return new_element;
        }
    }
}