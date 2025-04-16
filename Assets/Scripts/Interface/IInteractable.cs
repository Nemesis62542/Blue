using UnityEngine;

namespace Blue.Object
{
    public interface IInteractable
    {
        string ObjectName { get; }
        void Interact(MonoBehaviour interactor);
    }
}