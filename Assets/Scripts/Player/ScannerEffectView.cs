using UnityEngine;
using UnityEngine.Rendering.Universal;

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
                material = projector.material; // このProjector専用のマテリアルインスタンス
                if (material == null)
                {
                    Debug.LogError("[ScannerEffectView] Material is not assigned.", this);
                    enabled = false;
                    return;
                }
            }

            if (setAspectFromProjector)
            {
                Vector3 size = projector.size; // (W,H,Depth)
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
            projector.fadeFactor = 1.0f;
            material.SetFloat(ID_RevealRadius, startRadius);
        }

        private void Update()
        {
            if (!isPlaying) return;

            if (elapsed < expandDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / expandDuration);
                float r = Mathf.Lerp(startRadius, endRadius, t);
                material.SetFloat(ID_RevealRadius, r);
                return;
            }

            if (elapsed < expandDuration + holdTime)
            {
                elapsed += Time.deltaTime;
                return;
            }

            float tFade = (elapsed - (expandDuration + holdTime)) / Mathf.Max(fadeDuration, 1e-5f);
            projector.fadeFactor = 1.0f - Mathf.Clamp01(tFade);
            elapsed += Time.deltaTime;

            if (tFade >= 1.0f)
            {
                isPlaying = false;
                gameObject.SetActive(false);
            }
        }
    }
}
