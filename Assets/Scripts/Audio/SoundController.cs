using UnityEngine;

namespace Blue.Audio
{
    public class SoundController : MonoBehaviour
    {
        private static SoundController instance;

        [SerializeField] private BGMPlayer bgmPlayer;
        [SerializeField] private SEPlayer sePlayer;
        [SerializeField] private BGMAudioClip bgmAudioClip;
        [SerializeField] private SEAudioClip seAudioClip;

        public static SoundController Instance => instance;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void PlayBGM(BGMType type)
        {
            PlayBGM(type, 0f);
        }

        public void PlayBGM(BGMType type, float fade_time = 0f)
        {
            if (!ValidateBGM()) return;

            AudioClip clip = bgmAudioClip.GetClip(type);
            bool loop = bgmAudioClip.GetLoop(type);

            if (clip == null) return;
            if (clip == bgmPlayer.CurrentClip) return;

            bgmPlayer.Play(clip, loop, fade_time);
        }

        public void PlaySE(SEType type)
        {
            if (!ValidateSE()) return;

            AudioClip clip = seAudioClip.GetClip(type);
            sePlayer.Play(clip);
        }

        public void PlaySE(SEType type, Vector3 position)
        {
            if (!ValidateSE()) return;

            AudioClip clip = seAudioClip.GetClip(type);
            float min = seAudioClip.GetMinDistance(type);
            float max = seAudioClip.GetMaxDistance(type);

            sePlayer.PlayAt(clip, position, min, max);
        }

        public void StopBGM(float fade_time = 0f)
        {
            if (!ValidateBGM()) return;
            bgmPlayer.Stop(fade_time);
        }

        private bool ValidateBGM()
        {
            if (bgmPlayer == null || bgmAudioClip == null)
            {
                Debug.LogError("BGMPlayer または BGMAudioClip が設定されていません。");
                return false;
            }
            return true;
        }

        private bool ValidateSE()
        {
            if (seAudioClip == null || sePlayer == null)
            {
                Debug.LogError("SEPlayer または SEAudioClip が設定されていません。");
                return false;
            }
            return true;
        }
    }
}
