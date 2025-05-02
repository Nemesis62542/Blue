using UnityEngine;
using UnityEngine.UI;

namespace Blue.UI
{
    public class PlayerStatusView : MonoBehaviour
    {
        [SerializeField] private Slider hpSlider;
        [SerializeField] private Slider oxygenSlider;

        public void SetHPRatio(float ratio)
        {
            hpSlider.value = Mathf.Clamp01(ratio);
        }

        public void SetOxygenRatio(float ratio)
        {
            oxygenSlider.value = Mathf.Clamp01(ratio);
        }
    }
}