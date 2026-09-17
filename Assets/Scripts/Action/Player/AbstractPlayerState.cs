public abstract class AbstractPlayerState
{
    protected readonly PlayerController Controller;

    public virtual bool AllowsAiming => false;
    public virtual bool AllowsUpperBodyAttacks => false;

    protected AbstractPlayerState(PlayerController controller)
    {
        Controller = controller;
    }

    public abstract void OnEnter();
    public abstract void Update();
    public abstract void FixedUpdate();
    public virtual void LateUpdate()
    {
    }

    public abstract void OnExit();

    //public abstract bool AllowsBreak();
}
