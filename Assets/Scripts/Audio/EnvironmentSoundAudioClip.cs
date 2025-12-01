using UnityEngine;
using System.Collections.Generic;

namespace Blue.Audio
{
    [CreateAssetMenu(fileName = "EnvironmentSoundAudioClip", menuName = "Blue/ScriptableObject/EnvironmentSoundAudioClip")]
    public class EnvironmentSoundAudioClip : ScriptableObject
    {
        [System.Serializable]
        private class EnvironmentSoundData
        {
            public EnvironmentSoundType type;
            public AudioClip clip;
            public bool loop = true;
        }

        [SerializeField] private List<EnvironmentSoundData> soundList = new List<EnvironmentSoundData>();

        private Dictionary<EnvironmentSoundType, EnvironmentSoundData> soundDictionary;

        private void OnEnable()
        {
            soundDictionary = new Dictionary<EnvironmentSoundType, EnvironmentSoundData>();
            foreach (EnvironmentSoundData sound in soundList)
            {
                if (!soundDictionary.ContainsKey(sound.type))
                {
                    soundDictionary.Add(sound.type, sound);
                }
            }
        }

        public AudioClip GetClip(EnvironmentSoundType type)
        {
            if (soundDictionary.TryGetValue(type, out EnvironmentSoundData data))
            {
                return data.clip;
            }

            Debug.LogWarning($"EnvironmentSoundType {type} の AudioClip が見つかりません");
            return null;
        }

        public bool GetLoop(EnvironmentSoundType type)
        {
            if (soundDictionary.TryGetValue(type, out EnvironmentSoundData data))
            {
                if (data.clip == null)
                {
                    Debug.LogWarning($"EnvironmentSoundType {type} に設定された AudioClip が null です。loop 設定は {data.loop} です。");
                }
                return data.loop;
            }

            Debug.LogWarning($"EnvironmentSoundType {type} に対応する環境音が見つかりません");
            return true;
        }
    }
}
