using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Blue.UI
{
    public class PlayerStatusView : MonoBehaviour
    {
        [SerializeField] private Slider hpSlider;
        [SerializeField] private Slider oxygenSlider;
        [SerializeField] private Slider fuelSlider;
        [SerializeField] private Slider hpSliderShadow;
        [SerializeField] private Slider oxygenSliderShadow;
        [SerializeField] private Slider fuelSliderShadow;
        [SerializeField] private TextMeshProUGUI depth;
        [SerializeField] private float animationDuration = 0.5f;
        [SerializeField] private float shadowDelay = 0.5f;

        private Tweener hpTweener;
        private Tweener oxygenTweener;
        private Tweener fuelTweener;

        private void Start()
        {
            hpSlider.value = 1f;
            hpSliderShadow.value = 1f;
            oxygenSlider.value = 1f;
            oxygenSliderShadow.value = 1f;
            fuelSlider.value = 1f;
            fuelSliderShadow.value = 1f;
        }

        private void OnDestroy()
        {
            // Tweenのクリーンアップ
            hpTweener?.Kill();
            oxygenTweener?.Kill();
            fuelTweener?.Kill();
        }

        public void SetHPRatio(float ratio)
        {
            ratio = Mathf.Clamp01(ratio);
            AnimateGauge(hpSlider, hpSliderShadow, ratio, ref hpTweener);
        }

        public void SetOxygenRatio(float ratio)
        {
            ratio = Mathf.Clamp01(ratio);
            AnimateGauge(oxygenSlider, oxygenSliderShadow, ratio, ref oxygenTweener);
        }

        public void SetFuelRatio(float ratio)
        {
            ratio = Mathf.Clamp01(ratio);
            AnimateGauge(fuelSlider, fuelSliderShadow, ratio, ref fuelTweener);
        }

        private void AnimateGauge(Slider front, Slider shadow, float targetValue, ref Tweener tweener)
        {
            // 既存のアニメーションをキャンセル
            tweener?.Kill();

            float currentValue = front.value;

            if (targetValue > currentValue)
            {
                // 増加時: Shadowが先に伸びてからFrontが追従
                shadow.value = targetValue;
                tweener = front.DOValue(targetValue, animationDuration).SetEase(Ease.OutCubic);
            }
            else if (targetValue < currentValue)
            {
                // 減少時: Frontが先に縮んでからShadowが追従
                front.value = targetValue;
                tweener = shadow.DOValue(targetValue, animationDuration).SetDelay(shadowDelay).SetEase(Ease.OutCubic);
            }
        }

        public void SetDepth(float depth)
        {
            this.depth.text = $"depth : {depth:F1}m";
        }
    }
}