using UnityEngine;

public class RunState : AbstractPlayerState
{
    public override bool AllowsAiming => true;
    public override bool AllowsUpperBodyAttacks => true;

    public const float MoveSpeed = 10f;
    protected virtual float MovementSpeed => MoveSpeed;

    public const string animationName = "GroundMoveBlendTree"; // "run"

    private static readonly int VelocityRightParameter = Animator.StringToHash("VelocityRight");
    private static readonly int VelocityForwardParameter = Animator.StringToHash("VelocityForward");

    public RunState(PlayerController controller) : base(controller)
    {
    }

    public override void OnEnter()
    {
        Controller.UpdateAiming();
        Controller.ResetAerialJumpCounter();

        UpperBodyVisualsController upperBody = Controller.UpperBodyVisualsController;
        if (upperBody != null && upperBody.CurrentState == upperBody.ShootTweakState)
            Controller.ShootingTweaks?.TurnOn();

        if (Controller.animator != null)
        {
            Controller.animator.speed = 1.0f;
            Controller.animator.CrossFadeInFixedTime(animationName,0.15f, 0);
        }
    }

    public override void Update()
    {
        Controller.UpdateAiming();
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

        if (Controller.CheckIsGrounded())
        {
            Controller.VerticalSpeedOnFloor();
        }
        else
        {   
            Controller.KeepFloorSpeed();
            Controller.VerticalSpeedInAir();
            Controller.ClearGroundParent();
            Controller.SetState(Controller.FallState);
            return;
        }


        if (Controller.IsBlocking())
        {
            Controller.SetState(Controller.BlockingState);
            return;
        }

        Vector2 moveInput = Controller.GetMove2D();
        if (Controller.IsEvadeModifierPressed()
            && (moveInput.y != 0f || moveInput.x != 0f))
        {
            Controller.SetState(Controller.SlideState);
            return;
        }

        if (Controller.IsPunchButtonPresssed())
        {
            Controller.SwitchToAttackState();
        }

        if (Controller.IsJumpPressed())
        {   
            Controller.KeepFloorSpeed();
            Controller.VerticalSpeedInAir();
            Controller.ClearGroundParent();
            Controller.SetState(Controller.JumpUpState);
            return;
        }

        if (Controller.IsWASDStaying())
        {
            Controller.SetState(Controller.IdleState);
            return;
        }

        if (Controller.GroundMovementState != this)
        {
            Controller.SetState(Controller.GroundMovementState);
            return;
        }

        UpdateMovementAnimation(moveInput);
        Controller.UpdateMoveInputRotated();
        Controller.MoveWASDKinematic(Controller.MoveInputRotated, MovementSpeed);


    }

    private void UpdateMovementAnimation(Vector2 moveInput)
    {
        if (Controller.animator == null || Controller.visualsPivot == null)
            return;

        // Use this tick's input because MoveInputRotated is refreshed after this call.
        Vector2 planarMovement = Controller.RotateInputByCamera(moveInput);
        Vector3 movementDirection = new Vector3(planarMovement.x, 0f, planarMovement.y);
        Controller.animator.SetFloat(VelocityForwardParameter,
            FacingCalc.GetPlanarDirectionDot(Controller.visualsPivot.forward, movementDirection));
        Controller.animator.SetFloat(VelocityRightParameter,
            FacingCalc.GetPlanarDirectionDot(Controller.visualsPivot.right, movementDirection));
    }

    public override void OnExit()
    {
        Controller.ShootingTweaks?.TurnOff();
    }
}
