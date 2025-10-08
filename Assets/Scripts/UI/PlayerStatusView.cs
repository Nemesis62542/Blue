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

        private float targetHPRatio = 1.0f;
        private float targetOxygenRatio = 1.0f;
        private float targetFuelRatio = 1.0f;
        private float animationSpeed = 0.2f;

        private void Start()
        {
            hpSlider.value = targetHPRatio;
            hpSliderShadow.value = targetHPRatio;
            oxygenSlider.value = targetOxygenRatio;
            oxygenSliderShadow.value = targetOxygenRatio;
            fuelSlider.value = targetFuelRatio;
            fuelSliderShadow.value = targetFuelRatio;
        }

        private void Update()
        {
            UpdateGaugeAnimation(hpSlider, hpSliderShadow, targetHPRatio);
            UpdateGaugeAnimation(oxygenSlider, oxygenSliderShadow, targetOxygenRatio);
            UpdateGaugeAnimation(fuelSlider, fuelSliderShadow, targetFuelRatio);
        }

        public void SetHPRatio(float ratio)
        {
            targetHPRatio = Mathf.Clamp01(ratio);
        }

        public void SetOxygenRatio(float ratio)
        {
            targetOxygenRatio = Mathf.Clamp01(ratio);
        }

        public void SetFuelRatio(float ratio)
        {
            targetFuelRatio = Mathf.Clamp01(ratio);
        }

        private void UpdateGaugeAnimation(Slider front, Slider shadow, float targetRatio)
        {
            if (targetRatio > front.value)
            {
                shadow.value = targetRatio;
                front.value = Mathf.MoveTowards(front.value, targetRatio, animationSpeed * Time.deltaTime);
            }
            else if (targetRatio < shadow.value)
            {
                front.value = targetRatio;
                shadow.value = Mathf.MoveTowards(shadow.value, targetRatio, animationSpeed * Time.deltaTime);
            }
        }

        public void SetDepth(float depth)
        {
            this.depth.text = $"depth : {depth:F1}m";
        }
    }
}