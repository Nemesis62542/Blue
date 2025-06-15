using UnityEngine;

namespace Blue.UI
{
    public class MessageView : MonoBehaviour
    {
        [SerializeField] private MessageSlot messagePrefab;
        [SerializeField] private float messageDuration = 2.0f;

        public void ShowMessage(MessageData data)
        {
            MessageSlot slot = Instantiate(messagePrefab, transform);
            slot.Initialize(data);

            Destroy(slot.gameObject, messageDuration);
        }

        public void ShowMessage(string message)
        {
            ShowMessage(new MessageData(message));
        }
    }

    public class MessageData
    {
        public string Text { get; private set; }
        public Sprite Icon { get; private set; }
        public Color Color { get; private set; }

        public MessageData(string text, Sprite icon = null, Color? color = null)
        {
            Text = text;
            Icon = icon;
            Color = color ?? new Color(1, 0.7019608f, 0.2784314f);
        }
    }
}
