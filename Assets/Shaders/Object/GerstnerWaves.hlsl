#ifndef GERSTNER_WAVES_HLSL
#define GERSTNER_WAVES_HLSL

// ============================================================================
// Gerstner Wave Structure
// ============================================================================
struct GerstnerWave
{
    float amplitude;    // A: Wave height
    float frequency;    // w: Angular frequency (2PI / wavelength)
    float speed;        // phi: Phase speed
    float steepness;    // Q: Steepness factor (0-1)
    float phase0;       // Constant phase offset
    float2 direction;   // D: Normalized direction vector
};

// ============================================================================
// Gerstner Wave Output
// ============================================================================
struct GerstnerWaveOutput
{
    float3 displacement;
    float3 normal;
};

// ============================================================================
// Evaluate Single Gerstner Wave (World Space)
// Calculates displacement and tangent/binormal contributions for a single wave
// ============================================================================
void EvaluateGerstnerWave(
    GerstnerWave wave,
    float2 scaledPosXZ,
    float time,
    inout float3 displacement,
    inout float3 tangent,
    inout float3 binormal)
{
    float2 D = normalize(wave.direction);
    float w = wave.frequency;
    float A = wave.amplitude;
    float Q = wave.steepness;
    float phi = wave.speed;

    // phase = w * (D.x * x + D.y * z) + phi * t + phase0
    float phase = w * dot(D, scaledPosXZ) + phi * time + wave.phase0;
    float sinPhase = sin(phase);
    float cosPhase = cos(phase);

    // x' = x + Q * A * D.x * cos(phase)
    // y' = A * sin(phase)
    // z' = z + Q * A * D.y * cos(phase)
    displacement.x += Q * A * D.x * cosPhase;
    displacement.y += A * sinPhase;
    displacement.z += Q * A * D.y * cosPhase;

    // Partial derivatives for the normal
    float WA = w * A;
    float QWA = Q * WA;

    tangent.x += -QWA * D.x * D.x * sinPhase;
    tangent.y += WA * D.x * cosPhase;
    tangent.z += -QWA * D.x * D.y * sinPhase;

    binormal.x += -QWA * D.x * D.y * sinPhase;
    binormal.y += WA * D.y * cosPhase;
    binormal.z += -QWA * D.y * D.y * sinPhase;
}

// ============================================================================
// Spectral wave bank
//
// Three plane waves gave the sea long parallel crests and a lattice of
// identical interference peaks - every attempt to hide that downstream (foam
// gates, threshold jitter) was treating this symptom. Six components with
// spread directions and golden-ratio frequency spacing make the crest field
// short-crested: peaks become an irregular scatter of hills, and foam lands in
// natural clumps without any masking tricks.
//
// Per component: (direction offset in [-1,1], frequency ratio, amplitude
// weight, phase offset). Frequency ratios are powers of sqrt(golden ratio), so
// no pair is commensurate and the field never repeats on visible scales.
// Amplitude weights sum to 1.0: _WaveAmplitude is the TOTAL amplitude budget,
// which keeps world-space thresholds like _FoamHeight meaningful.
//
// BreakingWaveBubbles.cs re-evaluates these waves on the CPU to place bubble
// curtains under breaking crests - keep its WaveBank table in sync with this.
// ============================================================================
#define OCEAN_WAVE_COUNT 6
static const float4 OceanWaveBank[OCEAN_WAVE_COUNT] =
{
    // angle01  freqRatio  weight  phase0
    float4(-0.93, 1.000, 0.30, 0.00),
    float4( 0.71, 1.272, 0.24, 2.10),
    float4(-0.34, 1.618, 0.18, 4.31),
    float4( 0.19, 2.058, 0.12, 1.37),
    float4( 0.87, 2.618, 0.09, 5.62),
    float4(-0.58, 3.330, 0.07, 3.94)
};

// ============================================================================
// Evaluate the wave bank (using material properties, World Space)
//
// Wave groups: an amplitude envelope a few wavelengths across, because real
// waves arrive in sets. It multiplies the amplitudes, which also scales the
// normal's derivative terms consistently (they are linear in A); the envelope's
// own gradient is ignored - the standard slowly-varying-envelope shortcut.
//
// Lives inside this function so every caller - domain shaders, depth pass and
// the per-fragment evaluation - displaces identically.
// ============================================================================
GerstnerWaveOutput EvaluateGerstnerWavesSimple(float3 positionWS, float time)
{
    float2 windDir = normalize(_WaveDirection.xz + float2(1e-6, 0));

    // Groups travel at half the primary phase speed (deep-water group velocity),
    // so sets roll through rather than being painted onto the sea.
    float groupSpeed = 0.5 * _WaveSpeed / max(_WaveFrequency * _WorldScale, 1e-4);
    float2 groupUV = (positionWS.xz - windDir * (groupSpeed * time)) * _WaveGroupScale;
    float envelope = 1.0 - _WaveGroupAmount * (1.0 - OceanValueNoise(groupUV));

    float totalAmp = _WaveAmplitude * envelope;
    float spreadRad = radians(_WaveSpread);

    GerstnerWaveOutput output;
    output.displacement = float3(0, 0, 0);
    float3 tangent = float3(1, 0, 0);
    float3 binormal = float3(0, 0, 1);

    float2 scaledPosXZ = positionWS.xz * _WorldScale;

    [unroll]
    for (int i = 0; i < OCEAN_WAVE_COUNT; i++)
    {
        float4 bank = OceanWaveBank[i];

        // Rotate the wind direction by this component's share of the spread.
        float s, c;
        sincos(bank.x * spreadRad, s, c);
        float2 dir = float2(windDir.x * c - windDir.y * s,
                            windDir.x * s + windDir.y * c);

        GerstnerWave wave;
        wave.amplitude = totalAmp * bank.z;
        wave.frequency = _WaveFrequency * bank.y;
        // Deep-water dispersion: shorter waves run slower in phase (w ~ sqrt(k)).
        wave.speed = _WaveSpeed * sqrt(bank.y);
        wave.steepness = _WaveSteepness;
        wave.phase0 = bank.w;
        wave.direction = dir;

        EvaluateGerstnerWave(wave, scaledPosXZ, time, output.displacement, tangent, binormal);
    }

    output.normal = normalize(cross(binormal, tangent));

    return output;
}

#endif // GERSTNER_WAVES_HLSL
