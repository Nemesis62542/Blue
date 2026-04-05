using UnityEngine;

namespace Blue.Entity
{
    public class FrilledSharkView : BaseEntityView
    {
        [SerializeField] private SkinnedMeshRenderer skinnedMeshRenderer;
        [SerializeField] private Animator animator;

        public SkinnedMeshRenderer Renderer => skinnedMeshRenderer;

        public void SetAnimatorSwim(bool is_swim)
        {
            animator.SetBool("Swim", is_swim);
        }
    }
}