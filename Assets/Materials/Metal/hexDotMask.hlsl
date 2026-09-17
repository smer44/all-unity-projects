void HexDotMask_float(float2 UV, float DotRadius, float Softness, out float Mask)
{
    // Unit triangular lattice basis. Adjacent dot centers are 1 unit apart.
    float2 hexA = float2(1.0, 0.0);
    float2 hexB = float2(0.5, 0.8660254); // sqrt(3) / 2

    // Convert UV into lattice coordinates.
    float2x2 invBasis = float2x2(
        1.0, -0.57735027,
        0.0,  1.15470054
    );

    float2 grid = mul(invBasis, UV);
    float2 baseCell = floor(grid);

    // Rounding skewed lattice coordinates can pick the wrong center near
    // Voronoi edges, so test the surrounding lattice points in UV space.
    float minDistSq = 1e20;
    [unroll]
    for (int y = -1; y <= 1; y++)
    {
        [unroll]
        for (int x = -1; x <= 1; x++)
        {
            float2 cell = baseCell + float2(x, y);
            float2 center = cell.x * hexA + cell.y * hexB;
            float2 delta = UV - center;
            minDistSq = min(minDistSq, dot(delta, delta));
        }
    }

    Mask = sqrt(minDistSq);
}
