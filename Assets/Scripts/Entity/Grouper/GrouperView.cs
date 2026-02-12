using Blue.Visual;
using UnityEngine;

namespace Blue.Entity
{
    public class GrouperView : BaseEntityView
    {
        [SerializeField] private SkinnedMeshRenderer skinnedMeshRenderer;
        [SerializeField] private HighlightController highlightController;
        [SerializeField] private Animator animator;

        public SkinnedMeshRenderer Renderer => skinnedMeshRenderer;
        public void EnableHighlight() => highlightController.EnableHighlight();
        public void DisableHighlight() => highlightController.DisableHighlight();

        public void SetAnimatorBool(string param, bool value)
        {
            animator.SetBool(param, value);
        }

        public void SetAnimatorTrigger(string param)
        {
            animator.SetTrigger(param);
        }
    }
}