using UnityEngine;

namespace Blue.Entity
{
    public class DragonBabyView : BaseEntityView
    {
        [SerializeField] private SkinnedMeshRenderer skinnedMeshRenderer;
        [SerializeField] private Animator animator;

        public SkinnedMeshRenderer Renderer => skinnedMeshRenderer;

        public void SetAnimatorBool(string param, bool value)
        {
            animator.SetBool(param, value);
        }
    }
}