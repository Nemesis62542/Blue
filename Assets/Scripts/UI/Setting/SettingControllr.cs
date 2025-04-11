using NFPS.Audio;
using UnityEngine;
using UnityEngine.Audio;

namespace NFPS.UI.Setting
{
    public class SettingsController : MonoBehaviour
    {
        [SerializeField] private SettingsView settingsView;
        [SerializeField] private AudioMixer audioMixer;

        private SoundModel audioModel;

        private void Awake()
        {
            audioModel = new SoundModel(audioMixer);

            settingsView.MasterSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
            settingsView.BGMSlider.onValueChanged.AddListener(OnBGMVolumeChanged);
            settingsView.SESlider.onValueChanged.AddListener(OnSEVolumeChanged);
        }

        private void OnDestroy()
        {
            settingsView.MasterSlider.onValueChanged.RemoveListener(OnMasterVolumeChanged);
            settingsView.BGMSlider.onValueChanged.RemoveListener(OnBGMVolumeChanged);
            settingsView.SESlider.onValueChanged.RemoveListener(OnSEVolumeChanged);
        }

        private void OnMasterVolumeChanged(float value)
        {
            audioModel.SetMasterVolume(value);
        }

        private void OnBGMVolumeChanged(float value)
        {
            audioModel.SetBGMVolume(value);
        }

        private void OnSEVolumeChanged(float value)
        {
            audioModel.SetSEVolume(value);
        }
    }
}
