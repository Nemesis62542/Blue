#ifndef OCEAN_TESSELLATION_HLSL
#define OCEAN_TESSELLATION_HLSL

// ============================================================================
// Tessellation Control Point (Output from Vertex Shader, Input to Hull Shader)
// ============================================================================
struct TessellationControlPoint
{
    float4 positionOS : INTERNALTESSPOS;
    float3 normalOS : NORMAL;
    float4 tangentOS : TANGENT;
    float2 uv : TEXCOORD0;
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
// Calculate Distance-Based Tessellation Factor
// ============================================================================
float CalcDistanceTessFactor(float3 positionWS, float minDist, float maxDist, float maxTess)
{
    float3 cameraPos = GetCameraPositionWS();
    float dist = distance(positionWS, cameraPos);

    // Fade tessellation based on distance
    float f = saturate((maxDist - dist) / (maxDist - minDist));

    return lerp(1.0, maxTess, f);
}

// ============================================================================
// Calculate Edge Tessellation Factor (average of two vertices)
// ============================================================================
float CalcEdgeTessFactor(float3 pos0WS, float3 pos1WS, float minDist, float maxDist, float maxTess)
{
    float3 edgeCenter = (pos0WS + pos1WS) * 0.5;
    return CalcDistanceTessFactor(edgeCenter, minDist, maxDist, maxTess);
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
    output.normalOS = input.normalOS;
    output.tangentOS = input.tangentOS;
    output.uv = input.uv;

    return output;
}

// ============================================================================
// Patch Constant Function - Calculate tessellation factors for each patch
// ============================================================================
TessellationFactors PatchConstantFunction(
    InputPatch<TessellationControlPoint, 3> patch,
    uint patchID : SV_PrimitiveID)
{
    UNITY_SETUP_INSTANCE_ID(patch[0]);

    TessellationFactors f;

    // Convert to world space for distance calculation
    float3 pos0WS = TransformObjectToWorld(patch[0].positionOS.xyz);
    float3 pos1WS = TransformObjectToWorld(patch[1].positionOS.xyz);
    float3 pos2WS = TransformObjectToWorld(patch[2].positionOS.xyz);

    // Calculate edge tessellation factors based on distance
    f.edge[0] = CalcEdgeTessFactor(pos1WS, pos2WS, _TessellationMinDistance, _TessellationMaxDistance, _TessellationFactor);
    f.edge[1] = CalcEdgeTessFactor(pos2WS, pos0WS, _TessellationMinDistance, _TessellationMaxDistance, _TessellationFactor);
    f.edge[2] = CalcEdgeTessFactor(pos0WS, pos1WS, _TessellationMinDistance, _TessellationMaxDistance, _TessellationFactor);

    // Inside factor is average of edges
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
// Domain Shader - Generate final vertices after tessellation
// ============================================================================
[domain("tri")]
Varyings OceanDomainShader(
    TessellationFactors factors,
    OutputPatch<TessellationControlPoint, 3> patch,
    float3 barycentricCoords : SV_DomainLocation)
{
    Varyings output;

    UNITY_SETUP_INSTANCE_ID(patch[0]);
    UNITY_TRANSFER_INSTANCE_ID(patch[0], output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    // Interpolate attributes using barycentric coordinates
    float3 positionOS = patch[0].positionOS.xyz * barycentricCoords.x +
                        patch[1].positionOS.xyz * barycentricCoords.y +
                        patch[2].positionOS.xyz * barycentricCoords.z;

    float3 normalOS = patch[0].normalOS * barycentricCoords.x +
                      patch[1].normalOS * barycentricCoords.y +
                      patch[2].normalOS * barycentricCoords.z;
    normalOS = normalize(normalOS);

    float4 tangentOS = patch[0].tangentOS * barycentricCoords.x +
                       patch[1].tangentOS * barycentricCoords.y +
                       patch[2].tangentOS * barycentricCoords.z;
    tangentOS.xyz = normalize(tangentOS.xyz);

    float2 uv = patch[0].uv * barycentricCoords.x +
                patch[1].uv * barycentricCoords.y +
                patch[2].uv * barycentricCoords.z;

    // Get world position (before wave displacement)
    float3 positionWS = TransformObjectToWorld(positionOS);

    // Store world position for fragment shader (normal map sampling)
    output.positionOS = positionWS;

    // Apply Gerstner waves using world position
    float time = _Time.y;
    GerstnerWaveOutput waveOutput = EvaluateGerstnerWavesSimple(positionWS, time);

    // Apply displacement in world space
    float3 displacedPositionWS = positionWS + waveOutput.displacement;

    // Transform to clip space
    output.positionCS = TransformWorldToHClip(displacedPositionWS);
    output.positionWS = displacedPositionWS;

    // Wave normal is already in world space orientation
    output.normalWS = waveOutput.normal;

    // Calculate tangent in world space
    float3 tangentWS = TransformObjectToWorldDir(tangentOS.xyz);
    output.tangentWS = float4(tangentWS, tangentOS.w);

    // Other outputs
    output.uv = uv;
    output.screenPos = ComputeScreenPos(output.positionCS);
    output.viewDirWS = GetWorldSpaceViewDir(output.positionWS);
    output.fogFactor = ComputeFogFactor(output.positionCS.z);

    return output;
}

#endif // OCEAN_TESSELLATION_HLSL
