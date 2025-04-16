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

        public void SetEmissionToDim()
        {
            SetEmissionColor(dimEmissionColor);
        }

        public void SetEmissionToBright()
        {
            SetEmissionColor(brightEmissionColor);
        }

        public void TweenEmissionColor(Color targetColor, float duration)
        {
            if (!cachedMaterial.HasProperty("_EmissionColor")) return;

            Color start_color = cachedMaterial.GetColor("_EmissionColor");
            Debug.Log($"[Tween Start] from: {start_color} → to: {targetColor}");

            emissionTween?.Kill();

            float t = 0f;
            emissionTween = DOTween.To(() => t, x => {
                t = x;
                Color lerped = Color.Lerp(start_color, targetColor, t);
                cachedMaterial.SetColor("_EmissionColor", lerped);
            }, 1f, duration).SetEase(Ease.Linear);
        }
    }
}