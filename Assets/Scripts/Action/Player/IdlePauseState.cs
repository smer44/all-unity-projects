
using UnityEngine;
public class IdlePauseState : AbstractPlayerState
{
    public IdlePauseState(PlayerController controller) : base(controller)
    {
    }

    public override void OnEnter()
    {
        if (Controller.animator != null)
        {
            Controller.animator.speed = 1.0f;
            Controller.animator.CrossFadeInFixedTime("Idle",0.15f);
        }
    }

    public override void Update()
    {
        Controller.UpdateLook();
    }

    public override void FixedUpdate()
    {
        Controller.VerticalSpeedOnFloor();

    }

    public override void OnExit()
    {
    }
}
