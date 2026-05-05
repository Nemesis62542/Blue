using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

/// <summary>
/// コースティクス描画用のRenderPass（RenderGraph対応）
/// </summary>
public class CausticsRenderPass : ScriptableRenderPass
{
    private const string PassName = "Caustics";

    private Material m_Material;
    private CausticsRendererFeature.CausticsSettings m_Settings;
    private float m_WaterSurfaceY;

    // シェーダープロパティID
    private static readonly int LayerCountID = Shader.PropertyToID("_LayerCount");
    private static readonly int CellSizeID = Shader.PropertyToID("_CellSize");
    private static readonly int JitterID = Shader.PropertyToID("_Jitter");
    private static readonly int IntensityID = Shader.PropertyToID("_CausticsIntensity");
    private static readonly int PixelCountID = Shader.PropertyToID("_PixelCount");
    private static readonly int Speed1ID = Shader.PropertyToID("_Speed1");
    private static readonly int Speed2ID = Shader.PropertyToID("_Speed2");
    private static readonly int MaxDepthID = Shader.PropertyToID("_CausticsMaxDepth");
    private static readonly int WaterSurfaceYID = Shader.PropertyToID("_WaterSurfaceY");
    private static readonly int FadeDistanceID = Shader.PropertyToID("_FadeDistance");
    private static readonly int FadePowerID = Shader.PropertyToID("_FadePower");

    public CausticsRenderPass(Material material)
    {
        m_Material = material;
        profilingSampler = new ProfilingSampler(PassName);

        // 深度テクスチャが必要
        ConfigureInput(ScriptableRenderPassInput.Depth);

        // 中間テクスチャが必要（BackBufferを直接読むことはできない）
        requiresIntermediateTexture = true;
    }

    public void Setup(CausticsRendererFeature.CausticsSettings settings, float waterSurfaceY)
    {
        m_Settings = settings;
        m_WaterSurfaceY = waterSurfaceY;
    }

    public void Dispose()
    {
    }

    private void UpdateMaterialProperties()
    {
        if (m_Material == null || m_Settings == null)
            return;

        m_Material.SetInt(LayerCountID, m_Settings.layerCount);
        m_Material.SetFloat(CellSizeID, m_Settings.cellSize);
        m_Material.SetFloat(JitterID, m_Settings.jitter);
        m_Material.SetFloat(IntensityID, m_Settings.intensity);
        m_Material.SetFloat(PixelCountID, m_Settings.pixelCount);
        m_Material.SetVector(Speed1ID, new Vector4(m_Settings.speed1.x, m_Settings.speed1.y, 0, 0));
        m_Material.SetVector(Speed2ID, new Vector4(m_Settings.speed2.x, m_Settings.speed2.y, 0, 0));
        m_Material.SetFloat(MaxDepthID, m_Settings.maxDepth);
        m_Material.SetFloat(WaterSurfaceYID, m_WaterSurfaceY);
        m_Material.SetFloat(FadeDistanceID, m_Settings.fadeDistance);
        m_Material.SetFloat(FadePowerID, m_Settings.fadePower);
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

        // BackBufferは直接読み取れないのでスキップ
        if (resourceData.isActiveTargetBackBuffer)
            return;

        // マテリアルプロパティを更新
        UpdateMaterialProperties();

        // ソーステクスチャ
        TextureHandle source = resourceData.activeColorTexture;

        // デスティネーションテクスチャを作成
        TextureDesc destinationDesc = renderGraph.GetTextureDesc(source);
        destinationDesc.name = $"CameraColor-{PassName}";
        destinationDesc.clearBuffer = false;
        TextureHandle destination = renderGraph.CreateTexture(destinationDesc);

        // Blitパスを追加
        RenderGraphUtils.BlitMaterialParameters blitParams = new RenderGraphUtils.BlitMaterialParameters(source, destination, m_Material, 0);
        renderGraph.AddBlitPass(blitParams, passName: PassName);

        // カメラカラーを更新（次のパスで使われる）
        resourceData.cameraColor = destination;
    }
}
