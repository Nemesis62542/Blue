using UnityEngine;

namespace Blue.Visual
{
    /// <summary>
    /// 深度に応じて環境光とSkyboxを制御するコンポーネント
    /// </summary>
    public class DepthEnvironmentController : MonoBehaviour
    {
        [Header("参照")]
        [SerializeField] private Material skyboxMaterial;
        [SerializeField] private Light directionalLight;

        [Header("深度設定")]
        [Tooltip("グラデーションが始まる深度")]
        [SerializeField] private float minDepth = 0f;
        [Tooltip("グラデーションが終わる深度")]
        [SerializeField] private float maxDepth = 100f;

        [Header("DirectionalLight設定")]
        [SerializeField] private float surfaceLightIntensity = 1f;
        [SerializeField] private float deepLightIntensity = 0.1f;

        [Header("環境光設定")]
        [SerializeField] private float surfaceAmbientIntensity = 1f;
        [SerializeField] private float deepAmbientIntensity = 0.1f;
        [SerializeField] private Color surfaceAmbientColor = new Color(0.5f, 0.7f, 0.9f);
        [SerializeField] private Color deepAmbientColor = new Color(0.05f, 0.1f, 0.2f);

        [Header("Environment Reflections設定")]
        [SerializeField] private float surfaceReflectionIntensity = 1f;
        [SerializeField] private float deepReflectionIntensity = 0f;

        [Header("Fog設定（Lighting/Skybox共通）")]
        [SerializeField] private Color surfaceFogColor = new Color(0.21f, 0.53f, 0.77f);
        [SerializeField] private Color deepFogColor = new Color(0.02f, 0.05f, 0.15f);
        [SerializeField] private float surfaceSkyboxFogIntensity = 1f;
        [SerializeField] private float deepSkyboxFogIntensity = 2f;

        [Header("補間設定")]
        [SerializeField] private AnimationCurve depthCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        private Material skyboxInstance;

        // Skyboxシェーダープロパティ名
        private static readonly int TintProperty = Shader.PropertyToID("_Tint");
        private static readonly int FogIntensProperty = Shader.PropertyToID("_FogIntens");
        private static readonly int FogColProperty = Shader.PropertyToID("_FogCol");

        private void Awake()
        {
            // Skyboxマテリアルのインスタンスを作成（元のマテリアルを変更しないため）
            if (skyboxMaterial != null)
            {
                skyboxInstance = new Material(skyboxMaterial);
                RenderSettings.skybox = skyboxInstance;
            }
            else if (RenderSettings.skybox != null)
            {
                skyboxInstance = new Material(RenderSettings.skybox);
                RenderSettings.skybox = skyboxInstance;
            }
        }

        private void OnDestroy()
        {
            // インスタンス化したマテリアルを破棄
            if (skyboxInstance != null)
            {
                Destroy(skyboxInstance);
            }
        }

        /// <summary>
        /// 深度に基づいて環境を更新する
        /// </summary>
        /// <param name="depth">現在の深度（正の値 = 水面下）</param>
        public void UpdateEnvironment(float depth)
        {
#if UNITY_EDITOR
            // デバッグモード中は外部からの更新を無視
            if (debugMode) return;
#endif
            UpdateEnvironmentInternal(depth);
        }

        private void UpdateEnvironmentInternal(float depth)
        {
            // minDepth〜maxDepthの範囲を0〜1に正規化
            float range = maxDepth - minDepth;
            float normalizedDepth = range > 0f
                ? Mathf.Clamp01((depth - minDepth) / range)
                : 0f;

            // カーブを適用
            float t = depthCurve.Evaluate(normalizedDepth);

            // 共通のフォグ色を計算
            Color fogColor = Color.Lerp(surfaceFogColor, deepFogColor, t);

            // 各要素を更新
            UpdateDirectionalLight(t);
            UpdateAmbientLight(t);
            UpdateReflections(t);
            UpdateLightingFog(fogColor);
            UpdateSkybox(t, fogColor);
        }

        private void UpdateDirectionalLight(float t)
        {
            if (directionalLight == null) return;

            directionalLight.intensity = Mathf.Lerp(surfaceLightIntensity, deepLightIntensity, t);
        }

        private void UpdateAmbientLight(float t)
        {
            // 環境光の強度を補間
            RenderSettings.ambientIntensity = Mathf.Lerp(surfaceAmbientIntensity, deepAmbientIntensity, t);

            // 環境光の色を補間（Trilight/Gradientモードの場合）
            Color ambientColor = Color.Lerp(surfaceAmbientColor, deepAmbientColor, t);
            RenderSettings.ambientSkyColor = ambientColor;
            RenderSettings.ambientEquatorColor = ambientColor;
            RenderSettings.ambientGroundColor = Color.Lerp(ambientColor, Color.black, 0.5f);
        }

        private void UpdateReflections(float t)
        {
            RenderSettings.reflectionIntensity = Mathf.Lerp(surfaceReflectionIntensity, deepReflectionIntensity, t);
        }

        private void UpdateLightingFog(Color fogColor)
        {
            // RenderSettingsのフォグ色を更新
            RenderSettings.fogColor = fogColor;
        }

        private void UpdateSkybox(float t, Color fogColor)
        {
            if (skyboxInstance == null) return;

            // Tint（Fog Colorと共通）
            skyboxInstance.SetColor(TintProperty, fogColor);

            // フォグ強度を補間
            float fogIntensity = Mathf.Lerp(surfaceSkyboxFogIntensity, deepSkyboxFogIntensity, t);
            skyboxInstance.SetFloat(FogIntensProperty, fogIntensity);

            // フォグ色（Lighting Fogと共通）
            skyboxInstance.SetColor(FogColProperty, fogColor);

            // Skyboxの変更を反映
            DynamicGI.UpdateEnvironment();
        }

        /// <summary>
        /// 環境を初期状態にリセット
        /// </summary>
        public void ResetEnvironment()
        {
            UpdateEnvironment(minDepth);
        }

#if UNITY_EDITOR
        [Header("デバッグ")]
        [SerializeField] private bool debugMode;
        [SerializeField, Range(0f, 200f)] private float debugDepth;

        private void Update()
        {
            if (debugMode)
            {
                UpdateEnvironmentInternal(debugDepth);
            }
        }
#endif
    }
}
