using TMPro;
using UnityEngine;

namespace Blue.UI
{
    public class ScanUIElement : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private float revealHoldTime = 1.5f;

        private Transform target;
        private float lifeTime;
        private float elapsed;
        private string actualName;
        private bool isIdentified = false;
        private bool isHolding = false;
        private float holdTimer;

        public Transform Target => target;

        public void Initialize(Transform target, string display_name, float duration)
        {
            this.target = target;
            actualName = display_name;
            lifeTime = duration;
            elapsed = 0f;
            isIdentified = false;
            holdTimer = 0f;

            nameText.text = "？？？";
        }

        public void IncreaseElapsed()
        {
            elapsed += Time.deltaTime;

            if (!isIdentified && isHolding)
            {
                holdTimer += Time.deltaTime;
                if (holdTimer >= revealHoldTime)
                {
                    RevealName();
                }
            }

            if (elapsed >= lifeTime) Deactivate();
        }

        public void SetHolding(bool holding)
        {
            if (isIdentified) return;

            isHolding = holding;
            if (!holding) holdTimer = 0f;
        }

        private void RevealName()
        {
            isIdentified = true;
            nameText.text = actualName;
        }

        public void Deactivate()
        {
            Destroy(gameObject);
        }
    }
}