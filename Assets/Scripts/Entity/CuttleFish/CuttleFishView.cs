using UnityEngine;
using DG.Tweening;

namespace Blue.Entity
{
    public class CuttleFishView : BaseEntityView
    {
        [SerializeField] private SkinnedMeshRenderer skinnedMeshRenderer;
        [SerializeField] private Animator animator;
        [SerializeField, ColorUsage(false, true)] private Color dimEmissionColor;
        [SerializeField, ColorUsage(false, true)] private Color brightEmissionColor;
        [SerializeField] private ParticleSystem inkEffect;

        private Material cachedMaterial;
        private Tween emissionTween;

        public SkinnedMeshRenderer Renderer => skinnedMeshRenderer;
        public Animator Animator => animator;

        private void Awake()
        {
            Initialize();
        }

        private void Initialize()
        {
            cachedMaterial = skinnedMeshRenderer.material;

            if (cachedMaterial.HasProperty("_EmissionColor"))
            {
                cachedMaterial.EnableKeyword("_EMISSION");
                cachedMaterial.SetColor("_EmissionColor", dimEmissionColor);
            }
        }

        public void SetEmissionColor(Color color)
        {
            if (cachedMaterial.HasProperty("_EmissionColor"))
            {
                cachedMaterial.SetColor("_EmissionColor", color);
            }
        }

        public void DisableEmission()
        {
            if (cachedMaterial.HasProperty("_EmissionColor"))
            {
                cachedMaterial.DisableKeyword("_EMISSION");
            }
        }

        public void SetEmissionColorDim(float duration = 1f)
        {
            TweenEmissionColor(dimEmissionColor, duration);
        }

        public void SetEmissionColorBright(float duration = 1f)
        {
            TweenEmissionColor(brightEmissionColor, duration);
        }

        public void TweenEmissionColor(Color color, float duration)
        {
            if (!cachedMaterial.HasProperty("_EmissionColor")) return;

            Color start_color = cachedMaterial.GetColor("_EmissionColor");

            emissionTween?.Kill();

            float t = 0f;
            emissionTween = DOTween.To(() => t, x =>
            {
                t = x;
                Color lerped = Color.Lerp(start_color, color, t);
                cachedMaterial.SetColor("_EmissionColor", lerped);
            }, 1f, duration).SetEase(Ease.Linear);
        }

        public void SetAnimatorIntimidate(bool is_intimidate)
        {
            animator.SetBool("Intimidate", is_intimidate);
        }
        
        public void PlayInkEffect()
        {
            if (!inkEffect.isPlaying) inkEffect.Play();
        }
    }
}