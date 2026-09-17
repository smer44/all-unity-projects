using UnityEngine;

public static class GizmoDisplayer
{
    public static void DebugHalfCircleGismo(
        Collider colliderToUse,
        Vector3 forward,
        Vector3 referenceUp,
        float radius = 1f,
        int rimSegments = 24,
        int arcSegments = 12,
        int arcPlaneCount = 4)
    {
        if (colliderToUse == null)
        {
            return;
        }

        GetHalfSphereGeometry(
            colliderToUse,
            forward,
            referenceUp,
            radius,
            rimSegments,
            arcSegments,
            arcPlaneCount,
            out Vector3[] startCorners,
            out Vector3[] endCorners);

        for (int i = 0; i < startCorners.Length; i++)
        {
            Debug.DrawLine(startCorners[i], endCorners[i], Color.yellow);
        }
    }

    private static void GetHalfSphereGeometry(
        Collider colliderToUse,
        Vector3 forward,
        Vector3 referenceUp,
        float radius,
        int rimSegments,
        int arcSegments,
        int arcPlaneCount,
        out Vector3[] startCorners,
        out Vector3[] endCorners)
    {
        Vector3 up = referenceUp.sqrMagnitude <= Mathf.Epsilon ? Vector3.up : referenceUp.normalized;
        Vector3 flattenedForward = FacingCalc.GetReferenceForwardByUp(up, forward);

        Vector3 right = Vector3.Cross(up, flattenedForward).normalized;
        up = Vector3.Cross(flattenedForward, right).normalized;

        Vector3 center = ColliderSpatialUtil.GetForwardMiddlePoint(colliderToUse, flattenedForward);
        float safeRadius = Mathf.Max(0.01f, radius);
        int safeRimSegments = Mathf.Max(8, rimSegments);
        int safeArcSegments = Mathf.Max(4, arcSegments);
        int safeArcPlaneCount = Mathf.Max(2, arcPlaneCount);
        int lineCount = safeRimSegments + (safeArcSegments * safeArcPlaneCount);

        startCorners = new Vector3[lineCount];
        endCorners = new Vector3[lineCount];

        int lineIndex = 0;
        Vector3[] rimPoints = new Vector3[safeRimSegments];
        for (int i = 0; i < safeRimSegments; i++)
        {
            float angle = (Mathf.PI * 2f * i) / safeRimSegments;
            Vector3 rimDirection = (right * Mathf.Cos(angle)) + (up * Mathf.Sin(angle));
            rimPoints[i] = center + rimDirection * safeRadius;
        }

        // Draw the rim circle that splits the full sphere into front and back halves.
        for (int i = 0; i < safeRimSegments; i++)
        {
            startCorners[lineIndex] = rimPoints[i];
            endCorners[lineIndex] = rimPoints[(i + 1) % safeRimSegments];
            lineIndex++;
        }

        // Draw several forward-facing semicircular arcs from rim to rim to make
        // the hemisphere volume readable from different camera angles.
        for (int planeIndex = 0; planeIndex < safeArcPlaneCount; planeIndex++)
        {
            float planeAngle = (Mathf.PI * planeIndex) / safeArcPlaneCount;
            Vector3 lateral = ((right * Mathf.Cos(planeAngle)) + (up * Mathf.Sin(planeAngle))).normalized;
            Vector3 previousPoint = center + lateral * safeRadius;

            for (int segmentIndex = 1; segmentIndex <= safeArcSegments; segmentIndex++)
            {
                float arcAngle = (Mathf.PI * segmentIndex) / safeArcSegments;
                Vector3 currentPoint =
                    center
                    + (lateral * Mathf.Cos(arcAngle) * safeRadius)
                    + (flattenedForward * Mathf.Sin(arcAngle) * safeRadius);

                startCorners[lineIndex] = previousPoint;
                endCorners[lineIndex] = currentPoint;
                previousPoint = currentPoint;
                lineIndex++;
            }
        }
    }
}
