using System;
using Blue.Interface;
using Blue.UI.Common;
using TMPro;
using UniRx;
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
        private IScannable scannable;
        private IDisposable subscription;

        public Transform Target => target;
        public bool IsShowedDetail => name.gameObject.activeSelf;

        public void Initialize(Transform target, IScannable scannable)
        {
            this.target = target;
            this.scannable = scannable;

            // 既存の購読を解除
            subscription?.Dispose();

            // イベント購読
            subscription = scannable.OnScanDataChanged
                .Subscribe(_ => Refresh())
                .AddTo(this);

            Refresh();
            name.gameObject.SetActive(false);
            detail.gameObject.SetActive(false);
            gameObject.SetActive(true);

            scanProgressBar.value = 0f;
        }

        public void Refresh()
        {
            if (scannable == null) return;
            ScanData data = scannable.ScanData;
            nameText.text = data.displayName;
            detailText.text = GenerateDetail(data);
        }

        void Update()
        {
            if (target == null) Destroy(gameObject);
        }

        void OnDisable()
        {
            subscription?.Dispose();
            subscription = null;
        }

        public void ShowDetail()
        {
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

            // 危険度表記（カスタムラベルがあれば優先）
            if (!string.IsNullOrEmpty(data.threatLabel))
            {
                detail += data.threatLabel + "\n";
            }
            else
            {
                switch (data.threat)
                {
                    case ScanData.Threat.Safety:
                        detail += "危険度： 低\n";
                        break;

                    case ScanData.Threat.Warning:
                        detail += "危険度： 中\n";
                        break;

                    case ScanData.Threat.Danger:
                        detail += "危険度： 高\n";
                        break;
                }
            }

            // 捕獲可否表記（カスタムラベルがあれば優先）
            if (!string.IsNullOrEmpty(data.capturableLabel))
            {
                detail += data.capturableLabel;
            }
            else
            {
                if (data.isCapturable)
                {
                    detail += "捕獲可能";
                }
                else
                {
                    detail += "捕獲不可。体力を減らしてください";
                }
            }

            // 詳細文章があれば追加
            if (!string.IsNullOrEmpty(data.description))
            {
                detail += "\n" + data.description;
            }

            return detail;
        }
    }
}