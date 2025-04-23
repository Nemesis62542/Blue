using UnityEngine;

namespace Blue.Visual
{
    public class HighlightController : MonoBehaviour
    {
        [SerializeField] private Renderer targetRenderer;
        [SerializeField] private Material highlightMaterial;

        private Material[] baseMaterials;
        private bool isHighlighted = false;

        private void Awake()
        {
            baseMaterials = targetRenderer.materials;
        }

        public void EnableHighlight()
        {
            if (isHighlighted) return;

            Material[] new_materials = new Material[baseMaterials.Length + 1];
            baseMaterials.CopyTo(new_materials, 0);
            new_materials[baseMaterials.Length] = highlightMaterial;
            targetRenderer.materials = new_materials;

            isHighlighted = true;
        }

        public void DisableHighlight()
        {
            if (!isHighlighted) return;

            targetRenderer.materials = baseMaterials;
            isHighlighted = false;
        }
    }
}