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
// Evaluate Single Gerstner Wave
// Calculates displacement and tangent/binormal contributions for a single wave
// ============================================================================
void EvaluateGerstnerWave(
    GerstnerWave wave,
    float3 positionOS,
    float time,
    inout float3 displacement,
    inout float3 tangent,
    inout float3 binormal)
{
    // Normalize direction
    float2 D = normalize(wave.direction);
    float w = wave.frequency;
    float A = wave.amplitude;
    float Q = wave.steepness;
    float phi = wave.speed;

    // Calculate phase
    // phase = w * (D.x * x + D.y * z) + phi * t
    float phase = w * dot(D, positionOS.xz) + phi * time;
    float sinPhase = sin(phase);
    float cosPhase = cos(phase);

    // Calculate displacement
    // x' = x + Q * A * D.x * cos(phase)
    // y' = A * sin(phase)
    // z' = z + Q * A * D.y * cos(phase)
    displacement.x += Q * A * D.x * cosPhase;
    displacement.y += A * sinPhase;
    displacement.z += Q * A * D.y * cosPhase;

    // Calculate tangent and binormal contributions for normal calculation
    // Partial derivatives of the wave equation
    float WA = w * A;
    float QWA = Q * WA;

    // Tangent (partial derivative with respect to x)
    tangent.x += -QWA * D.x * D.x * sinPhase;
    tangent.y += WA * D.x * cosPhase;
    tangent.z += -QWA * D.x * D.y * sinPhase;

    // Binormal (partial derivative with respect to z)
    binormal.x += -QWA * D.x * D.y * sinPhase;
    binormal.y += WA * D.y * cosPhase;
    binormal.z += -QWA * D.y * D.y * sinPhase;
}

// ============================================================================
// Evaluate Multiple Gerstner Waves
// Combines multiple wave layers and computes the final displacement and normal
// ============================================================================
GerstnerWaveOutput EvaluateGerstnerWaves(
    float3 positionOS,
    float time,
    float amplitude1, float frequency1, float speed1, float steepness, float2 direction1,
    float amplitude2, float frequency2, float speed2, float2 direction2,
    float amplitude3, float frequency3, float speed3, float2 direction3)
{
    GerstnerWaveOutput output;
    output.displacement = float3(0, 0, 0);

    // Initialize tangent and binormal
    float3 tangent = float3(1, 0, 0);
    float3 binormal = float3(0, 0, 1);

    // Wave 1 - Primary wave (largest)
    GerstnerWave wave1;
    wave1.amplitude = amplitude1;
    wave1.frequency = frequency1;
    wave1.speed = speed1;
    wave1.steepness = steepness;
    wave1.direction = direction1;
    EvaluateGerstnerWave(wave1, positionOS, time, output.displacement, tangent, binormal);

    // Wave 2 - Secondary wave
    GerstnerWave wave2;
    wave2.amplitude = amplitude2;
    wave2.frequency = frequency2;
    wave2.speed = speed2;
    wave2.steepness = steepness * 0.9;
    wave2.direction = direction2;
    EvaluateGerstnerWave(wave2, positionOS, time, output.displacement, tangent, binormal);

    // Wave 3 - Tertiary wave (smallest, high frequency detail)
    GerstnerWave wave3;
    wave3.amplitude = amplitude3;
    wave3.frequency = frequency3;
    wave3.speed = speed3;
    wave3.steepness = steepness * 0.8;
    wave3.direction = direction3;
    EvaluateGerstnerWave(wave3, positionOS, time, output.displacement, tangent, binormal);

    // Calculate normal from tangent and binormal (cross product)
    // Normal = Binormal x Tangent
    output.normal = normalize(cross(binormal, tangent));

    return output;
}

// ============================================================================
// Simplified Gerstner Waves (using material properties)
// ============================================================================
GerstnerWaveOutput EvaluateGerstnerWavesSimple(float3 positionOS, float time)
{
    return EvaluateGerstnerWaves(
        positionOS,
        time,
        _WaveAmplitude, _WaveFrequency, _WaveSpeed, _WaveSteepness, _WaveDirection.xz,
        _Wave2Amplitude, _Wave2Frequency, _Wave2Speed, _Wave2Direction.xz,
        _Wave3Amplitude, _Wave3Frequency, _Wave3Speed, _Wave3Direction.xz
    );
}

#endif // GERSTNER_WAVES_HLSL
