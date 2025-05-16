using Blue.Visual;
using UnityEngine;

namespace Blue.Entity
{
    public class CoelacanthView : BaseEntityView
    {
        [SerializeField] private SkinnedMeshRenderer skinnedMeshRenderer;
        [SerializeField] private Animator animator;
        [SerializeField] private HighlightController highlightController;

        public SkinnedMeshRenderer Renderer => skinnedMeshRenderer;
        public void EnableHighlight() => highlightController.EnableHighlight();
        public void DisableHighlight() => highlightController.DisableHighlight();

        public void SetAnimatorSwim(bool is_swim)
        {
            animator.SetBool("Swim", is_swim);
        }
    }
}