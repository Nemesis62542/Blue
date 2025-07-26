using UnityEngine;
using TMPro;
using DG.Tweening;
using System.Collections;

namespace Blue.UI
{
    public class SubtitleUIController : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private float typeSpeed = 0.05f;
        [SerializeField] private float fadeDuration = 0.3f;
        [SerializeField] private float autoFadeTime = 4f;

        private Tween typeTween;
        private Coroutine typeCoroutine;
        private Coroutine fadeCoroutine;
        private float lastUpdateTime = -1f;

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

        public void ShowMessage(string message)
        {
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            if (typeCoroutine != null) StopCoroutine(typeCoroutine);
            if (typeTween != null && typeTween.IsActive()) typeTween.Kill();

            canvasGroup.DOFade(1f, fadeDuration);
            messageText.text = message;
            messageText.maxVisibleCharacters = 0;

            lastUpdateTime = Time.time;
            typeCoroutine = StartCoroutine(TypeTextRoutine(message));
            fadeCoroutine = StartCoroutine(AutoFadeOutRoutine());
        }

        private IEnumerator TypeTextRoutine(string message)
        {
            int total_chars = message.Length;
            float type_duration = total_chars * typeSpeed;

            typeTween = DOTween.To(
                () => messageText.maxVisibleCharacters,
                x => messageText.maxVisibleCharacters = x,
                total_chars,
                type_duration
            );

            yield return typeTween.WaitForCompletion();
        }

        private IEnumerator AutoFadeOutRoutine()
        {
            while (true)
            {
                if (Time.time - lastUpdateTime > autoFadeTime)
                {
                    canvasGroup.DOFade(0f, fadeDuration);
                    yield break;
                }
                yield return null;
            }
        }
    }
}