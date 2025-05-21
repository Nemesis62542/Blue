using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Blue.UI
{
    public class ScanUIElement : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private RectTransform detail;
        [SerializeField] private RectTransform lookingUI;
        [SerializeField] private Slider scanProgressBar;

        private Transform target;

        public Transform Target => target;

        public void Initialize(Transform target, string display_name)
        {
            this.target = target;

            nameText.text = display_name;
            gameObject.SetActive(true);
        }

        public void ShowDetail()
        {
            detail.gameObject.SetActive(true);
        }

        public void ToggleLookingUI(bool is_looking)
        {
            lookingUI.gameObject.SetActive(is_looking);
        }

        public void UpdateScanProgress(float progress)
        {
            if (scanProgressBar != null)
            {
                scanProgressBar.value = Mathf.Clamp01(progress);
            }
        }
    }
}