using UnityEngine;

namespace Blue.Visual
{
    /// <summary>
    /// 砕けている波頭の直下に気泡カーテンを撒くコンポーネント。
    /// OceanSurface マテリアルから波パラメータを読み、シェーダーと同じ波を
    /// CPU 側で評価して「泡が立っている場所」を特定する。
    /// 波の定義（波バンクのテーブルを含む）は GerstnerWaves.hlsl と
    /// 一致させること。片方を変えたら必ずもう片方も更新する。
    /// </summary>
    [RequireComponent(typeof(ParticleSystem))]
    public class BreakingWaveBubbles : MonoBehaviour
    {
        [Header("参照")]
        [Tooltip("波パラメータの取得元（OceanSurface マテリアル）")]
        [SerializeField] private Material oceanMaterial;
        [Tooltip("この周囲に泡を撒く。未設定なら自身の位置")]
        [SerializeField] private Transform followTarget;

        [Header("サンプリング")]
        [Tooltip("泡を撒く範囲の半径")]
        [SerializeField] private float sampleRadius = 35f;
        [Tooltip("1秒あたりの波頭サンプル数")]
        [SerializeField] private int samplesPerSecond = 120;
        [Tooltip("砕けている波頭1箇所あたりの気泡数")]
        [SerializeField] private int bubblesPerBurst = 6;

        [Header("カーテン形状")]
        [Tooltip("気泡が注入される深さ（水面からの距離）")]
        [SerializeField] private float curtainDepth = 2f;
        [Tooltip("1バーストの水平方向の広がり")]
        [SerializeField] private float burstRadius = 0.6f;
        [Tooltip("注入時の下向き初速。浮力（負のgravityModifier）で減速して浮上に転じる")]
        [SerializeField] private float downwardSpeed = 1.2f;

        // GerstnerWaves.hlsl の OceanWaveBank と同一の値を保つこと
        // (方向オフセット, 周波数比, 振幅ウェイト, 位相)
        private static readonly Vector4[] WaveBank =
        {
            new Vector4(-0.93f, 1.000f, 0.30f, 0.00f),
            new Vector4( 0.71f, 1.272f, 0.24f, 2.10f),
            new Vector4(-0.34f, 1.618f, 0.18f, 4.31f),
            new Vector4( 0.19f, 2.058f, 0.12f, 1.37f),
            new Vector4( 0.87f, 2.618f, 0.09f, 5.62f),
            new Vector4(-0.58f, 3.330f, 0.07f, 3.94f),
        };

        // シェーダープロパティID
        private static readonly int WaveAmplitudeId = Shader.PropertyToID("_WaveAmplitude");
        private static readonly int WaveFrequencyId = Shader.PropertyToID("_WaveFrequency");
        private static readonly int WaveSpeedId = Shader.PropertyToID("_WaveSpeed");
        private static readonly int WaveSteepnessId = Shader.PropertyToID("_WaveSteepness");
        private static readonly int WaveDirectionId = Shader.PropertyToID("_WaveDirection");
        private static readonly int WaveSpreadId = Shader.PropertyToID("_WaveSpread");
        private static readonly int WorldScaleId = Shader.PropertyToID("_WorldScale");
        private static readonly int WaveGroupAmountId = Shader.PropertyToID("_WaveGroupAmount");
        private static readonly int WaveGroupScaleId = Shader.PropertyToID("_WaveGroupScale");
        private static readonly int FoamHeightId = Shader.PropertyToID("_FoamHeight");
        private static readonly int FoamSoftnessId = Shader.PropertyToID("_FoamSoftness");
        private static readonly int WaterSurfaceYId = Shader.PropertyToID("_WaterSurfaceY");

        private ParticleSystem bubbleSystem;
        private float sampleAccumulator;

        // マテリアルから読むパラメータ（毎フレーム更新。コストは無視できる）
        private float waveAmplitude;
        private float waveFrequency;
        private float waveSpeed;
        private float waveSteepness;
        private float waveSpread;
        private float worldScale;
        private float groupAmount;
        private float groupScale;
        private float foamHeight;
        private float foamSoftness;
        private float waterSurfaceY;
        private Vector2 windDir;

        private void Awake()
        {
            bubbleSystem = GetComponent<ParticleSystem>();

            // ワールド座標で Emit するため。Local のままだと親の移動で泡がずれる
            var main = bubbleSystem.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            // 発生はこのスクリプトが Emit で駆動する
            var emission = bubbleSystem.emission;
            emission.enabled = false;

            if (oceanMaterial == null)
            {
                Debug.LogWarning($"{nameof(BreakingWaveBubbles)}: oceanMaterial 未設定のため無効化", this);
                enabled = false;
            }
        }

        private void Update()
        {
            ReadMaterialParams();

            Vector3 center = followTarget != null ? followTarget.position : transform.position;
            float time = Time.timeSinceLevelLoad; // シェーダーの _Time.y と同じ時計

            // フレームレート非依存のサンプル数
            sampleAccumulator += samplesPerSecond * Time.deltaTime;
            int samples = (int)sampleAccumulator;
            sampleAccumulator -= samples;

            for (int i = 0; i < samples; i++)
            {
                Vector2 offset = Random.insideUnitCircle * sampleRadius;
                float x = center.x + offset.x;
                float z = center.z + offset.y;

                Vector3 disp = EvaluateWaveDisplacement(x, z, time);

                // シェーダーの foam しきい値と同じ判定。強く砕けているほど高確率で
                // 泡を出す（レースパターンまでは再現しない。気泡は体積なので不要）
                float crest = (disp.y - foamHeight) / Mathf.Max(foamSoftness, 1e-4f);
                if (crest <= 0f || Random.value > crest)
                {
                    continue;
                }

                // Gerstner の水平変位も足して、見た目の波頭の真下に置く
                Vector3 crestPos = new Vector3(x + disp.x, waterSurfaceY + disp.y, z + disp.z);
                EmitBurst(crestPos);
            }
        }

