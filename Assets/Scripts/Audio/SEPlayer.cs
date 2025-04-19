using UnityEngine;

namespace Blue.Audio
{
    public class SEPlayer : MonoBehaviour
    {
        [SerializeField] private AudioSource audioSource;

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
            source.outputAudioMixerGroup = audioSource.outputAudioMixerGroup;
            source.spatialBlend = 1.0f;
            source.minDistance = min_distance;
            source.maxDistance = max_distance;
            source.rolloffMode = AudioRolloffMode.Logarithmic;

            source.PlayOneShot(clip);
            Destroy(temp_obj, clip.length);
        }
    }
}
