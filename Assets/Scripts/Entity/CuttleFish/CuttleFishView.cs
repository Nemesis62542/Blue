using UnityEngine;

namespace Blue.Entity
{
    public class CuttleFishView : BaseEntityView
    {
        [SerializeField] private SkinnedMeshRenderer skinnedMeshRenderer;
        [SerializeField] private Animator animator;

        public SkinnedMeshRenderer Renderer => skinnedMeshRenderer;
        public Animator Animator => animator;
    }
}