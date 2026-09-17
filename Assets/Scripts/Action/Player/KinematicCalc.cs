using System;
using UnityEngine;

  /// <summary>
  /// Utils about calculating kinematic player controlls
 /// </summary>
public static class KinematicCalc
{
    private static readonly Collider[] DefaultOverlapBuffer = new Collider[16];
    public static readonly Collider[] LastOverlaps = new Collider[DefaultOverlapBuffer.Length];
    public static int LastOverlapCount { get; private set; }

    public static Vector3 MoveAndSlideSingle(
        Rigidbody body,
        Vector3 displacement,
        float skin)
    {
        if (body == null || displacement == Vector3.zero)
            return displacement;

        var direction = displacement.normalized;
        var distance = displacement.magnitude;

        // QueryTriggerInteraction.Ignore makes the sweep ignore trigger colliders.
        if (body.SweepTest(direction, out RaycastHit hit, distance + skin, QueryTriggerInteraction.Ignore))
        {
            float allowed = Mathf.Max(0f, hit.distance - skin);

            var moveToHit = direction * allowed;
            var remaining = displacement - moveToHit;
            var slide = Vector3.ProjectOnPlane(remaining, hit.normal);
            return moveToHit + slide;
        }

        return displacement;
    }

    public static void SnapToGroundHard(
        Transform targetTransform,
        Rigidbody body,
        Collider playerCollider,
        RaycastHit bestGround,
        Transform groundParent,
        Vector3 referenceUp)
    {
        if (targetTransform == null || playerCollider == null || bestGround.collider == null)
            return;

        var bounds = playerCollider.bounds;
        var currentBottom = bounds.center - referenceUp * bounds.extents.y;
        var snapWorldDisplacement = bestGround.point - currentBottom;
        if (snapWorldDisplacement.sqrMagnitude <= Mathf.Epsilon)
            return;

        if (groundParent != null)
        {
            var localSnapDisplacement = groundParent.InverseTransformVector(snapWorldDisplacement);
            var snappedLocalPosition = targetTransform.localPosition;
            snappedLocalPosition.y += localSnapDisplacement.y;
            targetTransform.localPosition = snappedLocalPosition;
        }
        else
        {
            var snappedWorldPosition = targetTransform.position;
            snappedWorldPosition.y += snapWorldDisplacement.y;
            targetTransform.position = snappedWorldPosition;
        }

        if (body != null)
        {
            //body.position = targetTransform.position;
            body.MovePosition(targetTransform.position);
        }
    }

    /// <summary>
    /// Adds a ground snap displacement into the caller's local motion accumulator
    /// instead of teleporting the transform. The resulting motion can then be
    /// resolved by the caller's sweep / slide step.
    /// </summary>
    public static bool SnapToGround2(
        ref Vector3 localVelocity,
        Collider playerCollider,
        RaycastHit bestGround,
        Transform referenceFrame,
        Vector3 referenceUp,
        float maxSnapDistance = -1f,
        float minSnapDistance = 0.0001f)
    {
        if (playerCollider == null || bestGround.collider == null)
            return false;

        if (referenceUp.sqrMagnitude <= Mathf.Epsilon)
            return false;

        Vector3 up = referenceUp.normalized;
        Bounds bounds = playerCollider.bounds;
        Vector3 currentBottom = bounds.center - up * bounds.extents.y;

        // Snap only along the grounding axis. This prevents sideways pull
        // toward ledges while still letting the later sweep handle collisions.
        float snapDistance = Vector3.Dot(bestGround.point - currentBottom, up);
        if (Mathf.Abs(snapDistance) <= minSnapDistance)
            return false;

        //if (maxSnapDistance > 0f)
        //    snapDistance = Mathf.Clamp(snapDistance, -maxSnapDistance, maxSnapDistance);

        Vector3 snapWorldDisplacement = up * snapDistance;
        //Vector3 snapLocalDisplacement = referenceFrame != null
        //    ? referenceFrame.InverseTransformVector(snapWorldDisplacement)
        //    : snapWorldDisplacement;

        localVelocity += snapWorldDisplacement;
        return true;
    }

