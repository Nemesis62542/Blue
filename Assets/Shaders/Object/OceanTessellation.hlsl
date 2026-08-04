#ifndef OCEAN_TESSELLATION_HLSL
#define OCEAN_TESSELLATION_HLSL

// Shared by every pass: the vertex stage, the patch constant function and the
// hull shader are identical regardless of what the pass ends up writing, so only
// the domain shader is specialised. Keeping one copy stops the three passes from
// drifting out of sync.

// ============================================================================
// Tessellation Control Point (Output from Vertex Shader, Input to Hull Shader)
// ============================================================================
// Position is all the domain shaders need: the wave, its normal and the tangent
// frame are all rebuilt analytically in the fragment shader.
struct TessellationControlPoint
{
    float4 positionOS : INTERNALTESSPOS;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

// ============================================================================
// Tessellation Factors
// ============================================================================
struct TessellationFactors
{
    float edge[3] : SV_TessFactor;
    float inside : SV_InsideTessFactor;
};

// ============================================================================
// Position-only varyings for the depth / shadow passes
// ============================================================================
struct PositionOnlyVaryings
{
    float4 positionCS : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

// ============================================================================
// Distance-Based Tessellation Factors
// ============================================================================
float CalcDistanceTessFactor(float3 positionWS, float minDist, float maxDist, float maxTess)
{
    float dist = distance(positionWS, GetCameraPositionWS());
    float f = saturate((maxDist - dist) / max(maxDist - minDist, 0.001));
    return lerp(1.0, maxTess, f);
}

float CalcEdgeTessFactor(float3 pos0WS, float3 pos1WS)
{
    float3 edgeCenter = (pos0WS + pos1WS) * 0.5;
    return CalcDistanceTessFactor(edgeCenter, _TessellationMinDistance, _TessellationMaxDistance, _TessellationFactor);
}

// ============================================================================
// Vertex Shader (Pre-Tessellation) - Just pass through
// ============================================================================
TessellationControlPoint TessellationVert(Attributes input)
{
    TessellationControlPoint output;

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);

    output.positionOS = input.positionOS;

    return output;
}

// ============================================================================
// Patch Constant Function - Calculate tessellation factors for each patch
// ============================================================================
TessellationFactors PatchConstantFunction(InputPatch<TessellationControlPoint, 3> patch)
{
    UNITY_SETUP_INSTANCE_ID(patch[0]);

    TessellationFactors f;

    float3 pos0WS = TransformObjectToWorld(patch[0].positionOS.xyz);
    float3 pos1WS = TransformObjectToWorld(patch[1].positionOS.xyz);
    float3 pos2WS = TransformObjectToWorld(patch[2].positionOS.xyz);

    f.edge[0] = CalcEdgeTessFactor(pos1WS, pos2WS);
    f.edge[1] = CalcEdgeTessFactor(pos2WS, pos0WS);
    f.edge[2] = CalcEdgeTessFactor(pos0WS, pos1WS);
    f.inside = (f.edge[0] + f.edge[1] + f.edge[2]) / 3.0;

    return f;
}

// ============================================================================
// Hull Shader - Process control points
// ============================================================================
[domain("tri")]
[partitioning("fractional_odd")]
[outputtopology("triangle_cw")]
[outputcontrolpoints(3)]
[patchconstantfunc("PatchConstantFunction")]
[maxtessfactor(64)]
TessellationControlPoint OceanHullShader(
    InputPatch<TessellationControlPoint, 3> patch,
    uint id : SV_OutputControlPointID)
{
    return patch[id];
}

// ============================================================================
// Barycentric interpolation helper
// ============================================================================
#define OCEAN_BARY_LERP(a, b, c, bary) ((a) * (bary).x + (b) * (bary).y + (c) * (bary).z)

// ============================================================================
// Domain Shader - Forward pass
// ============================================================================
[domain("tri")]
Varyings OceanDomainShader(
    TessellationFactors factors,
    OutputPatch<TessellationControlPoint, 3> patch,
    float3 bary : SV_DomainLocation)
{
    Varyings output;

    UNITY_SETUP_INSTANCE_ID(patch[0]);
    UNITY_TRANSFER_INSTANCE_ID(patch[0], output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    float3 positionOS = OCEAN_BARY_LERP(patch[0].positionOS.xyz, patch[1].positionOS.xyz, patch[2].positionOS.xyz, bary);

    // Undisplaced world position drives the wave phase and the normal map UVs, so
    // the detail stays put instead of sliding around with the displacement. It is
    // also exact under interpolation, which is why the fragment shader works from
    // it rather than from the displaced position.
    float3 positionWS = TransformObjectToWorld(positionOS);
    output.flatPositionWS = positionWS;

    GerstnerWaveOutput waveOutput = EvaluateGerstnerWavesSimple(positionWS, _Time.y);

    output.positionCS = TransformWorldToHClip(positionWS + waveOutput.displacement);
    output.screenPos = ComputeScreenPos(output.positionCS);
    output.fogFactor = ComputeFogFactor(output.positionCS.z);

    return output;
}

// ============================================================================
// Domain Shader - Depth only pass
// ============================================================================
[domain("tri")]
PositionOnlyVaryings DepthDomainShader(
    TessellationFactors factors,
    OutputPatch<TessellationControlPoint, 3> patch,
    float3 bary : SV_DomainLocation)
{
    PositionOnlyVaryings output;

    UNITY_SETUP_INSTANCE_ID(patch[0]);
    UNITY_TRANSFER_INSTANCE_ID(patch[0], output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    float3 positionOS = OCEAN_BARY_LERP(patch[0].positionOS.xyz, patch[1].positionOS.xyz, patch[2].positionOS.xyz, bary);
    float3 positionWS = TransformObjectToWorld(positionOS);

    GerstnerWaveOutput waveOutput = EvaluateGerstnerWavesSimple(positionWS, _Time.y);
    output.positionCS = TransformWorldToHClip(positionWS + waveOutput.displacement);

    return output;
}

half4 DepthOnlyFrag(PositionOnlyVaryings input) : SV_Target
{
    return 0;
}

#endif // OCEAN_TESSELLATION_HLSL
