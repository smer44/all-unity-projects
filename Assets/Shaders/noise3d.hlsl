// ------------------------------------------------------------
// Hash function: returns a pseudo-random value from a 3D point.
// Input is usually an integer lattice position.
// Output range: 0..1
// ------------------------------------------------------------
float VN3D_Hash32(float3 p)
{
    // Different constants for each axis are important.
    p = frac(p * float3(0.1031, 0.11369, 0.13787));

    // Different bias per axis breaks x/y/z symmetry.
    p += dot(p, p.yzx + float3(19.19, 37.37, 11.11));

    // Asymmetric final mixing.
    return frac((p.x + p.y * 7.13) * (p.z + 3.31));
}

float VN3D_Hash31(float3 p)
{
    return frac(sin(dot(p, float3(127.1, 311.7, 74.7))) * 43758.5453123);
}

// ------------------------------------------------------------
// Smooth interpolation curve.
// This is smoother than f*f*(3 - 2*f).
// ------------------------------------------------------------
float3 VN3D_Fade(float3 t)
{
    return t * t * t * (t * (t * 6.0 - 15.0) + 10.0);
}

// ------------------------------------------------------------
// Core 3D value noise.
// Position: usually Object Space Position or World Space Position.
// Scale: how many noise cells per unit.
// Out: noise value from 0..1
// ------------------------------------------------------------
void ValueNoise3D_float(float3 Position, float Scale, out float Out)
{
    float3 p = Position * max(Scale, 0.0001);

    float3 i = floor(p);
    float3 f = frac(p);

    float3 u = VN3D_Fade(f);

    float v000 = VN3D_Hash31(i + float3(0.0, 0.0, 0.0));
    float v100 = VN3D_Hash31(i + float3(1.0, 0.0, 0.0));
    float v010 = VN3D_Hash31(i + float3(0.0, 1.0, 0.0));
    float v110 = VN3D_Hash31(i + float3(1.0, 1.0, 0.0));

    float v001 = VN3D_Hash31(i + float3(0.0, 0.0, 1.0));
    float v101 = VN3D_Hash31(i + float3(1.0, 0.0, 1.0));
    float v011 = VN3D_Hash31(i + float3(0.0, 1.0, 1.0));
    float v111 = VN3D_Hash31(i + float3(1.0, 1.0, 1.0));

    float x00 = lerp(v000, v100, u.x);
    float x10 = lerp(v010, v110, u.x);
    float x01 = lerp(v001, v101, u.x);
    float x11 = lerp(v011, v111, u.x);

    float y0 = lerp(x00, x10, u.y);
    float y1 = lerp(x01, x11, u.y);

    Out = lerp(y0, y1, u.z);
}

// Half precision wrapper for Shader Graph.
void ValueNoise3D_half(half3 Position, half Scale, out half Out)
{
    float result;
    ValueNoise3D_float((float3)Position, (float)Scale, result);
    Out = (half)result;
}


// ------------------------------------------------------------
// Fractal Brownian Motion using 3D value noise.
// Adds several octaves of value noise together.
// Octaves should usually be 1..8.
// Lacunarity usually around 2.
// Gain usually around 0.5.
// Output range: approximately 0..1
// ------------------------------------------------------------
void ValueNoise3D_FBM_float(
    float3 Position,
    float Scale,
    float Octaves,
    float Lacunarity,
    float Gain,
    out float Out)
{
    int octaveCount = clamp((int)Octaves, 1, 8);

    float sum = 0.0;
    float amp = 1.0;
    float ampSum = 0.0;
    float freq = Scale;

    [unroll]
    for (int o = 0; o < 8; o++)
    {
        if (o >= octaveCount)
            break;

        float n;
        ValueNoise3D_float(Position, freq, n);

        sum += n * amp;
        ampSum += amp;

        freq *= Lacunarity;
        amp *= Gain;
    }

    Out = sum / max(ampSum, 0.0001);
}