    /// <summary>
    /// Applies the full displacement directly to the Rigidbody without temporal smoothing.
    /// Records only the collision-resolved motion, after any velocity integration.
    /// </summary>
    public static void SimpleDisplacementApply(
        Rigidbody body,
        Vector3 totalDisplacement,
        ref Vector3 previousAppliedDisplacement)
    {
        previousAppliedDisplacement = totalDisplacement;

        if (body == null || totalDisplacement.sqrMagnitude <= Mathf.Epsilon)
        {
            return;
        }

        body.MovePosition(body.position + totalDisplacement);
    }

    /// <summary>
    /// Integrates world acceleration into velocity before collision resolution.
    /// The magnitude cap applies to the resulting vector, including diagonal input.
    /// </summary>
    public static Vector3 IntegrateVelocity(
        Vector3 velocity,
        Vector3 acceleration,
        float deltaTime,
        float maxSpeed)
    {
        return Vector3.ClampMagnitude(velocity + acceleration * deltaTime, maxSpeed);
    }

    /// <summary>
    /// Raycasts downward from the collider's displaced center and returns the closest hit
    /// that qualifies as ground. The caller supplies parentWorldDisplacement so the probe
    /// matches the support motion already considered by the controller, and referenceUp so
    /// the same method can work with arbitrary "up" directions instead of assuming Vector3.up.
    /// </summary>
    public static bool TryGetBestGroundHit(
        Collider sourceCollider,
        Vector3 parentWorldDisplacement,
        Vector3 referenceUp,
        float probeDistance,
        out RaycastHit bestGroundHit,
        float maxGroundAngle = 45f,
        int layerMask = Physics.AllLayers,
        QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.Ignore)
    {
        bestGroundHit = default;

        if (sourceCollider == null
            || referenceUp.sqrMagnitude <= Mathf.Epsilon
            || probeDistance <= 0f)
        {
            return false;
        }

        Bounds bounds = sourceCollider.bounds;
        Vector3 up = referenceUp.normalized;
        // Start the ray from the collider center after applying support displacement so
        // grounding is evaluated where the mover is expected to be this step, not where
        // it was at the beginning of the frame.
        Vector3 origin = bounds.center + parentWorldDisplacement;
        RaycastHit[] hits = Physics.RaycastAll(
            origin,
            -up,
            probeDistance,
            layerMask,
            queryTriggerInteraction);

        bool foundGround = false;
        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            if (hit.collider == null || IsSelfCollider(sourceCollider, hit.collider))
            {
                continue;
            }

            // Reject steep surfaces so walls and near-vertical geometry are not treated as floor.
            if (Vector3.Angle(hit.normal, up) > maxGroundAngle)
            {
                continue;
            }

            if (!foundGround || hit.distance < bestGroundHit.distance)
            {
                bestGroundHit = hit;
                foundGround = true;
            }
        }

