//UNITY_SHADER_NO_UPGRADE
#ifndef PARTICLE_DOTS_SHADER_GRAPH_INCLUDED
#define PARTICLE_DOTS_SHADER_GRAPH_INCLUDED

#define MAX_DISPLAY_PARTICLES 256
#define MAX_PARTICLE_TYPES 16

StructuredBuffer<float2> _ParticlePositions;
StructuredBuffer<int>    _ParticleTypes;

int _ParticleCount;

float _ParticlePositionScale;
float _DotRadius;
float _DotSoftness;

float4 _ParticleTypeColors[MAX_PARTICLE_TYPES];

void ParticleDots_float(
    float3 WorldPosition,
    out float4 OutColor
)
{
    float2 pixelPos = WorldPosition.xy;

    float4 accum = float4(0.0, 0.0, 0.0, 0.0);

    int count = min(_ParticleCount, MAX_DISPLAY_PARTICLES);

    for (int i = 0; i < count; i++)
    {
        float2 particlePos = _ParticlePositions[i] * _ParticlePositionScale;

        float d = distance(pixelPos, particlePos);

        float alpha = 1.0 - smoothstep(
            _DotRadius,
            _DotRadius + _DotSoftness,
            d
        );

        if (alpha <= 0.0)
            continue;

        int particleType = _ParticleTypes[i];
        particleType = max(0, min(particleType, MAX_PARTICLE_TYPES - 1));

        float4 particleColor = _ParticleTypeColors[particleType];

        // Simple alpha accumulation.
        accum.rgb = lerp(accum.rgb, particleColor.rgb, alpha * particleColor.a);
        accum.a = saturate(accum.a + alpha * particleColor.a);
    }

    OutColor = accum;
}

#endif