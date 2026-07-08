using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

/// <summary>
/// ボリューメトリックゴッドレイ描画用のRenderPass（RenderGraph対応）
/// 低解像度でレイマーチング → セパラブルガウシアンブラー → フル解像度で合成の4パス構成
/// </summary>
public class VolumetricGodRaysRenderPass : ScriptableRenderPass
{
    private const string PassName = "VolumetricGodRays";

    private Material m_Material;
    private VolumetricGodRaysRendererFeature.GodRaysSettings m_Settings;
    private float m_WaterSurfaceY;

    // シェーダープロパティID
    private static readonly int StepCountID = Shader.PropertyToID("_StepCount");
    private static readonly int MaxRayDistanceID = Shader.PropertyToID("_MaxRayDistance");
    private static readonly int IntensityID = Shader.PropertyToID("_GodRaysIntensity");
    private static readonly int DensityID = Shader.PropertyToID("_Density");
    private static readonly int AnisotropyID = Shader.PropertyToID("_Anisotropy");
    private static readonly int DepthFalloffID = Shader.PropertyToID("_DepthFalloff");
    private static readonly int TintID = Shader.PropertyToID("_Tint");
    private static readonly int NoiseCellSizeID = Shader.PropertyToID("_NoiseCellSize");
    private static readonly int NoiseInfluenceID = Shader.PropertyToID("_NoiseInfluence");
    private static readonly int NoiseSpeedID = Shader.PropertyToID("_NoiseSpeed");
    private static readonly int NoisePowerID = Shader.PropertyToID("_NoisePower");
    private static readonly int WaterSurfaceYID = Shader.PropertyToID("_WaterSurfaceY");
    private static readonly int TexelSizeID = Shader.PropertyToID("_TexelSize");
    private static readonly int GodRaysTextureID = Shader.PropertyToID("_GodRaysTexture");

    // シェーダーパスインデックス
    private const int RaymarchPass = 0;
    private const int BlurHPass = 1;
    private const int BlurVPass = 2;
    private const int CompositePass = 3;

    private class CompositePassData
    {
        public TextureHandle source;
        public TextureHandle godRays;
        public Material material;
    }

    public VolumetricGodRaysRenderPass(Material material)
    {
        m_Material = material;
        profilingSampler = new ProfilingSampler(PassName);

        // 深度テクスチャが必要
        ConfigureInput(ScriptableRenderPassInput.Depth);

        // 中間テクスチャが必要（BackBufferを直接読むことはできない）
        requiresIntermediateTexture = true;
    }

    public void Setup(VolumetricGodRaysRendererFeature.GodRaysSettings settings, float waterSurfaceY)
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

        m_Material.SetFloat(StepCountID, m_Settings.stepCount);
        m_Material.SetFloat(MaxRayDistanceID, m_Settings.maxRayDistance);
        m_Material.SetFloat(IntensityID, m_Settings.intensity);
        m_Material.SetFloat(DensityID, m_Settings.density);
        m_Material.SetFloat(AnisotropyID, m_Settings.anisotropy);
        m_Material.SetFloat(DepthFalloffID, m_Settings.depthFalloff);
        m_Material.SetColor(TintID, m_Settings.tint);
        m_Material.SetFloat(NoiseCellSizeID, m_Settings.noiseCellSize);
        m_Material.SetFloat(NoiseInfluenceID, m_Settings.noiseInfluence);
        m_Material.SetVector(NoiseSpeedID, new Vector4(m_Settings.noiseSpeed.x, m_Settings.noiseSpeed.y, 0, 0));
        m_Material.SetFloat(NoisePowerID, m_Settings.noisePower);
        m_Material.SetFloat(WaterSurfaceYID, m_WaterSurfaceY);
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
        TextureDesc sourceDesc = renderGraph.GetTextureDesc(source);

        // 低解像度テクスチャ（R=散乱量, G=代表距離）
        int shift = (int)m_Settings.downsample;
        int lowWidth = Mathf.Max(1, sourceDesc.width >> shift);
        int lowHeight = Mathf.Max(1, sourceDesc.height >> shift);

        TextureDesc raysDesc = new TextureDesc(lowWidth, lowHeight)
        {
            name = $"{PassName}-Raymarch",
            format = GraphicsFormat.R16G16_SFloat,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            clearBuffer = false,
        };
        TextureHandle raysTexture = renderGraph.CreateTexture(raysDesc);

        // 低解像度テクセルサイズを明示的にセット（_BlitTexture_TexelSizeの自動セットは保証されない）
        m_Material.SetVector(TexelSizeID, new Vector4(1f / lowWidth, 1f / lowHeight, lowWidth, lowHeight));

        // 1. レイマーチングパス（低解像度）
        RenderGraphUtils.BlitMaterialParameters raymarchParams = new RenderGraphUtils.BlitMaterialParameters(source, raysTexture, m_Material, RaymarchPass);
        renderGraph.AddBlitPass(raymarchParams, passName: $"{PassName} Raymarch");

        // 2. セパラブルガウシアンブラー（低解像度ピンポン）
        TextureHandle finalRaysTexture = raysTexture;
        if (m_Settings.blurEnabled)
        {
            raysDesc.name = $"{PassName}-BlurH";
            TextureHandle blurHTexture = renderGraph.CreateTexture(raysDesc);
            RenderGraphUtils.BlitMaterialParameters blurHParams = new RenderGraphUtils.BlitMaterialParameters(raysTexture, blurHTexture, m_Material, BlurHPass);
            renderGraph.AddBlitPass(blurHParams, passName: $"{PassName} BlurH");

            raysDesc.name = $"{PassName}-BlurV";
            TextureHandle blurVTexture = renderGraph.CreateTexture(raysDesc);
            RenderGraphUtils.BlitMaterialParameters blurVParams = new RenderGraphUtils.BlitMaterialParameters(blurHTexture, blurVTexture, m_Material, BlurVPass);
            renderGraph.AddBlitPass(blurVParams, passName: $"{PassName} BlurV");

            finalRaysTexture = blurVTexture;
        }

        // 3. 合成パス（フル解像度、カメラカラー＋ゴッドレイの2入力なのでRasterRenderPassを使う）
        TextureDesc destinationDesc = renderGraph.GetTextureDesc(source);
        destinationDesc.name = $"CameraColor-{PassName}";
        destinationDesc.clearBuffer = false;
        TextureHandle destination = renderGraph.CreateTexture(destinationDesc);

        using (IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass($"{PassName} Composite", out CompositePassData passData, profilingSampler))
        {
            passData.source = source;
            passData.godRays = finalRaysTexture;
            passData.material = m_Material;

            builder.UseTexture(source);
            builder.UseTexture(finalRaysTexture);
            builder.SetRenderAttachment(destination, 0);

            builder.SetRenderFunc((CompositePassData data, RasterGraphContext context) =>
            {
                data.material.SetTexture(GodRaysTextureID, data.godRays);

                // TextureHandleはRTHandleとRenderTargetIdentifierの両方に暗黙変換できオーバーロードが曖昧になるため明示的に変換
                RTHandle sourceHandle = data.source;
                Blitter.BlitTexture(context.cmd, sourceHandle, new Vector4(1f, 1f, 0f, 0f), data.material, CompositePass);
            });
        }

        // カメラカラーを更新（次のパスで使われる）
        resourceData.cameraColor = destination;
    }
}
