using UnityEngine;
using UnityEngine.Rendering;

public class MultiMeshLikeRenderer : MonoBehaviour
{
    public Mesh mesh;
    public Material material;

    public Vector3 scale = Vector3.one;
    public Quaternion rotation = Quaternion.identity;

    Matrix4x4[] matrices;

    const int BatchSize = 511;

    public void Rebuild(Vector3[] positions)
    {
        if (positions == null || positions.Length == 0)
        {
            matrices = null;
            return;
        }

        matrices = new Matrix4x4[positions.Length];

        for (int i = 0; i < positions.Length; i++)
        {
            matrices[i] = Matrix4x4.TRS(
                positions[i],
                rotation,
                scale
            );
        }

        if (material != null)
        {
            material.enableInstancing = true;
        }
    }

    public void Rebuild(Vector3Int[] positions)
    {
        if (positions == null || positions.Length == 0)
        {
            matrices = null;
            return;
        }

        matrices = new Matrix4x4[positions.Length];

        for (int i = 0; i < positions.Length; i++)
        {
            matrices[i] = Matrix4x4.TRS(
                (Vector3)positions[i],
                rotation,
                scale
            );
        }

        if (material != null)
        {
            material.enableInstancing = true;
        }
    }

    void Update()
    {
        if (mesh == null || material == null || matrices == null)
            return;

        RenderParams renderParams = new RenderParams(material)
        {
            shadowCastingMode = ShadowCastingMode.On,
            receiveShadows = true
        };

        for (int i = 0; i < matrices.Length; i += BatchSize)
        {
            int count = Mathf.Min(BatchSize, matrices.Length - i);

            Graphics.RenderMeshInstanced(
                renderParams,
                mesh,
                0,
                matrices,
                count,
                i
            );
        }
    }
}