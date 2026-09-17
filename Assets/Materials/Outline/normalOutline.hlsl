#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"


float SampleOutlineDeviceDepth(float2 screenUV)
{
#if UNITY_REVERSED_Z
    return SampleSceneDepth(screenUV);
#else
    return lerp(UNITY_NEAR_CLIP_VALUE, 1.0, SampleSceneDepth(screenUV));
#endif
}


float3 ReconstructOutlineWorldPos(float2 screenUV)
{
    return ComputeWorldSpacePosition(screenUV, SampleOutlineDeviceDepth(screenUV), UNITY_MATRIX_I_VP);
}


float3 ReconstructOutlineNormal(float2 screenUV, float2 px)
{
    float3 p = ReconstructOutlineWorldPos(screenUV);
    float3 pr = ReconstructOutlineWorldPos(screenUV + float2(px.x, 0.0));
    float3 pu = ReconstructOutlineWorldPos(screenUV + float2(0.0, px.y));

    return SafeNormalize(cross(pu - p, pr - p));
}


void NormalBasedOutlines_float(float2 screenUV, float2 px, float3x3 sharr_x, float3x3 sharr_y, out float outlines)
{
    float3 centerNormal = ReconstructOutlineNormal(screenUV, px);
    float edge = 0.0;
    float3 n;

    n = ReconstructOutlineNormal(screenUV + float2(-1.0, 0.0) * px, px);
    edge = max(edge, 1.0 - saturate(dot(centerNormal, n)));

    n = ReconstructOutlineNormal(screenUV + float2(1.0, 0.0) * px, px);
    edge = max(edge, 1.0 - saturate(dot(centerNormal, n)));

    n = ReconstructOutlineNormal(screenUV + float2(0.0, -1.0) * px, px);
    edge = max(edge, 1.0 - saturate(dot(centerNormal, n)));

    n = ReconstructOutlineNormal(screenUV + float2(0.0, 1.0) * px, px);
    edge = max(edge, 1.0 - saturate(dot(centerNormal, n)));

    outlines = step(0.95, edge);
}
