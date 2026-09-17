using UnityEngine;

public class SlideState : AbstractPlayerState
{
    private const float StateDuration = 0.65f;
    private float timeSinceEnter;
    private Vector2 sideFacingRotated;

    private Vector2 movementDirection;
    private Vector3 originalScale;
    private bool hasVisuals;
    private bool wasOnGround = false;
    private float slideSpeed = 15f;

    public SlideState(PlayerController controller) : base(controller)
    {
    }

    public override void OnEnter()
    {
        Controller.StopAiming();
        Controller.VisualsRotationController?.EnterFreezeRotationState(StateDuration);
        if (Controller.animator != null)
        {   
            Controller.animator.speed = 1.5f; // everything plays 1.5x
            Controller.animator.CrossFadeInFixedTime("StandingDodgeLeft", 0.10f);
            
        }
        timeSinceEnter = 0f;

        Controller.UpdateMoveInputRotated();
        movementDirection = Controller.MoveInputRotated;
        wasOnGround = false;

    }

    public override void Update()
    {
        Controller.UpdateLook();
    }

    public override void FixedUpdate()
    {
        if (Controller.IsInWater())
        {
            Controller.ToSwimKinematics();
            Controller.SetState(Controller.SwimIdleState);
            return;
        }

        Controller.UpdateMoveInputRotated();

        bool isGrounded = Controller.CheckIsGrounded();
        if (isGrounded)
        {
            Controller.VerticalSpeedOnFloor();
            wasOnGround = true;
        }
        else
        {
            if (wasOnGround)
            {
                Controller.KeepFloorSpeed();
                Controller.ClearGroundParent();
                wasOnGround = false;
            }

            Controller.VerticalSpeedInAir();
        }

        if (Controller.IsJumpPressed())
        {   
            Controller.KeepFloorSpeed();
            Controller.VerticalSpeedInAir();
            Controller.ClearGroundParent();
            
            Controller.SetState(Controller.JumpUpState);
            return;
        }


        timeSinceEnter += Time.fixedDeltaTime;
        if (timeSinceEnter >= StateDuration)
        {
            Controller.SetState(
                isGrounded
                    ? (Controller.IsWASDMoving() ? Controller.RunOrWalkState : Controller.IdleState)
                    : Controller.FallState);
            return;
        }

        // But movement in stored direction:
        Controller.MoveWASDKinematic(movementDirection, slideSpeed);
    }

    public override void OnExit()
    {
        Controller.VisualsRotationController?.ExitFreezeRotationState();
        if (hasVisuals)
        {
            Controller.visualsPivot.localScale = originalScale;
        }
    }
}
