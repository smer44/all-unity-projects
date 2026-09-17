using UnityEngine;

public class FreezeRotationState : AbstractPlayerVisualsRotationState
{
    private Transform frozenVisuals;
    private Quaternion frozenRotation;
    private float duration;
    private float timeSinceEnter;

    internal AbstractPlayerVisualsRotationState PreviousState { get; private set; }

    public FreezeRotationState(PlayerVisualsRotationController controller) : base(controller)
    {
    }

    internal void Configure(AbstractPlayerVisualsRotationState previousState, float freezeDuration)
    {
        duration = freezeDuration;
        timeSinceEnter = 0f;
        // Refreshing an active freeze keeps its original facing state and rotation.
        if (previousState != this)
        {
            PreviousState = previousState ?? Controller.GetDefaultState();
        }
    }

    public override void OnEnter()
    {
        frozenVisuals = Controller.PlayerController != null
            ? Controller.PlayerController.visualsPivot
            : null;
        if (frozenVisuals != null)
        {
            frozenRotation = frozenVisuals.rotation;
        }
    }

    public override void FixedUpdate()
    {
        ApplyFrozenRotation();
        timeSinceEnter += Time.fixedDeltaTime;
        if (timeSinceEnter >= duration)
        {
            Controller.ExitFreezeRotationState();
        }
    }

    public override void LateUpdate()
    {
        // Reapply after animation and parent motion to preserve world-space facing.
        ApplyFrozenRotation();
    }

    public override void OnExit()
    {
        frozenVisuals = null;
        PreviousState = null;
    }

    private void ApplyFrozenRotation()
    {
        if (frozenVisuals != null)
        {
            frozenVisuals.rotation = frozenRotation;
        }
    }
}
