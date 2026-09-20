using UnityEngine;

public abstract class AbstractUnitControls : ScriptableObject
{
    public virtual void Initialize(PlayerController controller)
    {
    }

    public virtual void UpdateControls(float deltaTime)
    {
    }

    public abstract Vector2 GetMove2D();
    public abstract Vector3 GetMove3D();
    public virtual Vector3 GetFlyingMove3D()
    {
        Vector2 planarInput = GetMove2D();
        return Vector3.ClampMagnitude(new Vector3(planarInput.x, IsJumpPressed() ? 1f : 0f, planarInput.y), 1f);
    }

    public abstract Vector2 GetMouseMove2D();
    public abstract bool IsJumpPressed();
    public abstract bool IsInteractButtonPressed();
    public abstract bool IsCancelButtonPressed();
    public abstract bool IsPunchButtonPresssed();
    public abstract bool IsBlockButtonPressed();
    public virtual bool IsAimButtonPressed() => false;
    public abstract bool IsEvadeModifierPressed();
    public abstract bool WasRunWalkTogglePressed();
    public virtual bool WasFlightTogglePressed() => false;
    public virtual bool WasFlyingDashPressed() => false;
    public virtual bool IsFlyingDashPressed() => false;
}
