using Blue.UI.Common;
using UnityEngine;

namespace Blue.Visual
{
    public class HighlightController : MonoBehaviour
    {
        private static readonly int FresnelColorId = Shader.PropertyToID("_BaseEmissionColor");

        [SerializeField] private Renderer targetRenderer;
        [SerializeField] private Material highlightMaterial;

        [Header("Threat Colors (HDR)")]
        [ColorUsage(true, true)]
        [SerializeField] private Color safetyColor = new Color(0f, 2f, 0.5f, 1f);
        [ColorUsage(true, true)]
        [SerializeField] private Color warningColor = new Color(2f, 1.5f, 0f, 1f);
        [ColorUsage(true, true)]
        [SerializeField] private Color dangerColor = new Color(2f, 0f, 0f, 1f);
        [ColorUsage(true, true)]
        [SerializeField] private Color defaultColor = new Color(0f, 1f, 2f, 1f);

        private Material[] highlightedMaterials;
        private MaterialPropertyBlock propertyBlock;

        // MecaSharkView等で死亡時にマテリアル差し替えのため公開
        public Material[] baseMaterials;
        private bool isHighlighted = false;

        private void Awake()
        {
            baseMaterials = (Material[])targetRenderer.sharedMaterials.Clone();

            highlightedMaterials = new Material[baseMaterials.Length + 1];
            baseMaterials.CopyTo(highlightedMaterials, 0);
            highlightedMaterials[baseMaterials.Length] = highlightMaterial;

            propertyBlock = new MaterialPropertyBlock();
        }

        public void EnableHighlight()
        {
            if (isHighlighted) return;

            targetRenderer.sharedMaterials = highlightedMaterials;
            isHighlighted = true;
        }

        public void EnableHighlight(ScanData.Threat threat)
        {
            if (!isHighlighted)
            {
                targetRenderer.sharedMaterials = highlightedMaterials;
                isHighlighted = true;
            }

            Color color = GetColorForThreat(threat);
            int highlightMaterialIndex = highlightedMaterials.Length - 1;

            targetRenderer.GetPropertyBlock(propertyBlock, highlightMaterialIndex);
            propertyBlock.SetColor(FresnelColorId, color);
            targetRenderer.SetPropertyBlock(propertyBlock, highlightMaterialIndex);
        }

        public void DisableHighlight()
        {
            if (!isHighlighted) return;

            targetRenderer.sharedMaterials = baseMaterials;
            isHighlighted = false;
        }

        private Color GetColorForThreat(ScanData.Threat threat)
        {
            return threat switch
            {
                ScanData.Threat.Safety => safetyColor,
                ScanData.Threat.Warning => warningColor,
                ScanData.Threat.Danger => dangerColor,
                _ => defaultColor
            };
        }
    }
}