        return foundGround;
    }

 /// <summary>
    /// Returns true if the given CapsuleCollider overlaps at least one other
    /// non-trigger collider.
    /// Fixed-size overlap buffer can produce a false negative 
    /// If more than 16 colliders overlap.
    /// Is checking the capsule at its current transform pose. and does not need capsule translation.
    /// </summary>
    public static bool HasOverlaps(CapsuleCollider capsule, int layerMask = Physics.AllLayers)
    {
        ClearLastOverlaps();

        if (capsule == null || !capsule.enabled)
            return false;

        return CollectOverlaps(capsule, Vector3.zero, layerMask) > 0;
    }

    /// <summary>
    /// Computes only the extra depenetration needed for a capsule after applying
    /// an optional probe offset. The caller's intended motion should be handled
    /// separately so escape motion is preserved.
    /// </summary>
    public static bool PushOutOfOverlaps(
        ref Vector3 depenetrationDisplacement,
        CapsuleCollider capsule,
        Vector3 probeOffset,
        int maxIterations = 3,
        int layerMask = Physics.AllLayers,
        float separationPadding = 0.001f,
        float minPenetration = 0.0001f)
    {
        ClearLastOverlaps();

        if (capsule == null || !capsule.enabled)
            return false;

        maxIterations = Mathf.Max(1, maxIterations);
        separationPadding = Mathf.Max(0f, separationPadding);
        minPenetration = Mathf.Max(0f, minPenetration);

        bool pushed = false;
        Quaternion capsuleRotation = capsule.transform.rotation;

        for (int iteration = 0; iteration < maxIterations; iteration++)
        {
            Vector3 currentProbeOffset = probeOffset + depenetrationDisplacement;
            int overlapCount = CollectOverlaps(capsule, currentProbeOffset, layerMask);
            if (overlapCount == 0)
                return pushed;

            bool pushedThisIteration = false;
            for (int i = 0; i < overlapCount; i++)
            {
                Collider overlap = LastOverlaps[i];
                if (overlap == null)
                    continue;

                Vector3 capsulePosition = capsule.transform.position + currentProbeOffset;
                if (!Physics.ComputePenetration(
                    capsule,
                    capsulePosition,
                    capsuleRotation,
                    overlap,
                    overlap.transform.position,
                    overlap.transform.rotation,
                    out Vector3 direction,
                    out float distance))
                {
                    continue;
                }

                if (distance <= minPenetration || direction.sqrMagnitude <= Mathf.Epsilon)
                    continue;

                depenetrationDisplacement += direction.normalized * (distance + separationPadding);
                pushed = true;
                pushedThisIteration = true;
            }

            if (!pushedThisIteration)
                break;
        }

        CollectOverlaps(capsule, probeOffset + depenetrationDisplacement, layerMask);
        return pushed;
    }

    private static void ClearLastOverlaps()
    {
        if (LastOverlapCount > 0)
        {
            Array.Clear(LastOverlaps, 0, LastOverlapCount);
        }
        else
        {
            Array.Clear(LastOverlaps, 0, LastOverlaps.Length);
        }

        LastOverlapCount = 0;
    }

    private static int CollectOverlaps(
        CapsuleCollider capsule,
        Vector3 worldTranslation,
        int layerMask)
    {
        ClearLastOverlaps();

        GetWorldCapsule(capsule, out Vector3 point0, out Vector3 point1, out float radius);
        point0 += worldTranslation;
        point1 += worldTranslation;

        int hitCount = Physics.OverlapCapsuleNonAlloc(
            point0,
            point1,
            radius,
            DefaultOverlapBuffer,
            layerMask,
            QueryTriggerInteraction.Ignore);

        int overlapCount = 0;
        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = DefaultOverlapBuffer[i];
            if (hit == null || IsSelfCollider(capsule, hit))
                continue;

            if (overlapCount >= LastOverlaps.Length)
                break;

            LastOverlaps[overlapCount] = hit;
            overlapCount++;
        }

        LastOverlapCount = overlapCount;
        return overlapCount;
    }

    private static bool IsSelfCollider(Collider source, Collider candidate)
    {
        if (source == null || candidate == null)
            return false;

        if (source == candidate)
            return true;

        if (source.attachedRigidbody != null
            && candidate.attachedRigidbody != null
            && source.attachedRigidbody == candidate.attachedRigidbody)
        {
            return true;
        }

        return source.transform.root == candidate.transform.root;
    }



    // Convert a CapsuleCollider from its local Unity representation
    // (center + height + radius + direction on the Transform)
    // into the world-space representation required by Physics.OverlapCapsule:
    //
    // - point0: center of one end sphere
    // - point1: center of the other end sphere
    // - radius: world-space capsule radius
    //
    // Why this is needed:
    // CapsuleCollider stores center / height / radius in local space,
    // but Physics.OverlapCapsule expects explicit world-space endpoints and radius.
    public static void GetWorldCapsule(
        CapsuleCollider capsule,
        out Vector3 point0,
        out Vector3 point1,
        out float radius)
    {
        // Cache the Transform because we need its position, rotation, and scale.
        Transform t = capsule.transform;

        // Convert the capsule center from local space into world space.
        Vector3 center = t.TransformPoint(capsule.center);

        // Use absolute lossy scale so negative scale does not invert sizes.
        Vector3 lossy = ColliderSpatialUtil.Abs(t.lossyScale);

        // axis      = world-space direction of the capsule's length
        // axisScale = scale applied along that length axis
        // radiusScale = scale applied to the capsule thickness
        Vector3 axis;
        float axisScale;
        float radiusScale;

        // CapsuleCollider.direction:
        // 0 = X axis, 1 = Y axis, 2 = Z axis.
        // Pick the matching world-space axis from the Transform
        // and derive the correct scale factors.
        switch (capsule.direction)
        {
            case 0: // Capsule length is along local X
                axis = t.right;
                axisScale = lossy.x;
                // Radius is perpendicular to X, so use the largest perpendicular scale.
                radiusScale = Mathf.Max(lossy.y, lossy.z);
                break;

            case 1: // Capsule length is along local Y
                axis = t.up;
                axisScale = lossy.y;
                // Radius is perpendicular to Y.
                radiusScale = Mathf.Max(lossy.x, lossy.z);
                break;

            case 2: // Capsule length is along local Z
                axis = t.forward;
                axisScale = lossy.z;
                // Radius is perpendicular to Z.
                radiusScale = Mathf.Max(lossy.x, lossy.y);
                break;

            default:
                // Fallback to Y so we still return a valid capsule.
                axis = t.up;
                axisScale = lossy.y;
                radiusScale = Mathf.Max(lossy.x, lossy.z);
                break;
        }

        // Convert the local collider radius into world-space radius.
        radius = capsule.radius * radiusScale;

        // Convert total local height into world-space height along the capsule axis.
        // Clamp so height is never smaller than the capsule diameter.
        float height = Mathf.Max(capsule.height * axisScale, radius * 2f);

        // The cylindrical middle section extends by:
        // halfHeight - radius
        // from the center in both directions.
        // If the capsule is effectively just a sphere, this becomes 0.
        float halfStraight = Mathf.Max(0f, height * 0.5f - radius);

        // Compute the world-space centers of the two end spheres.
        point0 = center + axis * halfStraight;
        point1 = center - axis * halfStraight;
    }

    /// <summary>
    /// Casts a CapsuleCollider into the provided hit buffer without allocating.
    /// This is the capsule-only equivalent of AnyCastNonAlloc and is intended for
    /// call sites that know their mover shape in advance, such as the player.
    /// The cast uses a caller-provided probe pose so solvers can test hypothetical
    /// positions without mutating the live scene objects.
    /// </summary>
    public static int CapsuleCastNonAlloc(
        CapsuleCollider sourceCollider,
        Vector3 currentRootPosition,
        Vector3 castRootPosition,
        Vector3 direction,
        RaycastHit[] hitBuffer,
        float distance,
        int layerMask = Physics.AllLayers,
        QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.Ignore)
    {
        if (sourceCollider == null || hitBuffer == null || hitBuffer.Length == 0 || distance <= 0f)
        {
            return 0;
        }

        // Convert the capsule from its live pose into world-space endpoints, then offset
        // those endpoints into the probe pose requested by the caller.
        Vector3 rootDelta = castRootPosition - currentRootPosition;
        GetWorldCapsule(sourceCollider, out Vector3 point0, out Vector3 point1, out float radius);
        point0 += rootDelta;
        point1 += rootDelta;

        return Physics.CapsuleCastNonAlloc(
            point0,
            point1,
            radius,
            direction,
            hitBuffer,
            distance,
            layerMask,
            queryTriggerInteraction);
    }

    /// <summary>
    /// Resolves a displacement by repeatedly sweeping a capsule and projecting the
    /// remaining motion along contact planes. This is a small iterative kinematic
    /// solver: sweep, move up to the hit minus skin, then slide the remainder.
    /// The optional startPositionOffset lets callers begin the sweep from a probe
    /// pose, such as after applying moving-platform support displacement first.
    /// </summary>
    public static Vector3 MoveAndSlideIterations(
        CapsuleCollider sourceCollider,
        Vector3 currentRootPosition,
        Vector3 displacement,
        int iterations,
        RaycastHit[] hitBuffer,
        float skin,
        Vector3 startPositionOffset = default)
    {
        if (displacement.sqrMagnitude <= Mathf.Epsilon
            || sourceCollider == null
            || hitBuffer == null
            || hitBuffer.Length == 0)
        {
            return displacement;
        }

        // Always allow at least one sweep so callers do not silently bypass collision.
        iterations = Mathf.Max(1, iterations);

        Vector3 startPosition = currentRootPosition + startPositionOffset;
        Vector3 resolvedDisplacement = Vector3.zero;
        Vector3 remainingDisplacement = displacement;
        Vector3 firstContactNormal = Vector3.zero;
        Vector3 secondContactNormal = Vector3.zero;
        int contactCount = 0;

        for (int i = 0; i < iterations; i++)
        {
            float remainingDistance = remainingDisplacement.magnitude;
            if (remainingDistance <= 0.0001f)
            {
                break;
            }

            Vector3 direction = remainingDisplacement / remainingDistance;
            Vector3 castOrigin = startPosition + resolvedDisplacement;
            int hitCount = CapsuleCastNonAlloc(
                sourceCollider,
                currentRootPosition,
                castOrigin,
                direction,
                hitBuffer,
                remainingDistance + skin);

            if (!TryGetClosestCastHit(sourceCollider, hitBuffer, hitCount, out RaycastHit hit))
            {
                resolvedDisplacement += remainingDisplacement;
                break;
            }

            // Stop short of the obstacle by the requested skin width so later sweeps
            // still have room to detect the surface instead of starting in contact.
            float moveDistance = Mathf.Max(0f, hit.distance - skin);
            if (moveDistance > 0f)
            {
                resolvedDisplacement += direction * moveDistance;
            }

            Vector3 remainder = remainingDisplacement - (direction * moveDistance);
            if (remainder.sqrMagnitude <= 0.0001f)
            {
                break;
            }

            Vector3 hitNormal = hit.normal.normalized;
            bool isNewPlane = true;
            for (int normalIndex = 0; normalIndex < contactCount; normalIndex++)
            {
                Vector3 existingNormal = normalIndex == 0 ? firstContactNormal : secondContactNormal;
                if (Vector3.Dot(existingNormal, hitNormal) > 0.999f)
                {
                    isNewPlane = false;
                    break;
                }
            }

            if (isNewPlane)
            {
                if (contactCount == 0)
                {
                    firstContactNormal = hitNormal;
                    contactCount++;
                }
                else if (contactCount == 1)
                {
                    secondContactNormal = hitNormal;
                    contactCount++;
                }
                else
                {
                    // Three distinct blocking planes leave no stable slide direction,
                    // so stop here rather than inventing extra motion.
                    break;
                }
            }

            if (contactCount == 1)
            {
                // First contact: remove the component pushing into the surface and keep
                // only the tangential part of the remaining motion.
                remainingDisplacement = Vector3.ProjectOnPlane(remainder, firstContactNormal);
            }
            else
            {
                Vector3 crease = Vector3.Cross(firstContactNormal, secondContactNormal);
                if (crease.sqrMagnitude <= 0.0001f)
                {
                    remainingDisplacement = Vector3.ProjectOnPlane(remainder, secondContactNormal);
                }
                else
                {
                    // Two distinct planes constrain the motion to their intersection line.
                    remainingDisplacement = Vector3.Project(remainder, crease.normalized);
                }
            }
        }

        return resolvedDisplacement;
    }

    /// <summary>
    /// Casts any supported collider shape into the provided hit buffer without allocating buffer ( writes on existing buffer)
    /// Hint about Non Alloc methods : 
    /// Unlike CapsuleCast or CapsuleCastAll, this method writes results into an existing array,
    /// which minimizes memory garbage collection, making it ideal for frequent calls
    /// (e.g., in Update or FixedUpdate).
    /// The cast is performed from a caller-provided world pose so higher-level solvers can
    /// probe hypothetical positions without moving the actual Rigidbody or Transform.
    /// Supports CapsuleCollider, BoxCollider, SphereCollider, and falls back to
    /// Rigidbody.SweepTestAll for unsupported collider types when a Rigidbody is available.
    /// </summary>
    public static int AnyCastNonAlloc(
        Collider sourceCollider,
        Rigidbody sourceBody,
        Vector3 currentRootPosition,
        Vector3 castRootPosition,
        Vector3 direction,
        RaycastHit[] hitBuffer,
        float distance,
        int layerMask = Physics.AllLayers,
        QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.Ignore)
    {
        if (sourceCollider == null || hitBuffer == null || hitBuffer.Length == 0 || distance <= 0f)
        {
            return 0;
        }

        // The collider currently lives at currentRootPosition. When a solver wants to cast
        // from a hypothetical pose, it passes castRootPosition and we offset the derived
        // world-space shape by the difference instead of mutating scene objects.
        Vector3 rootDelta = castRootPosition - currentRootPosition;

        if (sourceCollider is CapsuleCollider capsule)
        {
            GetWorldCapsule(capsule, out Vector3 point0, out Vector3 point1, out float radius);
            point0 += rootDelta;
            point1 += rootDelta;

            return Physics.CapsuleCastNonAlloc(
                point0,
                point1,
                radius,
                direction,
                hitBuffer,
                distance,
                layerMask,
                queryTriggerInteraction);
        }

        if (sourceCollider is BoxCollider box)
        {
            // BoxCast expects half extents in world scale and a center in the cast pose.
            Vector3 halfExtents = ColliderSpatialUtil.GetScaledHalfExtents(box.size * 0.5f, box.transform.lossyScale);
            Vector3 center = box.bounds.center + rootDelta;

            return Physics.BoxCastNonAlloc(
                center,
                halfExtents,
                direction,
                hitBuffer,
                box.transform.rotation,
                distance,
                layerMask,
                queryTriggerInteraction);
        }

        if (sourceCollider is SphereCollider sphere)
        {
            Vector3 center = sphere.bounds.center + rootDelta;
            // SphereCast needs a single world-space radius, so use the largest absolute
            // scale component to stay conservative under non-uniform scaling.
            float radius = sphere.radius * ColliderSpatialUtil.GetMaxAbsComponent(sphere.transform.lossyScale);

            return Physics.SphereCastNonAlloc(
                center,
                radius,
                direction,
                hitBuffer,
                distance,
                layerMask,
                queryTriggerInteraction);
        }

        if (sourceBody != null)
        {
            // Unity does not expose a generic arbitrary-pose sweep for unknown collider
            // types, so this fallback uses the Rigidbody's live pose instead of the
            // caller-provided castRootPosition. It keeps unsupported collider types
            // functional, but the explicit shape branches above are more precise.
            RaycastHit[] hits = sourceBody.SweepTestAll(direction, distance, queryTriggerInteraction);
            int count = Mathf.Min(hits.Length, hitBuffer.Length);
            for (int i = 0; i < count; i++)
            {
                hitBuffer[i] = hits[i];
            }

            return count;
        }

        return 0;
    }

    private static bool TryGetClosestCastHit(
        Collider sourceCollider,
        RaycastHit[] hitBuffer,
        int hitCount,
        out RaycastHit closestHit)
    {
        closestHit = default;
        bool foundHit = false;
        int clampedHitCount = Mathf.Min(hitCount, hitBuffer.Length);

        for (int i = 0; i < clampedHitCount; i++)
        {
            RaycastHit hit = hitBuffer[i];
            // Ignore hits against the mover itself so child colliders or colliders
            // sharing the same Rigidbody do not immediately block the sweep.
            if (hit.collider == null || IsSelfCollider(sourceCollider, hit.collider))
            {
                continue;
            }

            if (!foundHit || hit.distance < closestHit.distance)
            {
                closestHit = hit;
                foundHit = true;
            }
        }

        return foundHit;
    }

}
