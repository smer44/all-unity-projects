using UnityEngine;

public class InteractState : AbstractPlayerState
{   
    private AbstractInteractable interactable;
    public InteractState(PlayerController controller) : base(controller)
    {
    }

    public override void OnEnter()
    {
        interactable = null;

        if (Controller.InteractObject != null
            && Controller.InteractObject.TryGetComponent<AbstractInteractable>(out var inter))
        {   
            interactable = inter;
            interactable.OnEnter(Controller);
        }

        if (Controller.animator != null)
        {
            Controller.animator.speed = 1.0f;
            Controller.animator.CrossFadeInFixedTime("Idle", 0.15f);
        }
    }

    public override void Update()
    {
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

        if (interactable != null
            && Controller.IsCancelButtonPressed()
            && interactable.AllowsBreak())
        {
            Controller.SetState(Controller.IdleState);
        }
        //Do nothing - the state is exited from other scripts
    }

    public override void OnExit()
    {
        interactable?.OnExit();
    }
}
