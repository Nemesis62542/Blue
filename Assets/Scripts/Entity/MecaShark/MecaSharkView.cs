using Blue.Visual;
using UnityEngine;

namespace Blue.Entity
{
    public class MecaSharkView : BaseEntityView
    {
        [SerializeField] private SkinnedMeshRenderer skinnedMeshRenderer;
        [SerializeField] private Animator animator;
        [SerializeField] private HighlightController highlightController;

        public SkinnedMeshRenderer Renderer => skinnedMeshRenderer;
        public void EnableHighlight() => highlightController.EnableHighlight();
        public void DisableHighlight() => highlightController.DisableHighlight();

        public void LockOn(bool is_lock_on)
        {
            animator.SetBool("LockOn", is_lock_on);
        }
    }
}