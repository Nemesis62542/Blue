using Blue.UI.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Blue.UI
{
    public class ScanUIElement : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI detailText;
        [SerializeField] private new RectTransform name;
        [SerializeField] private RectTransform detail;
        [SerializeField] private RectTransform lookingUI;
        [SerializeField] private Slider scanProgressBar;

        private Transform target;

        public Transform Target => target;
        public bool IsShowedDetail => name.gameObject.activeSelf;

        public void Initialize(Transform target, ScanData data)
        {
            this.target = target;
            nameText.text = data.displayName;
            detailText.text = GenerateDetail(data);
            name.gameObject.SetActive(false);
            detail.gameObject.SetActive(false);
            gameObject.SetActive(true);

            scanProgressBar.value = 0f;
        }

        void Update()
        {
            if (target == null) Destroy(gameObject);
        }

        public void ShowDetail()
        {
            scanProgressBar.gameObject.SetActive(false);
            name.gameObject.SetActive(true);
            detail.gameObject.SetActive(true);
        }

        public void HideDetail()
        {
            name.gameObject.SetActive(false);
            detail.gameObject.SetActive(false);
        }

        public void ToggleLookingUI(bool is_looking)
        {
            lookingUI.gameObject.SetActive(is_looking);
        }

        public void UpdateScanProgress(float progress)
        {
            if (!scanProgressBar.gameObject.activeSelf) scanProgressBar.gameObject.SetActive(true);
            if (scanProgressBar != null)
            {
                scanProgressBar.value = Mathf.Clamp01(progress);
            }

            if (scanProgressBar.value <= 0.001f) scanProgressBar.gameObject.SetActive(false);
        }

        private string GenerateDetail(ScanData data)
        {
            if (data == null) return "詳細不明";
            string detail = "";

            switch (data.threat)
            {
                case ScanData.Threat.Safety:
                    detail += "危険度：<color=green>低</color>\n";
                    break;

                case ScanData.Threat.Warning:
                    detail += "危険度：<color=yellow>中</color>\n";
                    break;

                case ScanData.Threat.Danger:
                    detail += "危険度：<color=green>高</color>\n";
                    break;
            }

            return detail;
        }
    }
}