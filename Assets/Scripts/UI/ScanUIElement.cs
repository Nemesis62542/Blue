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

        public void Initialize(Transform target, string displayName, float duration)
        {
            this.target = target;
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

            Vector3 viewport_position = Camera.main.WorldToViewportPoint(target.position);
            Vector3 screen_position = new Vector3(
                viewport_position.x * UnityEngine.Screen.width,
                viewport_position.y * UnityEngine.Screen.height,
                0
            );

            transform.position = screen_position;
        }

        public void Deactivate()
        {
            elapsed = 0f;
            Destroy(gameObject);
        }
    }
}