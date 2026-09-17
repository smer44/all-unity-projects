using UnityEngine;

public static class ColliderQueryUtil
{
    /// <summary>
    /// Tries to read a component of type T from a single RaycastHit while
    /// ignoring hits against the supplied collider.
    /// </summary>
    public static bool TryGetComponentFromHit<T>(
        RaycastHit hit,
        Collider ignoredCollider,
        out T component)
        where T : MonoBehaviour
    {
        component = null;
        if (hit.collider == null || hit.collider == ignoredCollider)
        {
            return false;
        }

        return hit.collider.TryGetComponent(out component);
    }

    /// <summary>
    /// Scans a hit array, picks the closest hit that is not the ignored collider,
    /// and then tries to fetch a component of type T from that collider.
    /// </summary>
    public static bool TryGetDetectedComponentFromHits<T>(
        RaycastHit[] hits,
        Collider ignoredCollider,
        out T component)
        where T : MonoBehaviour
    {
        component = null;
        RaycastHit? closestHit = null;

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].collider == null || hits[i].collider == ignoredCollider)
            {
                continue;
            }

            if (closestHit == null || hits[i].distance < closestHit.Value.distance)
            {
                closestHit = hits[i];
            }
        }

        if (!closestHit.HasValue)
        {
            return false;
        }

        return TryGetComponentFromHit(closestHit.Value, ignoredCollider, out component);
    }

    /// <summary>
    /// Returns true when the collider matches either the given layer or tag.
    /// </summary>
    public static bool CheckColliderLayerOrTag(Collider other, int layer, string tag)
    {
        return other != null
            && (other.gameObject.layer == layer || other.gameObject.CompareTag(tag));
    }

    /// <summary>
    /// Returns true if any hit in the array, excluding the ignored collider,
    /// belongs to an object that matches the given layer or tag.
    /// </summary>
    public static bool CheckRaycastHitsLayerOrTag(
        RaycastHit[] hits,
        Collider ignoredCollider,
        int layer,
        string tag)
    {
        for (int i = 0; i < hits.Length; i++)
        {
            Collider hitCollider = hits[i].collider;
            if (hitCollider == null || hitCollider == ignoredCollider)
            {
                continue;
            }

            if (CheckColliderLayerOrTag(hitCollider, layer, tag))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Tests whether a candidate collider is a valid component match inside the
    /// forward half of a sphere query. If valid, returns the component and the
    /// squared distance from the half-sphere origin to the sampled candidate point.
    /// </summary>
    public static bool TryGetHalfSphereCandidateComponent<T>(
        Collider candidateCollider,
        Collider ignoredCollider,
        Vector3 origin,
        Vector3 forward,
        float radius,
        out T component,
        out float sqrDistance)
        where T : MonoBehaviour
    {
        component = null;
        sqrDistance = float.MaxValue;

        if (candidateCollider == null || candidateCollider == ignoredCollider)
        {
            return false;
        }

        if (!candidateCollider.TryGetComponent(out component))
        {
            return false;
        }


        var samplePoint = origin + forward * (radius * 0.5f);
        var candidatePoint = ColliderSpatialUtil.GetInteractableCandidatePoint(candidateCollider, samplePoint);
        if ((candidatePoint - origin).sqrMagnitude <= Mathf.Epsilon)
        {
            candidatePoint = ColliderSpatialUtil.GetColliderCenterPoint(candidateCollider);
        }

        var toCandidate = candidatePoint - origin;
        if (toCandidate.sqrMagnitude <= Mathf.Epsilon)
        {
            return false;
        }

        if (!ColliderSpatialUtil.IsInForwardHalfSpace(origin, candidatePoint, forward))
        {
            return false;
        }


        sqrDistance = toCandidate.sqrMagnitude;
        return true;
    }
}
