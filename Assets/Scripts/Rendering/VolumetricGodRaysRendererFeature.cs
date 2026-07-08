using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 水中の光芒（ゴッドレイ）をレイマーチングで描画するRendererFeature
/// カメラから各ピクセルへレイを飛ばし、シャドウマップをサンプリングしながら散乱光を積算する
/// 水面へライト方向に投影した位置のVoronoiノイズで光筋を揺らす
/// </summary>
public class VolumetricGodRaysRendererFeature : ScriptableRendererFeature
{
    /// <summary>
    /// レイマーチングの解像度（値はビットシフト量）
    /// </summary>
    public enum DownsampleMode
    {
        Half = 1,
        Quarter = 2,
    }

    [System.Serializable]
    public class GodRaysSettings
    {
        [Header("Ray Marching")]
        [Tooltip("レイマーチングのステップ数（多いほど高品質だが高負荷）")]
        [Range(8, 64)]
        public int stepCount = 24;

        [Tooltip("レイの最大距離（ShadowDistance以下を推奨）")]
        [Range(10f, 100f)]
        public float maxRayDistance = 50f;

        [Tooltip("レイマーチングの解像度（Half=1/2、Quarter=1/4）")]
        public DownsampleMode downsample = DownsampleMode.Quarter;

        [Header("Scattering")]
        [Tooltip("光芒の強度")]
        [Range(0f, 10f)]
        public float intensity = 1f;

        [Tooltip("散乱の密度")]
        [Range(0f, 5f)]
        public float density = 1f;

        [Tooltip("散乱の異方性（大きいほど太陽方向を見たときに光芒が強まる）")]
        [Range(0f, 0.9f)]
        public float anisotropy = 0.6f;

        [Tooltip("深度による減衰（大きいほど深い場所に光が届かなくなる）")]
        [Range(0f, 1f)]
        public float depthFalloff = 0.05f;

        [Tooltip("光芒の色合い")]
        public Color tint = Color.white;

        [Header("Shaft Noise")]
        [Tooltip("光筋ノイズのVoronoiセルサイズ（小さいほど細かい光筋）")]
        [Range(0.5f, 20f)]
        public float noiseCellSize = 4f;

        [Tooltip("光筋ノイズの影響度（0=均一な霧、1=くっきりした光筋）")]
        [Range(0f, 1f)]
        public float noiseInfluence = 0.8f;

        [Tooltip("第1層のアニメーション速度")]
        public Vector2 noiseSpeed1 = new Vector2(0.5f, 0.3f);

        [Tooltip("第2層のアニメーション速度")]
        public Vector2 noiseSpeed2 = new Vector2(-0.4f, 0.5f);

        [Tooltip("光筋ノイズのコントラスト（値が大きいほど鋭い光筋）")]
        [Range(0.5f, 5f)]
        public float noisePower = 2f;

        [Header("Pixelate")]
        [Tooltip("光のラインのピクセル化（0=無効、画面縦方向のブロック数。値が小さいほど粗いドット）")]
        [Range(0f, 480f)]
        public float pixelCount = 0f;

        [Header("Water")]
        [Tooltip("水面のY座標")]
        public float waterSurfaceY = 306f;

        [Tooltip("水面のY座標を自動取得するTransform（設定時はwaterSurfaceYを上書き）")]
        public Transform waterSurfaceTransform;

        [Header("Blur")]
        [Tooltip("ブラーを有効にする（ディザによるノイズを平滑化）")]
        public bool blurEnabled = true;

        [Header("Rendering")]
        [Tooltip("描画タイミング（水面は透明シェーダーなのでAfterRenderingTransparents推奨）")]
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
    }

    public GodRaysSettings settings = new GodRaysSettings();

    private VolumetricGodRaysRenderPass m_RenderPass;
    private Material m_Material;

    private const string ShaderName = "Hidden/VolumetricGodRays";

    public override void Create()
    {
        Shader shader = Shader.Find(ShaderName);
        if (shader == null)
        {
            Debug.LogError($"VolumetricGodRaysRendererFeature: Shader '{ShaderName}' not found.");
            return;
        }

        m_Material = CoreUtils.CreateEngineMaterial(shader);
        m_RenderPass = new VolumetricGodRaysRenderPass(m_Material);
        m_RenderPass.renderPassEvent = settings.renderPassEvent;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (m_Material == null || m_RenderPass == null)
            return;

        // プレビューカメラや反射カメラでは実行しない
        CameraType cameraType = renderingData.cameraData.cameraType;
        if (cameraType == CameraType.Preview || cameraType == CameraType.Reflection)
            return;

        // 水面Transformが設定されていれば、Y座標を自動取得
        float waterY = settings.waterSurfaceY;
        if (settings.waterSurfaceTransform != null)
        {
            waterY = settings.waterSurfaceTransform.position.y;
        }

        m_RenderPass.Setup(settings, waterY);
        m_RenderPass.renderPassEvent = settings.renderPassEvent;

        renderer.EnqueuePass(m_RenderPass);
    }

    protected override void Dispose(bool disposing)
    {
        m_RenderPass?.Dispose();
        CoreUtils.Destroy(m_Material);
    }
}
