using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RubicLike", menuName = "Grid/RubicLike")]
public class RubicLike : ScriptableObject
{
    [SerializeField]
    private Vector3Int size = new Vector3Int(3, 3, 3);

    [SerializeField]
    private Vector3Int[] coordinates = CreateDefault3x3x3MissingTopMiddle();

    public Vector3Int Size => size;

    public IReadOnlyList<Vector3Int> Coordinates => coordinates;

    public Vector3Int[] GetCoordinatesCopy()
    {
        return coordinates == null
            ? new Vector3Int[0]
            : (Vector3Int[])coordinates.Clone();
    }

    public bool Contains(Vector3Int coordinate)
    {
        if (coordinates == null)
            return false;

        for (int i = 0; i < coordinates.Length; i++)
        {
            if (coordinates[i] == coordinate)
                return true;
        }

        return false;
    }

    public bool IsInsideBounds(Vector3Int coordinate)
    {
        return coordinate.x >= 0 && coordinate.x < size.x
            && coordinate.y >= 0 && coordinate.y < size.y
            && coordinate.z >= 0 && coordinate.z < size.z;
    }

    [ContextMenu("Set Default 3x3x3 Missing Top Middle")]
    private void SetDefault3x3x3MissingTopMiddle()
    {
        size = new Vector3Int(3, 3, 3);
        coordinates = CreateDefault3x3x3MissingTopMiddle();
    }

    private static Vector3Int[] CreateDefault3x3x3MissingTopMiddle()
    {
        Vector3Int defaultSize = new Vector3Int(3, 3, 3);

        // Assuming Y is up:
        // top middle cell in a 3x3x3 grid is (1, 2, 1).
        Vector3Int missingCell = new Vector3Int(1, 2, 1);

        List<Vector3Int> result = new List<Vector3Int>();

        for (int x = 0; x < defaultSize.x; x++)
        {
            for (int y = 0; y < defaultSize.y; y++)
            {
                for (int z = 0; z < defaultSize.z; z++)
                {
                    Vector3Int coordinate = new Vector3Int(x, y, z);

                    if (coordinate == missingCell)
                        continue;

                    result.Add(coordinate);
                }
            }
        }

        return result.ToArray();
    }
}