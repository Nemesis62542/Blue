using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace Blue.UI
{
    public class MessageSlot : MonoBehaviour
    {
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private Image icon;

        public void Initialize(MessageData data)
        {
            if (messageText != null)
            {
                messageText.text = data.Text;
                messageText.color = data.Color;
            }

            if (data.Icon != null)
            {
                icon.sprite = data.Icon;
                icon.gameObject.SetActive(true);
            }
            else
            {
                icon.gameObject.SetActive(false);
            }
        }
    }
}
