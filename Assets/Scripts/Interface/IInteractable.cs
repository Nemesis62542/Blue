using UnityEngine;

namespace NFPS.Object
{
    public interface IInteractable
    {
        string ObjectName { get; }
        void Interact(MonoBehaviour interactor);
    }
}