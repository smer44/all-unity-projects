using UnityEngine;

public static class AnimationChanger
{
    public static void AddVerticalRotationAngle(Transform bone, float angleDegrees)
    {
        if (bone == null)
        {
            return;
        }

        Vector3 worldUp = Vector3.down;
        Vector3 boneDirection = GetBoneWorldDirection(bone);
        if (boneDirection.sqrMagnitude <= Mathf.Epsilon)
        {
            return;
        }

        Vector3 planarDirection = Vector3.ProjectOnPlane(boneDirection, worldUp);
        if (planarDirection.sqrMagnitude <= Mathf.Epsilon)
        {
            return;
        }

        Vector3 pitchAxis = Vector3.Cross(worldUp, planarDirection).normalized;
        Vector3 targetDirection = Quaternion.AngleAxis(angleDegrees, pitchAxis) * boneDirection;
        Quaternion swingRotation = Quaternion.FromToRotation(boneDirection, targetDirection);

        bone.rotation = swingRotation * bone.rotation;
    }

    private static Vector3 GetBoneWorldDirection(Transform bone)
    {
        if (bone.childCount > 0)
        {
            Vector3 childDirection = bone.GetChild(0).position - bone.position;
            if (childDirection.sqrMagnitude > Mathf.Epsilon)
            {
                return childDirection.normalized;
            }
        }

        return bone.forward;
    }
}
