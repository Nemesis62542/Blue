using TMPro;
using UnityEngine;

namespace Blue.UI
{
    public class ScanUIElement : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI nameText;
        private Transform target;
        private Camera mainCamera;
        private float lifetime;
        private float elapsed;

        public bool IsActive => target != null && elapsed < lifetime;

        public void Initialize(Transform newTarget, string displayName, float duration)
        {
            target = newTarget;
            lifetime = duration;
            elapsed = 0f;

            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            nameText.text = displayName;
            gameObject.SetActive(true);
        }

        private void Update()
        {
            if (target == null || mainCamera == null)
            {
                Deactivate();
                return;
            }

            elapsed += Time.deltaTime;
            if (elapsed >= lifetime)
            {
                Deactivate();
                return;
            }

            Vector3 world_pos = target.position + Vector3.up;
            transform.position = world_pos;

            transform.forward = mainCamera.transform.forward;
        }

        public void Deactivate()
        {
            target = null;
            elapsed = 0f;
            gameObject.SetActive(false);
        }
    }
}