using UnityEngine;

namespace Blue.Audio
{
    public class SEPlayer : MonoBehaviour
    {
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioSource loopAudioSource;

        public bool IsLoopPlaying => loopAudioSource != null && loopAudioSource.isPlaying;

        public void Play(AudioClip clip)
        {
            if (clip == null || audioSource == null)
            {
                Debug.LogWarning("再生に必要な情報が不足しています。");
                return;
            }

            audioSource.PlayOneShot(clip);
        }

        public void PlayAt(AudioClip clip, Vector3 position, float min_distance, float max_distance)
        {
            if (clip == null)
            {
                Debug.LogWarning("3D SE の AudioClip が指定されていません。");
                return;
            }

            GameObject temp_obj = new GameObject($"SE_3D_{clip.name}");
            temp_obj.transform.position = position;

            AudioSource source = temp_obj.AddComponent<AudioSource>();
            source.outputAudioMixerGroup = audioSource?.outputAudioMixerGroup;
            source.spatialBlend = 1.0f;
            source.minDistance = min_distance;
            source.maxDistance = max_distance;
            source.rolloffMode = AudioRolloffMode.Logarithmic;

            source.PlayOneShot(clip);

            // clip.lengthが0の場合も考慮し、最低1秒は保証
            float destroyTime = Mathf.Max(clip.length, 1.0f);
            Destroy(temp_obj, destroyTime);
        }

        public void PlayLoop(AudioClip clip)
        {
            if (clip == null || loopAudioSource == null)
            {
                Debug.LogWarning("ループ再生に必要な情報が不足しています。");
                return;
            }

            if (loopAudioSource.isPlaying && loopAudioSource.clip == clip) return;

            loopAudioSource.clip = clip;
            loopAudioSource.loop = true;
            loopAudioSource.Play();
        }

        public void StopLoop()
        {
            if (loopAudioSource == null) return;

            loopAudioSource.Stop();
            loopAudioSource.clip = null;
        }
    }
}
