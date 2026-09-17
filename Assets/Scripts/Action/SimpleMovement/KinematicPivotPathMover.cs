using UnityEngine;

public class KinematicPivotPathMover : KinematicTimelineMover
{
    [SerializeField] private Transform[] pivotPoints;
    [SerializeField] private float maxSpeed = 1f;
    [SerializeField] private float slowDownDistance = 1f;
    [SerializeField] private float arrivalDistance = 0.05f;
    [SerializeField] private bool loop = true;

    private int currentPivotIndex = -1;

    protected override void OnMoverAwake()
    {
        currentPivotIndex = GetFirstValidPivotIndex();
    }

    protected override void TickMovement()
    {
        TryAdvancePivotIfReached();
        UpdateSpeed();
        ApplySpeed();
        TryAdvancePivotIfReached();
    }

    protected override void UpdateSpeed()
    {
        if (!TryGetCurrentPivotPosition(out Vector3 targetPosition))
        {
            currentSpeed = Vector3.zero;
            return;
        }

        Vector3 toTarget = targetPosition - rb.position;
        float distanceToTarget = toTarget.magnitude;
        if (distanceToTarget <= arrivalDistance)
        {
            currentSpeed = Vector3.zero;
            return;
        }

        Vector3 normalizedDirection = toTarget / distanceToTarget;
        float targetSpeedMagnitude = maxSpeed;
        float effectiveSlowDownDistance = Mathf.Max(arrivalDistance + 0.0001f, slowDownDistance);

        if (distanceToTarget <= effectiveSlowDownDistance)
        {
            float distanceFactor = Mathf.InverseLerp(arrivalDistance, effectiveSlowDownDistance, distanceToTarget);
            targetSpeedMagnitude = Mathf.Lerp(0f, maxSpeed, distanceFactor);
        }

        UpdateSpeed(normalizedDirection, targetSpeedMagnitude);

        float maxReachableSpeedMagnitude = distanceToTarget / Time.fixedDeltaTime;
        if (currentSpeed.magnitude > maxReachableSpeedMagnitude)
        {
            currentSpeed = normalizedDirection * maxReachableSpeedMagnitude;
        }
    }

    private void TryAdvancePivotIfReached()
    {
        if (!TryGetCurrentPivotPosition(out Vector3 targetPosition))
        {
            return;
        }

        if ((targetPosition - rb.position).sqrMagnitude > arrivalDistance * arrivalDistance)
        {
            return;
        }

        //rb.position = targetPosition;
        rb.MovePosition(targetPosition);
        currentSpeed = Vector3.zero;

        int nextPivotIndex = GetNextValidPivotIndex(currentPivotIndex);
        currentPivotIndex = nextPivotIndex;
    }

    private bool TryGetCurrentPivotPosition(out Vector3 pivotPosition)
    {
        pivotPosition = Vector3.zero;

        if (currentPivotIndex < 0
            || pivotPoints == null
            || currentPivotIndex >= pivotPoints.Length
            || pivotPoints[currentPivotIndex] == null)
        {
            return false;
        }

        pivotPosition = pivotPoints[currentPivotIndex].position;
        return true;
    }

    private int GetFirstValidPivotIndex()
    {
        return GetNextValidPivotIndex(-1);
    }

    private int GetNextValidPivotIndex(int fromIndex)
    {
        if (pivotPoints == null || pivotPoints.Length == 0)
        {
            return -1;
        }

        int maxChecks = pivotPoints.Length;
        for (int offset = 1; offset <= maxChecks; offset++)
        {
            int candidateIndex = fromIndex + offset;

            if (loop)
            {
                candidateIndex = ((candidateIndex % pivotPoints.Length) + pivotPoints.Length) % pivotPoints.Length;
            }
            else if (candidateIndex >= pivotPoints.Length)
            {
                return -1;
            }

            if (pivotPoints[candidateIndex] != null)
            {
                return candidateIndex;
            }
        }

        return -1;
    }
}
