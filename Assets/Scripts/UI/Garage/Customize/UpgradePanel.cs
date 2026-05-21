using System;
using Blue.Upgrade;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Blue.UI.Garage.Customize
{
    public class UpgradePanel : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private float pressAnimationDuration = 0.3f;

        private UpgradeData upgradeData;
        private Tweener iconMoveTween;

        public event Action<UpgradeData> OnPointerEnter;
        public event Action<UpgradeData> OnPointerDown;
        public event Action<UpgradeData> OnPointerUp;

        public UpgradeData UpgradeData => upgradeData;

        public void SetLevel(int level)
        {
            if (levelText != null)
            {
                levelText.text = $"Lv.{level + 1}";
            }
        }

        public void Initialize(UpgradeData data)
        {
            upgradeData = data;
            if (icon != null && data.Icon != null)
            {
                icon.sprite = data.Icon;
            }
            if (icon != null)
            {
                Vector3 pos = icon.transform.localPosition;
                icon.transform.localPosition = new Vector3(pos.x, pos.y, -30);
            }
        }

        private void OnDestroy()
        {
            iconMoveTween?.Kill();
        }

        public void OnPointerEnterEvent()
        {
            OnPointerEnter?.Invoke(upgradeData);
        }

        public void OnPointerDownEvent()
        {
            OnPointerDown?.Invoke(upgradeData);
            PlayPressAnimation();
        }

        public void OnPointerUpEvent()
        {
            OnPointerUp?.Invoke(upgradeData);
            PlayReleaseAnimation();
        }

        private void PlayPressAnimation()
        {
            if (icon == null) return;
            iconMoveTween?.Kill();
            iconMoveTween = icon.transform
                .DOLocalMoveZ(0, pressAnimationDuration)
                .SetEase(Ease.OutCubic);
        }

        private void PlayReleaseAnimation()
        {
            if (icon == null) return;
            iconMoveTween?.Kill();
            iconMoveTween = icon.transform
                .DOLocalMoveZ(-30, pressAnimationDuration)
                .SetEase(Ease.OutCubic);
        }
    }
}
