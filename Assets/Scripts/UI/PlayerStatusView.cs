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

        public void SetHPRatio(float ratio)
        {
            hpSlider.value = Mathf.Clamp01(ratio);
            hpSliderShadow.value = hpSlider.value;
        }

        public void SetOxygenRatio(float ratio)
        {
            oxygenSlider.value = Mathf.Clamp01(ratio);
            oxygenSliderShadow.value = oxygenSlider.value;
        }

        public void SetDepth(float depth)
        {
            this.depth.text = $"depth : {depth:F1}m";
        }
    }
}