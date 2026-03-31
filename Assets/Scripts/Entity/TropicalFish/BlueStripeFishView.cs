using UnityEngine;

namespace Blue.Entity
{
    public class BlueStripeFishView : BaseEntityView
    {
        [SerializeField] private SkinnedMeshRenderer skinnedMeshRenderer;

        public SkinnedMeshRenderer Renderer => skinnedMeshRenderer;
    }
}