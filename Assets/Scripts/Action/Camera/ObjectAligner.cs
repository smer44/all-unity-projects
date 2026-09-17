using UnityEngine;

[DisallowMultipleComponent]
public sealed class ObjectAligner : MonoBehaviour
{   

    public static ObjectAligner CameraAligner;

    [Header("References")]
    [SerializeField] private Transform objectToAlign;
    [SerializeField] private Transform target;

    [Header("Smoothing (higher = snappier)")]
    [Tooltip("Position responsiveness in 1/seconds. Higher values reach the target faster.")]
    [Min(0f)]
    [SerializeField] private float positionSharpness = 12f;

    [Tooltip("Rotation responsiveness in 1/seconds. Higher values reach the target faster.")]
    [Min(0f)]
    [SerializeField] private float rotationSharpness = 12f;

    [Header("Completion Thresholds")]
    [Tooltip("Distance at which the objectToAlign is considered aligned to the target.")]
    [Min(0f)]
    [SerializeField] private float positionEpsilon = 0.01f;

    [Tooltip("Angle in degrees at which the objectToAlign is considered aligned to the target.")]
    [Min(0f)]
    [SerializeField] private float rotationEpsilonDegrees = 0.5f;


    void Awake()
    {
        if (CameraAligner == null)
        {
            CameraAligner = this;
        }
    }

    private void Update()
    {
        if( target != null)
        {
            MoveAlign(Time.deltaTime);

            if (IsAlignedToTarget())
            {
                target = null;
            }
        }
            
    }

    private void MoveAlign(float dt)
    {
        objectToAlign.position = CameraFacingCalc.ExpLerpMove(
            objectToAlign.position,
            target.position,
            positionSharpness,
            dt);

        objectToAlign.rotation = CameraFacingCalc.ExpLerpRotate(
            objectToAlign.rotation,
            target.rotation,
            rotationSharpness,
            dt);
    }

    private bool IsAlignedToTarget()
    {
        float positionDistance = Vector3.Distance(objectToAlign.position, target.position);
        float rotationAngle = Quaternion.Angle(objectToAlign.rotation, target.rotation);
        return positionDistance <= positionEpsilon && rotationAngle <= rotationEpsilonDegrees;
    }

    public void SetTarget(Transform newTarget) => target = newTarget;

}
