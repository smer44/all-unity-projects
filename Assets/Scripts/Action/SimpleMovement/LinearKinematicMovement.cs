
using UnityEngine;


public class KinematicTimelineMover : MonoBehaviour
{
    public Rigidbody rb;
    public Vector3 speed = Vector3.down;
    [SerializeField] private bool setLocalForward;
    [SerializeField] private float speedMagnitude = 1f;
    [SerializeField] protected float accelerationLerp = 0.1f;

    protected Vector3 currentSpeed;

    private void Awake()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }

        if (setLocalForward)
        {
            speed = CalculateLocalForwardSpeed();
        }

        OnMoverAwake();
    }

    private void FixedUpdate()
    {
        if (rb == null)
        {
            return;
        }

        TickMovement();
    }

    protected virtual void OnMoverAwake()
    {
    }

    private Vector3 CalculateLocalForwardSpeed()
    {
        return transform.forward * speedMagnitude;
    }

    protected virtual void TickMovement()
    {
        UpdateSpeed();
        ApplySpeed();
    }

    protected virtual void UpdateSpeed()
    {
        float targetSpeedMagnitude = speed.magnitude;
        Vector3 normalizedDirection = targetSpeedMagnitude > Mathf.Epsilon
            ? speed / targetSpeedMagnitude
            : Vector3.zero;

        UpdateSpeed(normalizedDirection, targetSpeedMagnitude);
    }

    protected void UpdateSpeed(Vector3 normalizedDirection, float targetSpeedMagnitude)
    {
        Vector3 targetSpeed = normalizedDirection.sqrMagnitude > Mathf.Epsilon
            ? normalizedDirection.normalized * Mathf.Max(0f, targetSpeedMagnitude)
            : Vector3.zero;

        currentSpeed = Vector3.Lerp(
            currentSpeed,
            targetSpeed,
            accelerationLerp * Time.fixedDeltaTime);

        if ((targetSpeed - currentSpeed).sqrMagnitude <= 0.0001f)
        {
            currentSpeed = targetSpeed;
        }
    }

    protected virtual void ApplySpeed()
    {
        rb.MovePosition(rb.position + currentSpeed * Time.fixedDeltaTime);
    }
}
