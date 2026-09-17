using UnityEngine;

public class AirHitState : AbstractPlayerState
{
    private const int BaseLayer = 0;
    private const string AirHitAnimationName = "AirHit";

    public AirHitState(PlayerController controller) : base(controller)
    {
    }

    public override void OnEnter()
    {
        if (Controller.animator != null)
        {
            Controller.animator.speed = 1.5f;
            Controller.animator.CrossFadeInFixedTime(AirHitAnimationName, 0.1f);
        }
    }

    public override void Update()
    {
        Controller.UpdateLook();
    }

    public override void FixedUpdate()
    {
        Controller.VerticalSpeedInAir();

        if (Controller.IsInWater())
        {
            Controller.ToSwimKinematics();
            Controller.SetState(Controller.SwimIdleState);
            return;
        }

        Controller.UpdateMoveInputRotated();

        if (Controller.CheckIsGrounded())
        {
            Controller.VerticalSpeedOnFloor();
            Controller.SetState(
                Controller.MoveInputRaw == Vector2.zero
                    ? Controller.IdleState
                    : Controller.RunOrWalkState
            );
            return;
        }

        Controller.MoveWASDKinematic(Controller.MoveInputRotated, Controller.MoveSpeed);

        if (Controller.animator == null)
        {
            Controller.SetState(Controller.FallState);
            return;
        }

        if (Controller.animator.IsInTransition(BaseLayer))
        {
            return;
        }

        var animationState = Controller.animator.GetCurrentAnimatorStateInfo(BaseLayer);
        if (!animationState.IsName(AirHitAnimationName))
        {
            return;
        }

        if (animationState.normalizedTime >= 1f)
        {
            Controller.SetState(Controller.FallState);
        }
    }

    public override void OnExit()
    {
    }
}
