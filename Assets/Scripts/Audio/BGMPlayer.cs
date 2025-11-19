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

        public async void Play(AudioClip clip, bool loop, float fade_time)
        {
            if (audioSource == null)
            {
                Debug.LogError("BGMPlayer: AudioSource が設定されていません。");
                return;
            }

            // 既存のフェード処理をキャンセル
            fadeCts?.Cancel();
            fadeCts?.Dispose();
            fadeCts = new CancellationTokenSource();

            audioSource.loop = loop;

            if (fade_time <= 0f)
            {
                audioSource.Stop();
                audioSource.clip = clip;
                audioSource.volume = 1.0f;
                audioSource.Play();
                return;
            }

            try
            {
                if (audioSource.isPlaying)
                {
                    await FadeOutAndPlayNewClipAsync(clip, fade_time, fadeCts.Token);
                }
                else
                {
                    await FadeInAsync(clip, fade_time, fadeCts.Token);
                }
            }
            catch (System.OperationCanceledException)
            {
                // キャンセルされた場合は正常終了
            }
        }

        public async void Stop(float fade_time)
        {
            if (audioSource == null)
            {
                Debug.LogError("BGMPlayer: AudioSource が設定されていません。");
                return;
            }

            // 既存のフェード処理をキャンセル
            fadeCts?.Cancel();
            fadeCts?.Dispose();
            fadeCts = new CancellationTokenSource();

            if (fade_time <= 0f)
            {
                audioSource.Stop();
                return;
            }

            try
            {
                await FadeOutAsync(fade_time, fadeCts.Token);
            }
            catch (System.OperationCanceledException)
            {
                // キャンセルされた場合は正常終了
            }
        }

        private void OnDestroy()
        {
            fadeCts?.Cancel();
            fadeCts?.Dispose();
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