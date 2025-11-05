float _TerrainWidthInv;
float _TerrainHeightInv;

#if defined(_NORMALMAP) && defined(SURFACE_GRADIENT)
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl"
#endif

TEXTURE2D(_Control0);

#define DECLARE_TERRAIN_LAYER_TEXS(n)   \
    TEXTURE2D(_Splat##n);               \
    TEXTURE2D(_Normal##n);              \
    TEXTURE2D(_Mask##n)

DECLARE_TERRAIN_LAYER_TEXS(0);
DECLARE_TERRAIN_LAYER_TEXS(1);
DECLARE_TERRAIN_LAYER_TEXS(2);
DECLARE_TERRAIN_LAYER_TEXS(3);
#ifdef _TERRAIN_8_LAYERS
    DECLARE_TERRAIN_LAYER_TEXS(4);
    DECLARE_TERRAIN_LAYER_TEXS(5);
    DECLARE_TERRAIN_LAYER_TEXS(6);
    DECLARE_TERRAIN_LAYER_TEXS(7);
    TEXTURE2D(_Control1);
#endif

#undef DECLARE_TERRAIN_LAYER_TEXS

SAMPLER(sampler_Splat0);
SAMPLER(sampler_Control0);

float GetSumHeight(float4 heights0, float4 heights1)
{
    float sumHeight = heights0.x;
    sumHeight += heights0.y;
    sumHeight += heights0.z;
    sumHeight += heights0.w;
    #ifdef _TERRAIN_8_LAYERS
        sumHeight += heights1.x;
        sumHeight += heights1.y;
        sumHeight += heights1.z;
        sumHeight += heights1.w;
    #endif
    return sumHeight;
}

float3 SampleNormalGrad(TEXTURE2D_PARAM(textureName, samplerName), float2 uv, float2 dxuv, float2 dyuv, float scale)
{
    float4 nrm = SAMPLE_TEXTURE2D_GRAD(textureName, samplerName, uv, dxuv, dyuv);
#ifdef SURFACE_GRADIENT
    #ifdef UNITY_NO_DXT5nm
        return float3(UnpackDerivativeNormalRGB(nrm, scale), 0);
    #else
        return float3(UnpackDerivativeNormalRGorAG(nrm, scale), 0);
    #endif
#else
    #ifdef UNITY_NO_DXT5nm
        return UnpackNormalRGB(nrm, scale);
    #else
        return UnpackNormalmapRGorAG(nrm, scale);
    #endif
#endif
}

float3 UnpackNormalDigger(float4 nrm, float scale)
{
#ifdef SURFACE_GRADIENT
    #ifdef UNITY_NO_DXT5nm
        return float3(UnpackDerivativeNormalRGB(nrm, scale), 0);
    #else
        return float3(UnpackDerivativeNormalRGorAG(nrm, scale), 0);
    #endif
#else
    #ifdef UNITY_NO_DXT5nm
        return UnpackNormalRGB(nrm, scale);
    #else
        return UnpackNormalmapRGorAG(nrm, scale);
    #endif
#endif
}


float3 GetBary (float2 uv) {
    // unity shader code
    float2 texSize = _Control0_TexelSize.zw; // size of texture
    float2 stp = _Control0_TexelSize.xy;     // size of texel
    // create a virtual quad for the pixel
    float2 stepped = uv * texSize;
    float2 uvBottom = floor(stepped);
    float2 uvFrac = frac(stepped);
    uvBottom /= texSize;
    uvBottom += stp * 0.5;
    // select virtual triangle from virtual quad
    float2 a, b, c;
    if (uvFrac.x > uvFrac.y)
    {
        a = uvBottom;
        b = uvBottom + float2(stp.x, 0);
        c = uvBottom + float2(stp.x, stp.y);
    }
    else
    {
        a = uvBottom;
        b = uvBottom + float2(0, stp.y);
        c = uvBottom + float2(stp.x, stp.y);
    }
    // our position in virtual triangle
    float2 p = uvFrac * stp + uvBottom;
    // generate barycentric coordinates
    float2 v0 = b - a;
    float2 v1 = c - a;
    float2 v2 = p - a;
    float d00 = dot(v0, v0);
    float d01 = dot(v0, v1);
    float d11 = dot(v1, v1);
    float d20 = dot(v2, v0);
    float d21 = dot(v2, v1);
    float denom = d00 * d11 - d01 * d01;
    float v = (d11 * d20 - d01 * d21) / denom;
    float w = (d00 * d21 - d01 * d20) / denom;
    float u = 1.0f - v - w;
    return float3(u, v, w);
}

float4 RemapMasks(float4 masks, float blendMask, float4 remapOffset, float4 remapScale)
{
    float4 ret = masks;
    ret.b *= blendMask; // height needs to be weighted before remapping
    ret = ret * remapScale + remapOffset;
    return ret;
}

#ifdef OVERRIDE_SPLAT_SAMPLER_NAME
    #define sampler_Splat0 OVERRIDE_SPLAT_SAMPLER_NAME
    SAMPLER(OVERRIDE_SPLAT_SAMPLER_NAME);
#endif

void TerrainSplatBlend(float2 controlUV, float2 splatBaseUV, FragInputs input, inout TerrainLitSurfaceData surfaceData)
{
    // TODO: triplanar
    // TODO: POM

    float4 albedo[_LAYER_COUNT];
    float3 normal[_LAYER_COUNT];
    float4 masks[_LAYER_COUNT];

#ifdef _NORMALMAP
    #define SampleNormalTriplanar(i) \
        float4 cXn = SAMPLE_TEXTURE2D(_Normal##i, sampler_Splat0, splatuvY);           \
        float4 cYn = SAMPLE_TEXTURE2D(_Normal##i, sampler_Splat0, splatuvX);           \
        float4 cZn = SAMPLE_TEXTURE2D(_Normal##i, sampler_Splat0, splatuvZ);           \
        float4 sideN = lerp(cXn, cZn, abs(normalWS.z));                                \
        float4 topN = lerp(sideN, cYn, abs(normalWS.y));                               \
        normal[i] = UnpackNormalDigger(topN, _NormalScale##i);
#else
    #define SampleNormalTriplanar(i) float3(0, 0, 0)
#endif

#define DefaultMask(i) float4(_Metallic##i, _MaskMapRemapOffset##i.y + _MaskMapRemapScale##i.y, _MaskMapRemapOffset##i.z + 0.5 * _MaskMapRemapScale##i.z, albedo[i].a * _Smoothness##i)

#ifdef _MASKMAP
    #define MaskModeMasks(i, blendMask) RemapMasks(SAMPLE_TEXTURE2D_GRAD(_Mask##i, sampler_Splat0, splatuv, splatdxuv, splatdyuv), blendMask, _MaskMapRemapOffset##i, _MaskMapRemapScale##i)
    #define NullMask(i)               float4(0, 1, _MaskMapRemapOffset##i.z, 0) // only height matters when weight is zero.
    #define SampleMasksTriplanar(i, blendMask) \
        float4 cXm = SAMPLE_TEXTURE2D(_Mask##i, sampler_Splat0, splatuvY);           \
        float4 cYm = SAMPLE_TEXTURE2D(_Mask##i, sampler_Splat0, splatuvX);           \
        float4 cZm = SAMPLE_TEXTURE2D(_Mask##i, sampler_Splat0, splatuvZ);           \
        float4 sideM = lerp(cXm, cZm, abs(normalWS.z));                              \
        float4 topM = lerp(sideM, cYm, abs(normalWS.y));                             \
        float4 maskModeMasksTriplanar = RemapMasks(topM, blendMask, _MaskMapRemapOffset##i, _MaskMapRemapScale##i); \
        masks[i] = lerp(DefaultMask(i), maskModeMasksTriplanar, _LayerHasMask##i);
#else
    #define NullMask(i)               float4(0, 1, 0, 0)
    #define SampleMasksTriplanar(i, blendMask) DefaultMask(i)
#endif
    
#define SampleResultsTriplanar(i, mask)                                                                         \
    UNITY_BRANCH if (mask > 1e-2f)                                                                              \
    {                                                                                                           \
        float2 splatuv = splatBaseUV * _Splat##i##_ST.xy + _Splat##i##_ST.zw;                                   \
        float2 splatdxuv = dxuv * _Splat##i##_ST.x;                                                             \
        float2 splatdyuv = dyuv * _Splat##i##_ST.y;                                                             \
        float2 tile = _Splat##i##_ST.xy * _TerrainSizeInv;                                                      \
        float2 splatuvY = positionWS.zy * tile + _Splat##i##_ST.zw;                                             \
        float2 splatuvX = positionWS.xz * tile + _Splat##i##_ST.zw;                                             \
        float2 splatuvZ = positionWS.xy * tile + _Splat##i##_ST.zw;                                             \
        float4 cX = SAMPLE_TEXTURE2D(_Splat##i, sampler_Splat0, splatuvY);                                      \
        float4 cY = SAMPLE_TEXTURE2D(_Splat##i, sampler_Splat0, splatuvX);                                      \
        float4 cZ = SAMPLE_TEXTURE2D(_Splat##i, sampler_Splat0, splatuvZ);                                      \
        float4 side = lerp(cX, cZ, abs(normalWS.z));                                                            \
        float4 top = lerp(side, cY, abs(normalWS.y));                                                           \
        albedo[i] = top;                                                                                        \
        albedo[i].rgb *= _DiffuseRemapScale##i.xyz;                                                             \
        SampleNormalTriplanar(i);                                                                               \
        SampleMasksTriplanar(i, mask);                                                                          \
    }                                                                                                           \
    else                                                                                                        \
    {                                                                                                           \
        albedo[i] = float4(0, 0, 0, 0);                                                                         \
        normal[i] = float3(0, 0, 0);                                                                            \
        masks[i] = NullMask(i);                                                                                 \
    }

    float2 _TerrainSizeInv = float2(_TerrainWidthInv, _TerrainHeightInv);
    float3 positionWS = GetAbsolutePositionWS(input.positionRWS);
    float3 normalWS = input.tangentToWorld[2].xyz;

    // Derivatives are not available for ray tracing for now
#if defined(SHADER_STAGE_RAY_TRACING)
#else
    float3 dx = ddx(positionWS);
    float3 dy = ddy(positionWS);
    float3 flatNormal = normalize(cross(dy, dx));
    
    // min of the barycentric coordinates is how close to an edge we are
    float3 bary = GetBary(splatBaseUV);
    float mb = min(bary.x, min(bary.y, bary.z));
    mb = saturate(mb * 20.0f);
    
    // now blend the normal
    normalWS = abs(lerp(normalWS, flatNormal, mb));
#endif

    // Derivatives are not available for ray tracing for now
#if defined(SHADER_STAGE_RAY_TRACING)
    float2 dxuv = 0;
    float2 dyuv = 0;
#else
    float2 dxuv = ddx(splatBaseUV);
    float2 dyuv = ddy(splatBaseUV);
#endif

    float2 blendUV0 = (controlUV.xy * (_Control0_TexelSize.zw - 1.0f) + 0.5f) * _Control0_TexelSize.xy;
    float4 blendMasks0 = SAMPLE_TEXTURE2D(_Control0, sampler_Control0, blendUV0);
    #ifdef _TERRAIN_8_LAYERS
        float2 blendUV1 = (controlUV.xy * (_Control1_TexelSize.zw - 1.0f) + 0.5f) * _Control1_TexelSize.xy;
        float4 blendMasks1 = SAMPLE_TEXTURE2D(_Control1, sampler_Control0, blendUV1);
    #else
        float4 blendMasks1 = float4(0, 0, 0, 0);
    #endif

    SampleResultsTriplanar(0, blendMasks0.x);
    SampleResultsTriplanar(1, blendMasks0.y);
    SampleResultsTriplanar(2, blendMasks0.z);
    SampleResultsTriplanar(3, blendMasks0.w);
    #ifdef _TERRAIN_8_LAYERS
        SampleResultsTriplanar(4, blendMasks1.x);
        SampleResultsTriplanar(5, blendMasks1.y);
        SampleResultsTriplanar(6, blendMasks1.z);
        SampleResultsTriplanar(7, blendMasks1.w);
    #endif

#undef SampleNormalTriplanar
#undef SampleMasksTriplanar
#undef SampleResultsTriplanar

    float weights[_LAYER_COUNT];
    ZERO_INITIALIZE_ARRAY(float, weights, _LAYER_COUNT);

    #ifdef _MASKMAP
        #if defined(_TERRAIN_BLEND_HEIGHT)
            // Modify blendMask to take into account the height of the layer. Higher height should be more visible.
            float maxHeight = masks[0].z;
            maxHeight = max(maxHeight, masks[1].z);
            maxHeight = max(maxHeight, masks[2].z);
            maxHeight = max(maxHeight, masks[3].z);
            #ifdef _TERRAIN_8_LAYERS
                maxHeight = max(maxHeight, masks[4].z);
                maxHeight = max(maxHeight, masks[5].z);
                maxHeight = max(maxHeight, masks[6].z);
                maxHeight = max(maxHeight, masks[7].z);
            #endif

            // Make sure that transition is not zero otherwise the next computation will be wrong.
            // The epsilon here also has to be bigger than the epsilon in the next computation.
            float transition = max(_HeightTransition, 1e-5);

            // The goal here is to have all but the highest layer at negative heights, then we add the transition so that if the next highest layer is near transition it will have a positive value.
            // Then we clamp this to zero and normalize everything so that highest layer has a value of 1.
            float4 weightedHeights0 = { masks[0].z, masks[1].z, masks[2].z, masks[3].z };
            weightedHeights0 = weightedHeights0 - maxHeight.xxxx;
            // We need to add an epsilon here for active layers (hence the blendMask again) so that at least a layer shows up if everything's too low.
            weightedHeights0 = (max(0, weightedHeights0 + transition) + 1e-6) * blendMasks0;

            #ifdef _TERRAIN_8_LAYERS
                float4 weightedHeights1 = { masks[4].z, masks[5].z, masks[6].z, masks[7].z };
                weightedHeights1 = weightedHeights1 - maxHeight.xxxx;
                weightedHeights1 = (max(0, weightedHeights1 + transition) + 1e-6) * blendMasks1;
            #else
                float4 weightedHeights1 = { 0, 0, 0, 0 };
            #endif

            // Normalize
            float sumHeight = GetSumHeight(weightedHeights0, weightedHeights1);
            blendMasks0 = weightedHeights0 / sumHeight.xxxx;
            #ifdef _TERRAIN_8_LAYERS
                blendMasks1 = weightedHeights1 / sumHeight.xxxx;
            #endif
        #elif defined(_TERRAIN_BLEND_DENSITY)
            // Denser layers are more visible.
            float4 opacityAsDensity0 = saturate((float4(albedo[0].a, albedo[1].a, albedo[2].a, albedo[3].a) - (float4(1.0, 1.0, 1.0, 1.0) - blendMasks0)) * 20.0); // 20.0 is the number of steps in inputAlphaMask (Density mask. We decided 20 empirically)
            opacityAsDensity0 += 0.001f * blendMasks0;      // if all weights are zero, default to what the blend mask says
            float4 useOpacityAsDensityParam0 = { _DiffuseRemapScale0.w, _DiffuseRemapScale1.w, _DiffuseRemapScale2.w, _DiffuseRemapScale3.w }; // 1 is off
            blendMasks0 = lerp(opacityAsDensity0, blendMasks0, useOpacityAsDensityParam0);
            #ifdef _TERRAIN_8_LAYERS
                float4 opacityAsDensity1 = saturate((float4(albedo[4].a, albedo[5].a, albedo[6].a, albedo[7].a) - (float4(1.0, 1.0, 1.0, 1.0) - blendMasks1)) * 20.0); // 20.0 is the number of steps in inputAlphaMask (Density mask. We decided 20 empirically)
                opacityAsDensity1 += 0.001f * blendMasks1;  // if all weights are zero, default to what the blend mask says
                float4 useOpacityAsDensityParam1 = { _DiffuseRemapScale4.w, _DiffuseRemapScale5.w, _DiffuseRemapScale6.w, _DiffuseRemapScale7.w };
                blendMasks1 = lerp(opacityAsDensity1, blendMasks1, useOpacityAsDensityParam1);
            #endif

            // Normalize
            float sumHeight = GetSumHeight(blendMasks0, blendMasks1);
            blendMasks0 = blendMasks0 / sumHeight.xxxx;
            #ifdef _TERRAIN_8_LAYERS
                blendMasks1 = blendMasks1 / sumHeight.xxxx;
            #endif
        #endif // if _TERRAIN_BLEND_HEIGHT
    #endif // if _MASKMAP

    weights[0] = blendMasks0.x;
    weights[1] = blendMasks0.y;
    weights[2] = blendMasks0.z;
    weights[3] = blendMasks0.w;
    #ifdef _TERRAIN_8_LAYERS
        weights[4] = blendMasks1.x;
        weights[5] = blendMasks1.y;
        weights[6] = blendMasks1.z;
        weights[7] = blendMasks1.w;
    #endif

    surfaceData.albedo = 0;
    surfaceData.normalData = 0;
    float3 outMasks = 0;
    UNITY_UNROLL for (int i = 0; i < _LAYER_COUNT; ++i)
    {
        surfaceData.albedo += albedo[i].rgb * weights[i];
        surfaceData.normalData += normal[i].rgb * weights[i]; // no need to normalize
        outMasks += masks[i].xyw * weights[i];
    }
    surfaceData.smoothness = outMasks.z;
    surfaceData.metallic = outMasks.x;
    surfaceData.ao = outMasks.y;
}

void TerrainLitShade(float2 uv, FragInputs input, inout TerrainLitSurfaceData surfaceData)
{
    TerrainSplatBlend(uv, uv, input, surfaceData);
}

void TerrainLitDebug(float2 uv, uint2 screenSpaceCoords, out float3 baseColor)
{
#ifdef DEBUG_DISPLAY
    if (_DebugMipMapModeTerrainTexture == DEBUGMIPMAPMODETERRAINTEXTURE_CONTROL)
    {
        baseColor = GET_TEXTURE_STREAMING_DEBUG_FOR_TERRAIN_TEX(screenSpaceCoords, uv, _Control0);
    }
    else if (_DebugMipMapModeTerrainTexture == DEBUGMIPMAPMODETERRAINTEXTURE_LAYER0)
    {
        baseColor = GET_TEXTURE_STREAMING_DEBUG_FOR_TERRAIN_TEX(screenSpaceCoords, uv * _Splat0_ST.xy + _Splat0_ST.zw, _Splat0);
    }
    else if (_DebugMipMapModeTerrainTexture == DEBUGMIPMAPMODETERRAINTEXTURE_LAYER1)
    {
        baseColor = GET_TEXTURE_STREAMING_DEBUG_FOR_TERRAIN_TEX(screenSpaceCoords, uv * _Splat1_ST.xy + _Splat1_ST.zw, _Splat1);
    }
    else if (_DebugMipMapModeTerrainTexture == DEBUGMIPMAPMODETERRAINTEXTURE_LAYER2)
    {
        baseColor = GET_TEXTURE_STREAMING_DEBUG_FOR_TERRAIN_TEX(screenSpaceCoords, uv * _Splat2_ST.xy + _Splat2_ST.zw, _Splat2);
    }
    else if (_DebugMipMapModeTerrainTexture == DEBUGMIPMAPMODETERRAINTEXTURE_LAYER3)
    {
        baseColor = GET_TEXTURE_STREAMING_DEBUG_FOR_TERRAIN_TEX(screenSpaceCoords, uv * _Splat3_ST.xy + _Splat3_ST.zw, _Splat3);
    }
    #ifdef _TERRAIN_8_LAYERS
        else if (_DebugMipMapModeTerrainTexture == DEBUGMIPMAPMODETERRAINTEXTURE_LAYER4)
        {
            baseColor = GET_TEXTURE_STREAMING_DEBUG_FOR_TERRAIN_TEX(screenSpaceCoords, uv * _Splat4_ST.xy + _Splat4_ST.zw, _Splat4);
        }
        else if (_DebugMipMapModeTerrainTexture == DEBUGMIPMAPMODETERRAINTEXTURE_LAYER5)
        {
            baseColor = GET_TEXTURE_STREAMING_DEBUG_FOR_TERRAIN_TEX(screenSpaceCoords, uv * _Splat5_ST.xy + _Splat5_ST.zw, _Splat5);
        }
        else if (_DebugMipMapModeTerrainTexture == DEBUGMIPMAPMODETERRAINTEXTURE_LAYER6)
        {
            baseColor = GET_TEXTURE_STREAMING_DEBUG_FOR_TERRAIN_TEX(screenSpaceCoords, uv * _Splat6_ST.xy + _Splat6_ST.zw, _Splat6);
        }
        else if (_DebugMipMapModeTerrainTexture == DEBUGMIPMAPMODETERRAINTEXTURE_LAYER7)
        {
            baseColor = GET_TEXTURE_STREAMING_DEBUG_FOR_TERRAIN_TEX(screenSpaceCoords, uv * _Splat7_ST.xy + _Splat7_ST.zw, _Splat7);
        }
    #else
        else
        {
            // User is trying to debug layer 4/5/6/7 but this terrain only has 4 layers: let's try to display some basic "invalid" debug info...
            baseColor = GET_TEXTURE_STREAMING_DEBUG_FOR_TERRAIN_NO_TEX(screenSpaceCoords, uv);
        }
    #endif
#endif
}
