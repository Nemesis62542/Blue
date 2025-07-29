using UnityEngine;

namespace Blue.Object
{
    public class BoxObject : MonoBehaviour, IInteractable
    {
        [SerializeField] private Animator animator;

        private string objectName;

        public string ObjectName => objectName;

        public void Interact(MonoBehaviour interactor)
        {
            gameObject.GetComponent<Collider>().enabled = false;
            animator.SetBool("Open", true);
        }
    }
}