public abstract class AbstractPlayerVisualsRotationState
{
    protected readonly PlayerVisualsRotationController Controller;

    protected AbstractPlayerVisualsRotationState(PlayerVisualsRotationController controller)
    {
        Controller = controller;
    }

    public abstract void OnEnter();
    public abstract void FixedUpdate();
    public virtual void LateUpdate()
    {
    }

    public abstract void OnExit();
}
