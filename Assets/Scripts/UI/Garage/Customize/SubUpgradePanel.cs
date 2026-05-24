using System;
using Blue.Upgrade;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Blue.UI.Garage.Customize
{
    /// <summary>
    /// サブアップグレードパネルのUI制御
    /// - ロック中: 長押しで解放
    /// - 解放後: クリックでオン/オフ切り替え
    /// </summary>
    public class SubUpgradePanel : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private Image lockOverlay;
        [SerializeField] private Image enabledIndicator;
        [SerializeField] private float pressAnimationDuration = 0.3f;

        private SubUpgradeData subUpgradeData;
        private Tweener iconMoveTween;
        private bool isUnlocked;
        private bool isEnabled;

        public event Action<SubUpgradeData> OnPointerEnter;
        public event Action<SubUpgradeData> OnPointerDown;
        public event Action<SubUpgradeData> OnPointerUp;

        public SubUpgradeData SubUpgradeData => subUpgradeData;
        public bool IsUnlocked => isUnlocked;
        public bool IsEnabled => isEnabled;

        public void Initialize(SubUpgradeData data)
        {
            subUpgradeData = data;
            if (icon != null && data.Icon != null)
            {
                icon.sprite = data.Icon;
            }
            if (icon != null)
            {
                Vector3 pos = icon.transform.localPosition;
                icon.transform.localPosition = new Vector3(pos.x, pos.y, 0);
            }
        }

        /// <summary>
        /// 解放/有効状態に応じた表示更新
        /// </summary>
        public void UpdateState(bool unlocked, bool enabled)
        {
            isUnlocked = unlocked;
            isEnabled = enabled;

            if (lockOverlay != null)
            {
                lockOverlay.gameObject.SetActive(!isUnlocked);
            }
            if (enabledIndicator != null)
            {
                enabledIndicator.gameObject.SetActive(isEnabled);
            }
        }

        private void OnDestroy()
        {
            iconMoveTween?.Kill();
        }

        public void OnPointerEnterEvent()
        {
            OnPointerEnter?.Invoke(subUpgradeData);
        }

        public void OnPointerDownEvent()
        {
            OnPointerDown?.Invoke(subUpgradeData);
            PlayPressAnimation();
        }

        public void OnPointerUpEvent()
        {
            OnPointerUp?.Invoke(subUpgradeData);
            PlayReleaseAnimation();
        }

        private void PlayPressAnimation()
        {
            if (icon == null) return;
            iconMoveTween?.Kill();
            iconMoveTween = icon.transform
                .DOLocalMoveZ(30, pressAnimationDuration)
                .SetEase(Ease.OutCubic);
        }

        private void PlayReleaseAnimation()
        {
            if (icon == null) return;
            iconMoveTween?.Kill();
            iconMoveTween = icon.transform
                .DOLocalMoveZ(0, pressAnimationDuration)
                .SetEase(Ease.OutCubic);
        }
    }
}
