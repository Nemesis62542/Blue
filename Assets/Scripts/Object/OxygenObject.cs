using Blue.Inventory;
using Blue.Item;
using Blue.Player;
using Blue.UI;
using UnityEngine;

namespace Blue.Object
{
    public class OxygenObject : MonoBehaviour, IInteractable
    {
        [SerializeField] private string objectName;

        public string ObjectName => objectName;

        public void Interact(MonoBehaviour interactor)
        {
            if (interactor.TryGetComponent(out PlayerController player))
            {
                player.SetOxygenMax();
                MessageView.Instance.ShowMessage(new MessageData("酸素を補給しました"));
            }
        }
    }
}