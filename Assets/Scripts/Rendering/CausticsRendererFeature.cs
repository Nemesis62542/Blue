using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 水面下にコースティクス（光の集光パターン）を描画するRendererFeature
/// プロシージャルVoronoiノイズを使用してテクスチャ不要で実装
/// </summary>
public class CausticsRendererFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class CausticsSettings
    {
        [Header("Caustics Pattern")]
        [Tooltip("Voronoiの層の数（多いほど空洞が埋まる）")]
        [Range(2, 8)]
        public int layerCount = 3;

        [Tooltip("Voronoiセルのサイズ（小さいほど細かいパターン）")]
        [Range(0.05f, 5f)]
        public float cellSize = 0.5f;

        [Tooltip("セルのランダム性（0=グリッド、1=完全ランダム）")]
        [Range(0f, 1f)]
        public float jitter = 0.9f;

        [Tooltip("コースティクスの強度")]
        [Range(0f, 5f)]
        public float intensity = 1.0f;

        [Tooltip("ピクセル化の解像度（0=無効、値が大きいほど細かいドット）")]
        [Range(0f, 64f)]
        public float pixelCount = 0f;

        [Header("Animation")]
        [Tooltip("第1層のアニメーション速度")]
        public Vector2 speed1 = new Vector2(0.5f, 0.3f);

        [Tooltip("第2層のアニメーション速度")]
        public Vector2 speed2 = new Vector2(-0.4f, 0.5f);

        [Header("Depth Settings")]
        [Tooltip("コースティクスが届く最大深度（水面からの深さ）")]
        public float maxDepth = 30f;

        [Tooltip("水面のY座標")]
        public float waterSurfaceY = 0f;

        [Tooltip("水面のY座標を自動取得するTransform（設定時はwaterSurfaceYを上書き）")]
        public Transform waterSurfaceTransform;

        [Header("Distance Fade")]
        [Tooltip("カメラからの距離減衰の最大距離")]
        public float fadeDistance = 100f;

        [Tooltip("距離減衰の強さ（0=減衰なし、1=強い減衰）")]
        [Range(0f, 1f)]
        public float fadePower = 0.5f;

        [Header("Rendering")]
        [Tooltip("描画タイミング")]
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
    }

    public CausticsSettings settings = new CausticsSettings();

    private CausticsRenderPass m_RenderPass;
    private Material m_Material;

    private const string ShaderName = "Hidden/Caustics";

    public override void Create()
    {
        Shader shader = Shader.Find(ShaderName);
        if (shader == null)
        {
            Debug.LogError($"CausticsRendererFeature: Shader '{ShaderName}' not found.");
            return;
        }

        m_Material = CoreUtils.CreateEngineMaterial(shader);
        m_RenderPass = new CausticsRenderPass(m_Material);
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
