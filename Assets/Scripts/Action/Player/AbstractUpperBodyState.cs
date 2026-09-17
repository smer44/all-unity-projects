using UnityEngine;

public abstract class AbstractUpperBodyState
{
    protected readonly UpperBodyVisualsController Controller;
    protected float TimeSinceEnter { get; private set; }
    private float duration;

    public virtual bool IsMelee => false;

    protected AbstractUpperBodyState(UpperBodyVisualsController controller)
    {
        Controller = controller;
    }

    public abstract void OnEnter();
    public virtual void Update() { }
    public virtual void FixedUpdate() { }
    public virtual void LateUpdate() { }
    public virtual void OnExit() { }
    public virtual void OnAttackRequested() { }

    protected void BeginAttack(string animationName, float attackDuration)
    {
        TimeSinceEnter = 0f;
        duration = Mathf.Max(0f, attackDuration);
        Controller.PlayAttackAnimation(animationName);
    }

    protected void UpdateAttackTimer()
    {
        TimeSinceEnter += Time.fixedDeltaTime;
        if (TimeSinceEnter >= duration)
            Controller.SetState(Controller.DefaultUpperBodyState);
    }
}
