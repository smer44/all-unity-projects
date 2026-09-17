using UnityEngine;

public class KinematicTimelineRotator : MonoBehaviour
{
    public Rigidbody rb;
    public Vector3 rotationSpeed = new Vector3(0f, 90f, 0f);
    [SerializeField] protected float accelerationLerp = 0.1f;
    [SerializeField] private Space rotationSpace = Space.Self;

    protected Vector3 currentRotationSpeed;

    private void Awake()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }

        OnRotatorAwake();
    }

    private void FixedUpdate()
    {
        if (rb == null)
        {
            return;
        }

        TickRotation();
    }

    protected virtual void OnRotatorAwake()
    {
    }

    protected virtual void TickRotation()
    {
        UpdateRotationSpeed();
        ApplyRotation();
    }

    protected virtual void UpdateRotationSpeed()
    {
        float targetSpeedMagnitude = rotationSpeed.magnitude;
        Vector3 normalizedAxis = targetSpeedMagnitude > Mathf.Epsilon
            ? rotationSpeed / targetSpeedMagnitude
            : Vector3.zero;

        UpdateRotationSpeed(normalizedAxis, targetSpeedMagnitude);
    }

    protected void UpdateRotationSpeed(Vector3 normalizedAxis, float targetSpeedMagnitude)
    {
        Vector3 targetRotationSpeed = normalizedAxis.sqrMagnitude > Mathf.Epsilon
            ? normalizedAxis.normalized * Mathf.Max(0f, targetSpeedMagnitude)
            : Vector3.zero;

        currentRotationSpeed = Vector3.Lerp(
            currentRotationSpeed,
            targetRotationSpeed,
            accelerationLerp * Time.fixedDeltaTime);

        if ((targetRotationSpeed - currentRotationSpeed).sqrMagnitude <= 0.0001f)
        {
            currentRotationSpeed = targetRotationSpeed;
        }
    }

    protected virtual void ApplyRotation()
    {
        if (currentRotationSpeed.sqrMagnitude <= Mathf.Epsilon)
        {
            return;
        }

        Quaternion stepRotation = Quaternion.Lerp(
            Quaternion.identity,
            Quaternion.Euler(currentRotationSpeed),
            Time.fixedDeltaTime);

        Quaternion targetRotation = rotationSpace == Space.Self
            ? rb.rotation * stepRotation
            : stepRotation * rb.rotation;

        rb.MoveRotation(targetRotation);
    }
}
