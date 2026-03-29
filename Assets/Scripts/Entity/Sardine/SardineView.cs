using UnityEngine;

namespace Blue.Entity
{
    public class SardineView : BaseEntityView
    {
        [SerializeField] private SkinnedMeshRenderer skinnedMeshRenderer;

        public SkinnedMeshRenderer Renderer => skinnedMeshRenderer;
    }
}