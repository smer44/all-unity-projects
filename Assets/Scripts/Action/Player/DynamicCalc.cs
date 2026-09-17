using UnityEngine;

public static class DynamicCalc
{
    public static Vector3 MoveWASDDynamic(
        Vector2 moveInputRotated,
        Vector3 currentLinearVelocity,
        Transform referenceFrame)
    {
        var desiredWorldVelocity = new Vector3(moveInputRotated.x, 0f, moveInputRotated.y);
        var desiredVelocity = new Vector3(
            desiredWorldVelocity.x,
            currentLinearVelocity.y,
            desiredWorldVelocity.z);

        return referenceFrame != null
            ? referenceFrame.TransformVector(desiredVelocity)
            : desiredVelocity;
    }
}
