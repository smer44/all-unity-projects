using UnityEngine;

[CreateAssetMenu(fileName = "SimpleBotController", menuName = "ScriptableObjects/Player/SimpleBotController")]
public class SimpleBotController : AbstractUnitControls
{
    [SerializeField] private string targetObjectName = "Player";
    private Transform target;
    [SerializeField, Min(0f)] private float attackDistance = 1.5f;
    [SerializeField, Min(0f)] private float rotationSpeedDegrees = 360f;
    [SerializeField, Min(0f)] private float moveAfterAngleDegrees = 5f;
    [SerializeField, Min(0f)] private float flightHeightThreshold = 20f;
    [SerializeField, Min(0.01f)] private float firingDuration = 3f;
    [SerializeField, Min(0.01f)] private float firingPauseDuration = 3f;
    [SerializeField] private int meleeWeaponIndex = 2;

    private PlayerController owner;
    private TargetSelector targetSelector;
    private TogglerOfGameObjectKeySwitch weaponSwitch;
    private float firingCycleTime;
    private int lastWeaponIndex = -1;
    private bool wasFlying;

    public override void Initialize(PlayerController controller)
    {
        owner = controller;
        targetSelector = owner.GetComponentInChildren<TargetSelector>(true);
        weaponSwitch = owner.HandWeaponSwitch;
        if (targetSelector != null)
            targetSelector.UseMouseInput = false;
        if (weaponSwitch != null)
            weaponSwitch.UseKeyboardInput = false;
        firingCycleTime = 0f;
        lastWeaponIndex = -1;
        wasFlying = false;
        UpdateTarget();
    }

    public override void UpdateControls(float deltaTime)
    {
        UpdateTarget();
        if (owner == null || target == null)
        {
            firingCycleTime = 0f;
            lastWeaponIndex = -1;
            return;
        }

        int weaponIndex = IsTargetCloseEnough() ? meleeWeaponIndex : 1;
        weaponSwitch?.SwitchActive(weaponIndex);
        if (weaponIndex != lastWeaponIndex || (owner.IsFlying && !wasFlying))
        {
            firingCycleTime = 0f;
        }
        else
        {
            float cycleDuration = Mathf.Max(0.01f, firingDuration) + Mathf.Max(0.01f, firingPauseDuration);
            firingCycleTime = Mathf.Repeat(firingCycleTime + Mathf.Max(0f, deltaTime), cycleDuration);
        }
        lastWeaponIndex = weaponIndex;
        wasFlying = owner.IsFlying;
    }

    public override Vector2 GetMove2D()
    {
        UpdateTarget();

        if (owner == null || target == null)
        {
            return Vector2.zero;
        }

        // The shared targeted-facing states rotate a configured bot in both ground and flight modes.
        if (!owner.IsTargeted)
            RotateOwnerTowardsTarget();

        if (IsTargetCloseEnough() || !IsOwnerFacingTarget())
        {
            return Vector2.zero;
        }

        return GetMoveTowardsTargetInput();
    }

    public override Vector3 GetMove3D()
    {
        Vector2 move = GetMove2D();
        return new Vector3(move.x, 0f, move.y);
    }

    public override Vector2 GetMouseMove2D()
    {
        return Vector2.zero;
    }

    public override bool IsJumpPressed()
    {
        return false;
    }

    public override bool IsInteractButtonPressed()
    {
        return false;
    }

    public override bool IsCancelButtonPressed()
    {
        return false;
    }

    public override bool IsPunchButtonPresssed()
    {
        UpdateTarget();
        if (owner == null || target == null)
            return false;

        return owner.IsGunSelected()
            ? firingCycleTime < Mathf.Max(0.01f, firingDuration)
            : IsTargetCloseEnough();
    }

    public override bool IsBlockButtonPressed()
    {
        return false;
    }

    public override bool IsEvadeModifierPressed()
    {
        return false;
    }

    public override bool WasRunWalkTogglePressed()
    {
        return false;
    }

    public override bool WasFlightTogglePressed()
    {
        UpdateTarget();
        if (owner == null || target == null || owner.IsFlying
            || target.position.y - owner.transform.position.y < flightHeightThreshold)
        {
            return false;
        }

        // Request entry once; never toggle back out just because the height gap shrinks.
        weaponSwitch?.SwitchActive(1);
        firingCycleTime = 0f;
        return true;
    }

    private void UpdateTarget()
    {
        if (owner == null)
            return;

        if (targetSelector != null)
        {
            if (targetSelector.Selected == null)
            {
                FindTargetByName();
                if (target != null)
                    targetSelector.SetSelectedTarget(target.gameObject);
            }
            else
            {
                // Keep the current selection and its shared facing reference synchronized.
                targetSelector.SetSelectedTarget(targetSelector.Selected);
            }
            target = targetSelector.Selected != null ? targetSelector.Selected.transform : null;
            return;
        }

        if (target == null)
            FindTargetByName();
    }

    private void FindTargetByName()
    {
        target = null;
        if (string.IsNullOrWhiteSpace(targetObjectName))
        {
            return;
        }

        GameObject targetObject = GameObject.Find(targetObjectName);
        if (targetObject != null)
        {
            target = targetObject.transform;
        }
    }

    private void RotateOwnerTowardsTarget()
    {
        Transform rotatedTransform = owner.visualsPivot != null ? owner.visualsPivot : owner.transform;
        Vector3 direction = GetDirectionToTarget(rotatedTransform.position);
        if (direction.sqrMagnitude <= Mathf.Epsilon)
        {
            return;
        }

        Vector3 up = Mathf.Abs(Vector3.Dot(direction.normalized, Vector3.up)) > 0.999f ? Vector3.forward : Vector3.up;
        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, up);
        rotatedTransform.rotation = Quaternion.RotateTowards(
            rotatedTransform.rotation,
            targetRotation,
            rotationSpeedDegrees * Time.deltaTime);
    }

    private bool IsOwnerFacingTarget()
    {
        Transform rotatedTransform = owner.visualsPivot != null ? owner.visualsPivot : owner.transform;
        Vector3 direction = GetDirectionToTarget(rotatedTransform.position);
        if (direction.sqrMagnitude <= Mathf.Epsilon)
        {
            return true;
        }

        return Vector3.Angle(rotatedTransform.forward, direction.normalized) <= moveAfterAngleDegrees;
    }

    private bool IsTargetCloseEnough()
    {
        return Vector3.Distance(owner.transform.position, target.position) <= attackDistance;
    }

    private Vector3 GetDirectionToTarget(Vector3 origin)
    {
        Vector3 direction = target.position - origin;
        if (!owner.IsFlying)
            direction.y = 0f;
        return direction;
    }

    private Vector2 GetMoveTowardsTargetInput()
    {
        if (owner.Direction != null)
        {
            return Vector2.up;
        }

        Vector3 direction = GetDirectionToTarget(owner.transform.position);
        if (direction.sqrMagnitude <= Mathf.Epsilon)
        {
            return Vector2.zero;
        }

        direction.Normalize();
        return new Vector2(direction.x, direction.z);
    }
}
