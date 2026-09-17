using UnityEngine;

public static class TargetAssistUtil
{
    public static GameObject GetClosestToCameraForwardLine(
        Transform cameraTransform,
        string targetTag,
        float perpendicularDistanceWeight = 1f,
        float forwardDistanceWeight = 0.1f)
    {
        if (cameraTransform == null || string.IsNullOrEmpty(targetTag))
        {
            return null;
        }

        GameObject[] candidates = GameObject.FindGameObjectsWithTag(targetTag);
        Vector3 origin = cameraTransform.position;
        Vector3 forward = cameraTransform.forward.normalized;

        GameObject bestCandidate = null;
        float bestScore = float.MaxValue;

        for (int i = 0; i < candidates.Length; i++)
        {
            GameObject candidate = candidates[i];
            if (candidate == null)
            {
                continue;
            }

            Vector3 toCandidate = candidate.transform.position - origin;
            float forwardDistance = Vector3.Dot(toCandidate, forward);
            if (forwardDistance < 0f)
            {
                continue;
            }

            Vector3 closestPointOnLine = origin + forward * forwardDistance;
            float perpendicularDistance = Vector3.Distance(candidate.transform.position, closestPointOnLine);
            float score = perpendicularDistance * perpendicularDistanceWeight
                + forwardDistance * forwardDistanceWeight;

            if (score < bestScore)
            {
                bestCandidate = candidate;
                bestScore = score;
            }
        }

        return bestCandidate;
    }

    public static GameObject GetClosestToCameraForwardLine<T>(
        Transform cameraTransform,
        string targetTag,
        float perpendicularDistanceWeight = 1f,
        float forwardDistanceWeight = 0.1f)
        where T : Component
    {
        if (cameraTransform == null || string.IsNullOrEmpty(targetTag))
        {
            return null;
        }

        GameObject[] candidates = GameObject.FindGameObjectsWithTag(targetTag);
        Vector3 origin = cameraTransform.position;
        Vector3 forward = cameraTransform.forward.normalized;

        GameObject bestCandidate = null;
        float bestScore = float.MaxValue;

        for (int i = 0; i < candidates.Length; i++)
        {
            GameObject candidate = candidates[i];
            if (candidate == null || !candidate.TryGetComponent(out T _))
            {
                continue;
            }

            Vector3 toCandidate = candidate.transform.position - origin;
            float forwardDistance = Vector3.Dot(toCandidate, forward);
            if (forwardDistance < 0f)
            {
                continue;
            }

            Vector3 closestPointOnLine = origin + forward * forwardDistance;
            float perpendicularDistance = Vector3.Distance(candidate.transform.position, closestPointOnLine);
            float score = perpendicularDistance * perpendicularDistanceWeight
                + forwardDistance * forwardDistanceWeight;

            if (score < bestScore)
            {
                bestCandidate = candidate;
                bestScore = score;
            }
        }

        return bestCandidate;
    }
}
