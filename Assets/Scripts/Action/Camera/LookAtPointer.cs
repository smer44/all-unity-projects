using UnityEngine;

public class LookAtPointer : DirectionPointer
{
    [SerializeField] private Transform origin;
    [SerializeField] private Transform target;
    [SerializeField] private TargetSelector targetSelector;
    private PlayerController owner;


    public override Transform GetDirection()
    {
        UpdateDirection();
        return owner != null && owner.IsFlying ? transform : origin;
    }

    private void LateUpdate()
    {
        UpdateDirection();
    }

    private void UpdateDirection()
    {
        if (origin == null)
            return;

        if (owner == null)
            owner = origin.GetComponent<PlayerController>();
        Transform currentTarget = targetSelector != null
            ? (targetSelector.Selected != null ? targetSelector.Selected.transform : null)
            : target;
        if (currentTarget == null)
            return;

        bool flying = owner != null && owner.IsFlying;
        var direction = currentTarget.position - origin.position;
        if (!flying)
            direction.y = 0f;
        if (direction.sqrMagnitude <= Mathf.Epsilon)
        {
            return;
        }

        Vector3 up = Mathf.Abs(Vector3.Dot(direction.normalized, Vector3.up)) > 0.999f ? Vector3.forward : Vector3.up;
        // Pitch the direction pointer during flight without tilting the bot's physics root.
        Transform rotated = flying ? transform : origin;
        rotated.rotation = Quaternion.LookRotation(direction.normalized, up);
    }
}
