#ifndef OUTLINE_INCLUDED
#define OUTLINE_INCLUDED

float SampleOutlineSceneDepth(float2 screenUV)
{
#if defined(SHADERGRAPH_SAMPLE_SCENE_DEPTH)
    return SHADERGRAPH_SAMPLE_SCENE_DEPTH(screenUV);
#else
    return SampleSceneDepth(screenUV);
#endif
}


void DepthBasedOutlines_float(float2 screenUV, float2 px, float3x3 sharr_x, float3x3 sharr_y, out float outlines)
{

    float gx = 0;
    float gy = 0;
    float d = 0;

    d = SampleOutlineSceneDepth(screenUV + float2(-1.0, -1.0) * px);
    gx += d * sharr_x._m00;
    gy += d * sharr_y._m00;

    d = SampleOutlineSceneDepth(screenUV + float2(0.0, -1.0) * px);
    gx += d * sharr_x._m01;
    gy += d * sharr_y._m01;

    d = SampleOutlineSceneDepth(screenUV + float2(1.0, -1.0) * px);
    gx += d * sharr_x._m02;
    gy += d * sharr_y._m02;

    d = SampleOutlineSceneDepth(screenUV + float2(-1.0, 0.0) * px);
    gx += d * sharr_x._m10;
    gy += d * sharr_y._m10;

    d = SampleOutlineSceneDepth(screenUV + float2(1.0, 0.0) * px);
    gx += d * sharr_x._m12;
    gy += d * sharr_y._m12;

    d = SampleOutlineSceneDepth(screenUV + float2(-1.0, 1.0) * px);
    gx += d * sharr_x._m20;
    gy += d * sharr_y._m20;

    d = SampleOutlineSceneDepth(screenUV + float2(0.0, 1.0) * px);
    gx += d * sharr_x._m21;
    gy += d * sharr_y._m21;

    d = SampleOutlineSceneDepth(screenUV + float2(1.0, 1.0) * px);
    gx += d * sharr_x._m22;
    gy += d * sharr_y._m22;

    float g = sqrt(gx * gx + gy * gy);
    outlines = step(0.01, g);
}

#endif
