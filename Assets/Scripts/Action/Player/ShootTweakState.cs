public sealed class ShootTweakState : AbstractUpperBodyState
{
    public ShootTweakState(UpperBodyVisualsController controller) : base(controller) { }

    public override void OnEnter()
    {
        BeginAttack("Shoot", Controller.ShootDuration);
        PlayerController player = Controller.PlayerController;
        if (player != null && player.IsGroundRunning)
            player.ShootingTweaks?.TurnOn();
    }
    public override void FixedUpdate() => UpdateAttackTimer();

    public override void LateUpdate()
    {
        PlayerController player = Controller.PlayerController;
        if (player != null)
            player.AddVerticalRotationAngleToBones(player.GetCameraVerticalAngleDegrees());
    }

    public override void OnExit() => Controller.PlayerController?.ShootingTweaks?.TurnOff();
}
