using UnityEngine;

public class TerrainToMeshExample
{
    public static Mesh BuildMesh(Terrain terrain, int step = 1)
    {
        TerrainData td = terrain.terrainData;
        int hm = td.heightmapResolution;
        Vector3 size = td.size;

        int w = (hm - 1) / step + 1;
        int h = (hm - 1) / step + 1;

        Vector3[] verts = new Vector3[w * h];
        Vector2[] uvs = new Vector2[w * h];
        int[] tris = new int[(w - 1) * (h - 1) * 6];

        int vi = 0;
        for (int y = 0; y < hm; y += step)
        {
            for (int x = 0; x < hm; x += step)
            {
                float xf = (float)x / (hm - 1);
                float yf = (float)y / (hm - 1);
                float height = td.GetHeight(x, y);

                verts[vi] = new Vector3(xf * size.x, height, yf * size.z);
                uvs[vi] = new Vector2(xf, yf);
                vi++;
            }
        }

        int ti = 0;
        for (int y = 0; y < h - 1; y++)
        {
            for (int x = 0; x < w - 1; x++)
            {
                int i = y * w + x;
                tris[ti++] = i;
                tris[ti++] = i + w;
                tris[ti++] = i + 1;

                tris[ti++] = i + 1;
                tris[ti++] = i + w;
                tris[ti++] = i + w + 1;
            }
        }

        Mesh mesh = new Mesh();
        mesh.indexFormat = verts.Length > 65535
            ? UnityEngine.Rendering.IndexFormat.UInt32
            : UnityEngine.Rendering.IndexFormat.UInt16;

        mesh.vertices = verts;
        mesh.uv = uvs;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }
}