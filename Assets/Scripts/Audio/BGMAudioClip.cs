using UnityEngine;
using System.Collections.Generic;

namespace NFPS.Audio
{
    [CreateAssetMenu(fileName = "BGMAudioClip", menuName = "NFPS/ScriptableObject/BGMAudioClip")]
    public class BGMAudioClip : ScriptableObject
    {
        [System.Serializable]
        private class BGMData
        {
            public BGMType type;
            public AudioClip clip;
            public bool loop = true;
        }

        [SerializeField] private List<BGMData> bgmList = new List<BGMData>();

        private Dictionary<BGMType, BGMData> bgmDictionary;

        private void OnEnable()
        {
            bgmDictionary = new Dictionary<BGMType, BGMData>();
            foreach (BGMData bgm in bgmList)
            {
                if (!bgmDictionary.ContainsKey(bgm.type))
                {
                    bgmDictionary.Add(bgm.type, bgm);
                }
            }
        }

        public AudioClip GetClip(BGMType type)
        {
            if (bgmDictionary.TryGetValue(type, out BGMData data))
            {
                return data.clip;
            }

            Debug.LogWarning($"BGMType {type} の AudioClip が見つかりません");
            return null;
        }

        public bool GetLoop(BGMType type)
        {
            if (bgmDictionary.TryGetValue(type, out BGMData data))
            {
                if (data.clip == null)
                {
                    Debug.LogWarning($"BGMType {type} に設定された AudioClip が null です。loop 設定は {data.loop} です。");
                }
                return data.loop;
            }

            Debug.LogWarning($"BGMType {type} に対応する BGM が見つかりません");
            return true;
        }
    }
}
