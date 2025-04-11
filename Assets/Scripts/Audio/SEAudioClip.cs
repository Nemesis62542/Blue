using UnityEngine;
using System.Collections.Generic;

namespace NFPS.Audio
{
    [CreateAssetMenu(fileName = "SEAudioClip", menuName = "NFPS/ScriptableObject/SEAudioClip")]
    public class SEAudioClip : ScriptableObject
    {
        [System.Serializable]
        private class SEData
        {
            public SEType type;
            public AudioClip clip;
            public float minDistance = 1.0f;
            public float maxDistance = 20.0f;
        }

        [SerializeField] private List<SEData> seList = new List<SEData>();

        private Dictionary<SEType, SEData> seDictionary;

        private void OnEnable()
        {
            seDictionary = new Dictionary<SEType, SEData>();
            foreach (SEData se in seList)
            {
                if (!seDictionary.ContainsKey(se.type))
                {
                    seDictionary.Add(se.type, se);
                }
            }
        }

        public AudioClip GetClip(SEType type)
        {
            if (seDictionary.TryGetValue(type, out SEData data))
            {
                return data.clip;
            }

            Debug.LogWarning($"SEType {type} の AudioClip が見つかりません");
            return null;
        }

        public float GetMinDistance(SEType type)
        {
            if (seDictionary.TryGetValue(type, out SEData data))
            {
                return data.minDistance;
            }

            return 1.0f;
        }

        public float GetMaxDistance(SEType type)
        {
            if (seDictionary.TryGetValue(type, out SEData data))
            {
                return data.maxDistance;
            }

            return 20.0f;
        }
    }
}
