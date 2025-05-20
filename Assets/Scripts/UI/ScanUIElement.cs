using TMPro;
using UnityEngine;

namespace Blue.UI
{
    public class ScanUIElement : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI nameText;
        private Transform target;
        private float lifeTime;
        private float elapsed;

        public Transform Target => target;

        public void Initialize(Transform target, string display_name, float duration)
        {
            this.target = target;
            lifeTime = duration;
            elapsed = 0f;

            nameText.text = display_name;
            gameObject.SetActive(true);
        }

        public void IncreaseElapsed()
        {
            elapsed += Time.deltaTime;
            if (elapsed >= lifeTime) Deactivate();
        }

        public void Deactivate()
        {
            elapsed = 0f;
            Destroy(gameObject);
        }
    }
}