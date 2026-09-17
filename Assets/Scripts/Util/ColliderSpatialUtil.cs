using UnityEngine;

public static class ColliderSpatialUtil
{
    public static Vector3 GetColliderCenterPoint(Collider colliderToUse)
    {
        return colliderToUse.bounds.center;
    }

    /// <summary>
    /// Returns world-space half extents for the collider shape while ignoring
    /// object rotation. Scale is applied, but the result stays in the collider's
    /// own local-axis layout instead of using the rotated world AABB.
    /// </summary>
    public static Vector3 GetColliderHalfExtents(Collider colliderToUse)
    {
        if (colliderToUse is BoxCollider boxCollider)
        {
            return GetBoxColliderHalfExtents(boxCollider);
        }

        if (colliderToUse is SphereCollider sphereCollider)
        {
            return GetSphereColliderHalfExtents(sphereCollider);
        }

        if (colliderToUse is CapsuleCollider capsuleCollider)
        {
            return GetCapsuleColliderHalfExtents(capsuleCollider);
        }

        return GetDefaultColliderHalfExtents(colliderToUse);
    }

    /// <summary>
    /// BoxCollider already stores its unrotated local size, so world half extents
    /// are just local half size multiplied by absolute lossy scale on each axis.
    /// </summary>
    public static Vector3 GetBoxColliderHalfExtents(BoxCollider boxCollider)
    {
        return GetScaledHalfExtents(boxCollider.size * 0.5f, boxCollider.transform.lossyScale);
    }

    /// <summary>
    /// SphereCollider stays a sphere in physics queries, so use the largest
    /// absolute scale component as the world radius and mirror it on all axes.
    /// </summary>
    public static Vector3 GetSphereColliderHalfExtents(SphereCollider sphereCollider)
    {
        float radius = sphereCollider.radius * GetMaxAbsComponent(sphereCollider.transform.lossyScale);
        return new Vector3(radius, radius, radius);
    }

    /// <summary>
    /// CapsuleCollider needs special handling because its long axis depends on
    /// direction and its radius is scaled from the perpendicular axes. The result
    /// is returned in the collider's local-axis layout, before transform rotation.
    /// </summary>
    public static Vector3 GetCapsuleColliderHalfExtents(CapsuleCollider capsuleCollider)
    {
        Vector3 lossyScale = Abs(capsuleCollider.transform.lossyScale);

        float axisScale;
        float radiusScale;

        switch (capsuleCollider.direction)
        {
            case 0:
                axisScale = lossyScale.x;
                radiusScale = Mathf.Max(lossyScale.y, lossyScale.z);
                break;

            case 1:
                axisScale = lossyScale.y;
                radiusScale = Mathf.Max(lossyScale.x, lossyScale.z);
                break;

            case 2:
                axisScale = lossyScale.z;
                radiusScale = Mathf.Max(lossyScale.x, lossyScale.y);
                break;

            default:
                axisScale = lossyScale.y;
                radiusScale = Mathf.Max(lossyScale.x, lossyScale.z);
                break;
        }

        float radius = capsuleCollider.radius * radiusScale;
        float halfHeight = Mathf.Max(capsuleCollider.height * axisScale, radius * 2f) * 0.5f;

        switch (capsuleCollider.direction)
        {
            case 0:
                return new Vector3(halfHeight, radius, radius);

            case 1:
                return new Vector3(radius, halfHeight, radius);

            case 2:
                return new Vector3(radius, radius, halfHeight);

            default:
                return new Vector3(radius, halfHeight, radius);
        }
    }

    /// <summary>
    /// Unsupported colliders do not expose generic unrotated shape extents
    /// through the base Collider API. For MeshCollider, sharedMesh.bounds is the
    /// closest local-space source, scaled into world units. If no shared mesh is
    /// available, fall back to bounds.extents as the best generic approximation.
    /// </summary>
    public static Vector3 GetDefaultColliderHalfExtents(Collider colliderToUse)
    {
        if (colliderToUse is MeshCollider meshCollider && meshCollider.sharedMesh != null)
        {
            return GetScaledHalfExtents(meshCollider.sharedMesh.bounds.extents, meshCollider.transform.lossyScale);
        }

        return colliderToUse.bounds.extents;
    }

