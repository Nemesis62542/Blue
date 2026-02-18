using UnityEngine;
using UnityEngine.Audio;

namespace Blue.Audio
{
    public class SoundModel
    {
        private AudioMixer audioMixer;
        private const float MIN_DECIBEL = -80f;

        public SoundModel(AudioMixer mixer)
        {
            audioMixer = mixer;
        }

        public void SetMasterVolume(float volume)
        {
            audioMixer.SetFloat("MasterVolume", ConvertToDecibel(volume));
        }

        public void SetBGMVolume(float volume)
        {
            audioMixer.SetFloat("BGMVolume", ConvertToDecibel(volume));
        }

        public void SetSEVolume(float volume)
        {
            audioMixer.SetFloat("SEVolume", ConvertToDecibel(volume));
        }

        private float ConvertToDecibel(float value)
        {
            return value > 0.0001f ? Mathf.Log10(value) * 20f : MIN_DECIBEL;
        }
    }
}
