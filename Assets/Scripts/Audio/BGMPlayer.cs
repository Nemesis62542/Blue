using UnityEngine;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Blue.Audio
{
    public class BGMPlayer : MonoBehaviour
    {
        [SerializeField] private AudioSource audioSource;

        private CancellationTokenSource fadeCts;

        public AudioClip CurrentClip => audioSource != null ? audioSource.clip : null;

        public void Play(AudioClip clip, bool loop, float fade_time)
        {
            if (audioSource == null)
            {
                Debug.LogError("BGMPlayer: AudioSource が設定されていません。");
                return;
            }

            if (isFading)
            {
                Debug.LogWarning($"BGMPlayer: フェード中に再生が要求されました（Clip: {clip?.name ?? "null"}）。処理をスキップします。");
                return;
            }

            audioSource.loop = loop;

            if (fade_time <= 0f)
            {
                audioSource.Stop();
                audioSource.clip = clip;
                audioSource.volume = 1.0f;
                audioSource.Play();
                return;
            }

            if (audioSource.isPlaying)
            {
                StartFadeCoroutine(FadeOutAndPlayNewClip(clip, fade_time));
            }
            else
            {
                StartFadeCoroutine(FadeIn(clip, fade_time));
            }
        }

        public void Stop(float fade_time)
        {
            if (audioSource == null)
            {
                Debug.LogError("BGMPlayer: AudioSource が設定されていません。");
                return;
            }

            if (isFading)
            {
                Debug.LogWarning("BGMPlayer: フェード中に停止が要求されました。処理をスキップします。");
                return;
            }

            if (fade_time <= 0f)
            {
                audioSource.Stop();
                return;
            }

            StartFadeCoroutine(FadeOut(fade_time));
        }

        private void StartFadeCoroutine(IEnumerator routine)
        {
            if (isFading)
            {
                Debug.LogWarning("BGMPlayer: フェード処理が重複しようとしました。スキップします。");
                return;
            }

            fadeCoroutine = StartCoroutine(FadeWrapper(routine));
        }

        private IEnumerator FadeWrapper(IEnumerator routine)
        {
            isFading = true;
            yield return StartCoroutine(routine);
            isFading = false;
        }

        private async UniTask FadeOutAsync(float duration, CancellationToken ct)
        {
            float startVolume = audioSource.volume;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                audioSource.volume = Mathf.Lerp(startVolume, 0, elapsed / duration);
                elapsed += Time.deltaTime;
                await UniTask.Yield(ct);
            }

            audioSource.volume = 0;
            audioSource.Stop();
        }

        private async UniTask FadeInAsync(AudioClip clip, float duration, CancellationToken ct)
        {
            audioSource.clip = clip;
            audioSource.volume = 0;
            audioSource.Play();

            float elapsed = 0f;

            while (elapsed < duration)
            {
                audioSource.volume = Mathf.Lerp(0, 1, elapsed / duration);
                elapsed += Time.deltaTime;
                await UniTask.Yield(ct);
            }

            audioSource.volume = 1;
        }

        private async UniTask FadeOutAndPlayNewClipAsync(AudioClip new_clip, float duration, CancellationToken ct)
        {
            await FadeOutAsync(duration, ct);
            await FadeInAsync(new_clip, duration, ct);
        }
    }
}