using UnityEngine;

/// <summary>
/// PC画面のMeshにUIをRenderTextureで表示するためのセットアップを行う
/// RenderTextureとMaterialを動的に生成するため、アセットの量産が不要
/// </summary>
public class ScreenSetup : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] private Camera uiCamera;

    [Header("Screen Settings")]
    [SerializeField] private Renderer screenRenderer;
    [SerializeField] private int materialIndex = 0;

    [Header("RenderTexture Settings")]
    [SerializeField] private Vector2Int resolution = new Vector2Int(1920, 1080);
    [SerializeField] private int antiAliasing = 1; // 1 = disabled, must match URP settings

    [Header("Material Settings")]
    [SerializeField] private Material baseMaterial;
    [SerializeField] private string texturePropertyName = "_MainTex";

    private RenderTexture renderTexture;
    private Material materialInstance;

    private void Awake()
    {
        Setup();
    }

    private void Setup()
    {
        CreateRenderTexture();
        SetupCamera();
        SetupScreenMaterial();
    }

    private void CreateRenderTexture()
    {
        renderTexture = new RenderTexture(resolution.x, resolution.y, 24) // Depth buffer required for URP
        {
            antiAliasing = antiAliasing,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        renderTexture.Create();
    }

    private void SetupCamera()
    {
        if (uiCamera == null)
        {
            Debug.LogError($"[ScreenSetup] UI Camera is not assigned on {gameObject.name}");
            return;
        }

        uiCamera.targetTexture = renderTexture;
    }

    private void SetupScreenMaterial()
    {
        if (screenRenderer == null)
        {
            Debug.LogError($"[ScreenSetup] Screen Renderer is not assigned on {gameObject.name}");
            return;
        }

        if (baseMaterial == null)
        {
            Debug.LogError($"[ScreenSetup] Base Material is not assigned on {gameObject.name}");
            return;
        }

        materialInstance = new Material(baseMaterial);
        materialInstance.SetTexture(texturePropertyName, renderTexture);

        Material[] materials = screenRenderer.materials;
        if (materialIndex < materials.Length)
        {
            materials[materialIndex] = materialInstance;
            screenRenderer.materials = materials;
        }
        else
        {
            Debug.LogError($"[ScreenSetup] Material index {materialIndex} is out of range on {gameObject.name}");
        }
    }

    private void OnDestroy()
    {
        if (renderTexture != null)
        {
            renderTexture.Release();
            Destroy(renderTexture);
        }

        if (materialInstance != null)
        {
            Destroy(materialInstance);
        }
    }

    /// <summary>
    /// 解像度を変更する（実行時に呼び出し可能）
    /// </summary>
    public void SetResolution(Vector2Int newResolution)
    {
        resolution = newResolution;

        if (renderTexture != null)
        {
            renderTexture.Release();
            renderTexture.width = resolution.x;
            renderTexture.height = resolution.y;
            renderTexture.Create();
        }
    }
}
