using UnityEngine;
using UnityEngine.UI;

namespace NFPS.UI.Setting
{
    public class SettingsView : MonoBehaviour
    {
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider bgmVolumeSlider;
        [SerializeField] private Slider seVolumeSlider;

        public Slider MasterSlider => masterVolumeSlider;
        public Slider BGMSlider => bgmVolumeSlider;
        public Slider SESlider => seVolumeSlider;
    }
}
