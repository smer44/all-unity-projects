using UnityEngine;

public class LandingState : AbstractPlayerState
{
    private const float LandingDuration = 0.3f;
    private float timeSinceEnter;

    public LandingState(PlayerController controller) : base(controller)
    {
    }

    public override void OnEnter()
    {
        if (Controller.animator != null)
        {
            Controller.animator.speed = 1.0f;
            Controller.animator.CrossFadeInFixedTime("Landing",0.05f);
        }
        timeSinceEnter = 0f;
    }

    public override void Update()
    {
    }

    public override void FixedUpdate()
    {
        timeSinceEnter += Time.fixedDeltaTime;
        if (timeSinceEnter < LandingDuration)
        {
            return;
        }


        Controller.SetState(
            Controller.MoveInputRaw == Vector2.zero
                ? Controller.IdleState
                : Controller.RunOrWalkState
        );
        return;

    }

    public override void OnExit()
    {
    }
}
