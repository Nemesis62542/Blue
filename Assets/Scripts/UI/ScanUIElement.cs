using TMPro;
using UnityEngine;

namespace Blue.UI
{
    public class ScanUIElement : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI nameText;
        private Transform target;

        public Transform Target => target;

        public void Initialize(Transform target, string display_name)
        {
            this.target = target;

            nameText.text = display_name;
            gameObject.SetActive(true);
        }
    }
}