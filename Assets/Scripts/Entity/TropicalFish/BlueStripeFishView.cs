using Blue.Visual;
using UnityEngine;

namespace Blue.Entity
{
    public class BlueStripeFishView : BaseEntityView
    {
        [SerializeField] private SkinnedMeshRenderer skinnedMeshRenderer;
        [SerializeField] private HighlightController highlightController;

        public SkinnedMeshRenderer Renderer => skinnedMeshRenderer;
        public void EnableHighlight() => highlightController.EnableHighlight();
        public void DisableHighlight() => highlightController.DisableHighlight();
    }
}