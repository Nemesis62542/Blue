using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace Blue.Player
{
    [RequireComponent(typeof(DecalProjector))]
    public class ScannerEffectView : MonoBehaviour
    {
        [SerializeField] private DecalProjector projector;
        [SerializeField] private float startRadius = 0.00f;
        [SerializeField] private float endRadius   = 0.48f;
        [SerializeField] private float expandDuration = 0.60f;
        [SerializeField] private float holdTime       = 0.10f;
        [SerializeField] private float fadeDuration   = 0.25f;
        [SerializeField] private bool setAspectFromProjector = true;

        private static readonly int ID_RevealRadius = Shader.PropertyToID("_RevealRadius");
        private static readonly int ID_RevealFeather= Shader.PropertyToID("_RevealFeather");
        private static readonly int ID_Aspect       = Shader.PropertyToID("_Aspect");

        [SerializeField] private float revealFeather = 0.04f;
        [SerializeField] private Material material;

        private float elapsed = 0.0f;
        private bool isPlaying = false;
        private float lastRevealRadius = -1f;
        private float cachedDeltaTime;
        private int currentPhase = 0;

        private void Reset()
        {
            TryGetComponent(out projector);
        }

        private void Awake()
        {
            if (projector == null && !TryGetComponent(out projector))
            {
                Debug.LogError("[ScannerEffectView] DecalProjector is missing.", this);
                enabled = false;
                return;
            }

            if (material == null)
            {
                material = projector.material;
                if (material == null)
                {
                    Debug.LogError("[ScannerEffectView] Material is not assigned.", this);
                    enabled = false;
                    return;
                }
            }

            if (setAspectFromProjector)
            {
                Vector3 size = projector.size;
                float aspect = (size.y > 0.0f) ? (size.x / size.y) : 1.0f;
                material.SetFloat(ID_Aspect, aspect);
            }

            material.SetFloat(ID_RevealFeather, revealFeather);
            material.SetFloat(ID_RevealRadius, startRadius);
            projector.fadeFactor = 1.0f;
        }

        public void PlayOnce()
        {
            isPlaying = true;
            elapsed = 0.0f;
            currentPhase = 0;
            projector.fadeFactor = 1.0f;
            material.SetFloat(ID_RevealRadius, startRadius);
            lastRevealRadius = startRadius;
        }

        private void Update()
        {
            if (!isPlaying) return;

            cachedDeltaTime = Time.deltaTime;
            elapsed += cachedDeltaTime;

            switch (currentPhase)
            {
                case 0:
                    if (elapsed < expandDuration)
                    {
                        float t = elapsed / expandDuration;
                        float r = Mathf.Lerp(startRadius, endRadius, t);
                        if (Mathf.Abs(r - lastRevealRadius) > 0.001f)
                        {
                            material.SetFloat(ID_RevealRadius, r);
                            lastRevealRadius = r;
                        }
                    }
                    else
                    {
                        material.SetFloat(ID_RevealRadius, endRadius);
                        lastRevealRadius = endRadius;
                        currentPhase = 1;
                    }
                    break;

                case 1:
                    if (elapsed >= expandDuration + holdTime)
                    {
                        currentPhase = 2;
                    }
                    break;

                case 2:
                    float fade_elapsed = elapsed - (expandDuration + holdTime);
                    float t_fade = fade_elapsed / fadeDuration;

                    if (t_fade >= 1.0f)
                    {
                        isPlaying = false;
                        gameObject.SetActive(false);
                    }
                    else
                    {
                        projector.fadeFactor = 1.0f - t_fade;
                    }
                    break;
            }
        }
    }
}