    /// <summary>
    /// Returns the collider surface point that sits furthest in the provided
    /// world-space forward direction when looking from the collider's center.
    /// The "middle" name comes from the common upright-capsule case where this
    /// lands on the midpoint of the front arc, but the same query works for any
    /// collider shape via ClosestPoint, so CapsuleCollider does not need a
    /// dedicated branch here.
    /// </summary>
    public static Vector3 GetForwardMiddlePoint(Collider colliderToUse, Vector3 forward)
    {
        var centerPoint = colliderToUse.bounds.center;
        if (forward.sqrMagnitude <= Mathf.Epsilon)
        {
            return centerPoint;
        }

        var normalizedForward = forward.normalized;
        // Sample from a point that is definitely outside the collider's world
        // bounds, then let ClosestPoint snap that sample back to the real
        // forward-facing surface of the collider.
        var sampleDistance = Mathf.Max(colliderToUse.bounds.size.magnitude, 0.01f) + 0.01f;
        var outsideSamplePoint = centerPoint + normalizedForward * sampleDistance;
        return colliderToUse.ClosestPoint(outsideSamplePoint);
    }

    /// <summary>
    /// Returns the total cast distance that starts at the collider center and
    /// extends past the collider's own forward-facing surface by the requested
    /// extra distance. Internally this measures how far the forward middle point
    /// sits from the center when projected onto the forward direction.
    /// </summary>
    public static float GetProjectedFromForwardMiddlePoint(
        Collider colliderToUse,
        Vector3 forward,
        float distance)
    {
        var centerPoint = GetColliderCenterPoint(colliderToUse);
        var forwardMiddlePoint = GetForwardMiddlePoint(colliderToUse, forward);
        var forwardOffset = Mathf.Max(0f, Vector3.Dot(forwardMiddlePoint - centerPoint, forward));
        return distance + forwardOffset;
    }

    public static bool IsInForwardHalfSpace(Vector3 centerPoint, Vector3 pointToCheck, Vector3 forward)
    {
        if (forward.sqrMagnitude <= Mathf.Epsilon)
        {
            return false;
        }

        return Vector3.Dot(forward.normalized, pointToCheck - centerPoint) >= 0f;
    }

    /// <summary>
    /// Returns a representative point on the candidate collider for interaction
    /// checks. Most collider types support ClosestPoint directly. Non-convex
    /// MeshCollider does not, so use the broader bounds closest point there.
    /// </summary>
    public static Vector3 GetInteractableCandidatePoint(Collider candidateCollider, Vector3 samplePoint)
    {
        if (candidateCollider is TerrainCollider)
        {
            Debug.LogWarning(
                $"{nameof(GetInteractableCandidatePoint)} should not be called with a {nameof(TerrainCollider)}.",
                candidateCollider);
            return candidateCollider.bounds.ClosestPoint(samplePoint);
        }

        if (candidateCollider is MeshCollider meshCollider && !meshCollider.convex)
        {
            return candidateCollider.bounds.ClosestPoint(samplePoint);
        }

        return candidateCollider.ClosestPoint(samplePoint);
    }

    /// <summary>
    /// Sphere-casts upward from the collider bounds center through the requested
    /// upper vertical slice. If that corridor still intersects a collider marked
    /// as water, the actor is still too submerged to jump out.
    /// </summary>
    public static bool CheckUpperHalfBoundsSphere(
        Collider sourceCollider,
        int waterLayer,
        Vector3 referenceUp,
        float upperPart = 0.5f)
    {
        if (sourceCollider == null || referenceUp.sqrMagnitude <= Mathf.Epsilon)
        {
            return false;
        }

        Bounds bounds = sourceCollider.bounds;
        Vector3 origin = bounds.center;
        float distance = Mathf.Max(0f, bounds.size.y * upperPart);
        float radius = Mathf.Max(0.01f, bounds.size.y * Mathf.Max(0f, 1f - upperPart - 0.01f));
        Vector3 up = referenceUp.normalized;
        RaycastHit[] hits = Physics.SphereCastAll(
            origin,
            radius,
            up,
            distance,
            Physics.AllLayers,
            QueryTriggerInteraction.Collide);

        return !ColliderQueryUtil.CheckRaycastHitsLayerOrTag(hits, sourceCollider, waterLayer, "Water");
    }

    public static Vector3 GetScaledHalfExtents(Vector3 halfExtents, Vector3 lossyScale)
    {
        Vector3 absoluteScale = Abs(lossyScale);
        return new Vector3(
            halfExtents.x * absoluteScale.x,
            halfExtents.y * absoluteScale.y,
            halfExtents.z * absoluteScale.z);
    }

    public static Vector3 Abs(Vector3 value)
    {
        return new Vector3(
            Mathf.Abs(value.x),
            Mathf.Abs(value.y),
            Mathf.Abs(value.z));
    }

    public static float GetMaxAbsComponent(Vector3 value)
    {
        return Mathf.Max(Mathf.Abs(value.x), Mathf.Abs(value.y), Mathf.Abs(value.z));
    }
}