        private void ReadMaterialParams()
        {
            waveAmplitude = oceanMaterial.GetFloat(WaveAmplitudeId);
            waveFrequency = oceanMaterial.GetFloat(WaveFrequencyId);
            waveSpeed = oceanMaterial.GetFloat(WaveSpeedId);
            waveSteepness = oceanMaterial.GetFloat(WaveSteepnessId);
            waveSpread = oceanMaterial.GetFloat(WaveSpreadId);
            worldScale = oceanMaterial.GetFloat(WorldScaleId);
            groupAmount = oceanMaterial.GetFloat(WaveGroupAmountId);
            groupScale = oceanMaterial.GetFloat(WaveGroupScaleId);
            foamHeight = oceanMaterial.GetFloat(FoamHeightId);
            foamSoftness = oceanMaterial.GetFloat(FoamSoftnessId);
            waterSurfaceY = oceanMaterial.GetFloat(WaterSurfaceYId);

            Vector4 dir = oceanMaterial.GetVector(WaveDirectionId);
            windDir = new Vector2(dir.x + 1e-6f, dir.z).normalized;
        }

        /// <summary>
        /// EvaluateGerstnerWavesSimple (GerstnerWaves.hlsl) の変位のみ版
        /// </summary>
        private Vector3 EvaluateWaveDisplacement(float x, float z, float time)
        {
            // 波の群れの包絡線
            float groupSpeed = 0.5f * waveSpeed / Mathf.Max(waveFrequency * worldScale, 1e-4f);
            Vector2 groupUV = (new Vector2(x, z) - windDir * (groupSpeed * time)) * groupScale;
            float envelope = 1f - groupAmount * (1f - ValueNoise(groupUV));

            float totalAmp = waveAmplitude * envelope;
            float spreadRad = waveSpread * Mathf.Deg2Rad;

            float scaledX = x * worldScale;
            float scaledZ = z * worldScale;

            Vector3 disp = Vector3.zero;
            for (int i = 0; i < WaveBank.Length; i++)
            {
                Vector4 bank = WaveBank[i];

                float s = Mathf.Sin(bank.x * spreadRad);
                float c = Mathf.Cos(bank.x * spreadRad);
                float dirX = windDir.x * c - windDir.y * s;
                float dirZ = windDir.x * s + windDir.y * c;

                float amplitude = totalAmp * bank.z;
                float frequency = waveFrequency * bank.y;
                float phase = frequency * (dirX * scaledX + dirZ * scaledZ)
                            + waveSpeed * Mathf.Sqrt(bank.y) * time
                            + bank.w;

                float sinPhase = Mathf.Sin(phase);
                float cosPhase = Mathf.Cos(phase);

                disp.x += waveSteepness * amplitude * dirX * cosPhase;
                disp.y += amplitude * sinPhase;
                disp.z += waveSteepness * amplitude * dirZ * cosPhase;
            }

            return disp;
        }

        private void EmitBurst(Vector3 crestPos)
        {
            var emitParams = new ParticleSystem.EmitParams();

            for (int i = 0; i < bubblesPerBurst; i++)
            {
                Vector2 spread = Random.insideUnitCircle * burstRadius;
                float depth = Random.Range(0.15f, curtainDepth);

                emitParams.position = crestPos + new Vector3(spread.x, -depth, spread.y);

                // 砕けた波が気泡を巻き込む下向きの初速。横に少し散らす
                emitParams.velocity = new Vector3(
                    spread.x * 0.5f,
                    -downwardSpeed * Random.Range(0.5f, 1f),
                    spread.y * 0.5f);

                bubbleSystem.Emit(emitParams, 1);
            }
        }

        // ============================================================
        // OceanSurfaceInput.hlsl の OceanHash / OceanValueNoise と同じ実装
        // （群れの包絡線をシェーダーと一致させるため）
        // ============================================================
        private static float Frac(float v)
        {
            return v - Mathf.Floor(v);
        }

        private static float Hash(float px, float py)
        {
            px = Frac(px * 127.1f);
            py = Frac(py * 311.7f);
            float d = px * (py + 19.19f) + py * (px + 19.19f);
            px = Frac((px + d) * (py + d));
            return px;
        }

        private static float ValueNoise(Vector2 p)
        {
            float ix = Mathf.Floor(p.x);
            float iy = Mathf.Floor(p.y);
            float fx = p.x - ix;
            float fy = p.y - iy;
            fx = fx * fx * (3f - 2f * fx);
            fy = fy * fy * (3f - 2f * fy);

            float a = Hash(ix, iy);
            float b = Hash(ix + 1f, iy);
            float c = Hash(ix, iy + 1f);
            float d = Hash(ix + 1f, iy + 1f);

            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
        }
    }
}
