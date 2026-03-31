using UnityEngine;

namespace Blue.Entity
{
    public class AmarylisJerryFishView : BaseEntityView
    {
        [SerializeField] private SkinnedMeshRenderer skinnedMeshRenderer;
        [SerializeField] private Animator animator;

        public SkinnedMeshRenderer Renderer => skinnedMeshRenderer;
    }
}