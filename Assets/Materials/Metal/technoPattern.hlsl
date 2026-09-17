// Assets/Shaders/TechPanelPattern.hlsl

#ifndef TECH_PANEL_PATTERN_INCLUDED
#define TECH_PANEL_PATTERN_INCLUDED

float TechHash21(float2 p)
{
    p = frac(p * float2(123.34, 456.21));
    p += dot(p, p + 45.32);
    return frac(p.x * p.y);
}

float2 TechRotate(float2 p, float a)
{
    float s = sin(a);
    float c = cos(a);
    return float2(c * p.x - s * p.y, s * p.x + c * p.y);
}

float TechLineSegment(float2 p, float2 a, float2 b, float width, float aa)
{
    float2 pa = p - a;
    float2 ba = b - a;
    float h = saturate(dot(pa, ba) / max(dot(ba, ba), 0.00001));
    float d = length(pa - ba * h);

    return 1.0 - smoothstep(width, width + aa, d);
}

void TechPanelPattern_float(
    float2 UV,
    float Aspect,
    float Scale,
    float Angle,
    float MainWidth,
    float ThinWidth,
    float Seed,
    out float MainMask,
    out float ThinMask
)
{
    UV.x *= Aspect;

    float2 p = UV - 0.5;
    p = TechRotate(p, Angle);
    p *= Scale;

    float2 cell = floor(p);
    float2 f = frac(p);

    float aa = max(fwidth(p.x), fwidth(p.y)) * 1.4;

    float h0 = TechHash21(cell + Seed);
    float h1 = TechHash21(cell + Seed + 13.71);
    float h2 = TechHash21(cell + Seed + 41.19);
    float h3 = TechHash21(cell + Seed + 91.43);

    MainMask = 0.0;
    ThinMask = 0.0;

    // Main diagonal traces
    float flip = step(0.5, h0);

    float mainDiagA = TechLineSegment(
        f,
        float2(-0.20, 0.18),
        float2(1.20, 0.88),
        MainWidth,
        aa
    );

    float mainDiagB = TechLineSegment(
        f,
        float2(-0.20, 0.82),
        float2(1.20, 0.12),
        MainWidth,
        aa
    );

    MainMask = max(MainMask, lerp(mainDiagA, mainDiagB, flip) * step(0.28, h1));

    // Main vertical / stepped branches
    float branchX = lerp(0.22, 0.78, step(0.5, h2));
    float branchGate = step(0.48, h0);

    MainMask = max(
        MainMask,
        TechLineSegment(
            f,
            float2(branchX, -0.15),
            float2(branchX, 0.42),
            MainWidth,
            aa
        ) * branchGate
    );

    MainMask = max(
        MainMask,
        TechLineSegment(
            f,
            float2(branchX, 0.42),
            float2(branchX + lerp(-0.28, 0.28, flip), 0.72),
            MainWidth,
            aa
        ) * branchGate
    );

    // Secondary thin panel lines
    float thinY = lerp(0.24, 0.76, step(0.5, h3));
    float thinGateA = step(0.22, h2);
    float thinGateB = step(0.45, h1);

    ThinMask = max(
        ThinMask,
        TechLineSegment(
            f,
            float2(-0.10, thinY),
            float2(0.36, thinY),
            ThinWidth,
            aa
        ) * thinGateA
    );

    ThinMask = max(
        ThinMask,
        TechLineSegment(
            f,
            float2(0.36, thinY),
            float2(0.62, thinY + lerp(-0.26, 0.26, flip)),
            ThinWidth,
            aa
        ) * thinGateA
    );

    ThinMask = max(
        ThinMask,
        TechLineSegment(
            f,
            float2(0.62, thinY + lerp(-0.26, 0.26, flip)),
            float2(1.10, thinY + lerp(-0.26, 0.26, flip)),
            ThinWidth,
            aa
        ) * thinGateA
    );

    // Thin vertical details
    float thinX = lerp(0.14, 0.86, step(0.5, h1));

    ThinMask = max(
        ThinMask,
        TechLineSegment(
            f,
            float2(thinX, -0.10),
            float2(thinX, 0.58),
            ThinWidth,
            aa
        ) * thinGateB
    );

    // Remove thin lines underneath thick lines
    ThinMask *= 1.0 - saturate(MainMask);

    MainMask = saturate(MainMask);
    ThinMask = saturate(ThinMask);
}

#endif