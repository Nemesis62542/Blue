using UnityEngine;
using TMPro;
using DG.Tweening;

namespace Blue.UI
{
    public class SubtitleUIController : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private float defaultShowDuration = 3f;
        [SerializeField] private float typeSpeed = 0.05f;
        [SerializeField] private float fadeDuration = 0.3f;

        private Coroutine currentCoroutine;
        private Tween typeTween;
        private Tween fadeTween;

        public static SubtitleUIController Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            canvasGroup.alpha = 0f;
        }

        public void ShowMessage(string message, float duration = -1f)
        {
            if (currentCoroutine != null)
            {
                StopCoroutine(currentCoroutine);
            }

            if (typeTween != null && typeTween.IsActive()) typeTween.Kill();
            if (fadeTween != null && fadeTween.IsActive()) fadeTween.Kill();

            currentCoroutine = StartCoroutine(ShowRoutine(message, duration > 0f ? duration : defaultShowDuration));
        }

        private System.Collections.IEnumerator ShowRoutine(string message, float duration)
        {
            canvasGroup.alpha = 0f;
            fadeTween = canvasGroup.DOFade(1f, fadeDuration);
            yield return fadeTween.WaitForCompletion();

            messageText.text = message;
            messageText.maxVisibleCharacters = 0;

            int total_chars = message.Length;
            float type_duration = total_chars * typeSpeed;

            typeTween = DOTween.To(
                () => messageText.maxVisibleCharacters,
                x => messageText.maxVisibleCharacters = x,
                total_chars,
                type_duration
            );

            yield return typeTween.WaitForCompletion();

            yield return new WaitForSeconds(duration);

            fadeTween = canvasGroup.DOFade(0f, fadeDuration);
            yield return fadeTween.WaitForCompletion();

            currentCoroutine = null;
        }
    }
}