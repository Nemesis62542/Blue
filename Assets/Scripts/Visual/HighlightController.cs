using UnityEngine;

namespace Blue.Visual
{
    public class HighlightController : MonoBehaviour
    {
        [SerializeField] private Renderer targetRenderer;
        [SerializeField] private Material highlightMaterial;

        private Material[] baseMaterials;
        private Material[] highlightedMaterials;
        private bool isHighlighted = false;

        private void Awake()
        {
            baseMaterials = (Material[])targetRenderer.sharedMaterials.Clone();

            highlightedMaterials = new Material[baseMaterials.Length + 1];
            baseMaterials.CopyTo(highlightedMaterials, 0);
            highlightedMaterials[baseMaterials.Length] = highlightMaterial;
        }

        public void EnableHighlight()
        {
            if (isHighlighted) return;

            targetRenderer.sharedMaterials = highlightedMaterials;
            isHighlighted = true;
        }

        public void DisableHighlight()
        {
            if (!isHighlighted) return;

            targetRenderer.sharedMaterials = baseMaterials;
            isHighlighted = false;
        }
    }
}