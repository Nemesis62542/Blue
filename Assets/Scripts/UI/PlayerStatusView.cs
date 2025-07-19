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
        [SerializeField] private Slider hpSliderShadow;
        [SerializeField] private Slider oxygenSliderShadow;
        [SerializeField] private TextMeshProUGUI depth;
        [SerializeField] private float tweenDuration = 1.5f;

        private float currentHPRatio = 1.0f;
        private float currentOxygenRatio = 1.0f;

        public void SetHPRatio(float ratio)
        {
            UpdateGaugeView(hpSlider, hpSliderShadow, currentHPRatio, ratio);
            currentHPRatio = Mathf.Clamp01(ratio);
        }

        public void SetOxygenRatio(float ratio)
        {
            UpdateGaugeView(oxygenSlider, oxygenSliderShadow, currentOxygenRatio, ratio);
            currentOxygenRatio = Mathf.Clamp01(ratio);
        }

        public void UpdateGaugeView(Slider front, Slider shadow, float current_ratio, float ratio)
        {
            ratio = Mathf.Clamp01(ratio);

            if (ratio > current_ratio)
            {
                shadow.value = ratio;
                DOTween.To(() => front.value, x => front.value = x, ratio, tweenDuration);
            }
            else if (ratio < current_ratio)
            {
                front.value = ratio;
                DOTween.To(() => shadow.value, x => shadow.value = x, ratio, tweenDuration);
            }
        }

        public void SetDepth(float depth)
        {
            this.depth.text = $"depth : {depth:F1}m";
        }
    }
}