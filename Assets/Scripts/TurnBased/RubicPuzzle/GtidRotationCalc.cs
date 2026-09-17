using UnityEngine;
using System;

public enum GridAxis
{
    X,
    Y,
    Z
}

public enum RotationDirection
{
    Clockwise,
    CounterClockwise
}



public class GtidRotationCalc : MonoBehaviour
{
    /// <summary>
    /// Rotates only coordinates that are on the selected grid layer.
    ///
    /// Example:
    /// axis = GridAxis.X, plane = 2
    /// Rotates all coordinates where coord.x == 2 around the X axis,
    /// meaning their Y/Z coordinates rotate in the YZ plane.
    ///
    /// Returns a new array. The original array is not modified.
    /// </summary>
    public static Vector3Int[] RotateLayer90(
        Vector3Int[] coordinates,
        GridAxis axis,
        int plane,
        RotationDirection direction,
        Vector3Int pivot)
    {
        if (coordinates == null)
            throw new ArgumentNullException(nameof(coordinates));

        Vector3Int[] result = new Vector3Int[coordinates.Length];

        for (int i = 0; i < coordinates.Length; i++)
        {
            Vector3Int coord = coordinates[i];

            if (GetAxisValue(coord, axis) == plane)
            {
                result[i] = Rotate90AroundPivot(coord, pivot, axis, direction);
            }
            else
            {
                result[i] = coord;
            }
        }

        return result;
    }

    public static Vector3Int Rotate90AroundPivot(
        Vector3Int coord,
        Vector3Int pivot,
        GridAxis axis,
        RotationDirection direction)
    {
        Vector3Int local = coord - pivot;
        Vector3Int rotatedLocal = Rotate90(local, axis, direction);
        return rotatedLocal + pivot;
    }

    public static Vector3Int Rotate90(
        Vector3Int coord,
        GridAxis axis,
        RotationDirection direction)
    {
        int x = coord.x;
        int y = coord.y;
        int z = coord.z;

        bool clockwise = direction == RotationDirection.Clockwise;

        switch (axis)
        {
            case GridAxis.X:
                // Rotate in YZ plane
                return clockwise
                    ? new Vector3Int(x, z, -y)
                    : new Vector3Int(x, -z, y);

            case GridAxis.Y:
                // Rotate in XZ plane
                return clockwise
                    ? new Vector3Int(-z, y, x)
                    : new Vector3Int(z, y, -x);

            case GridAxis.Z:
                // Rotate in XY plane
                return clockwise
                    ? new Vector3Int(y, -x, z)
                    : new Vector3Int(-y, x, z);

            default:
                return coord;
        }
    }


    private static int GetAxisValue(Vector3Int coord, GridAxis axis)
    {
        switch (axis)
        {
            case GridAxis.X:
                return coord.x;

            case GridAxis.Y:
                return coord.y;

            case GridAxis.Z:
                return coord.z;

            default:
                throw new ArgumentOutOfRangeException(nameof(axis), axis, null);
        }
    }

}
