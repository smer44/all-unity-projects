using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public class FlyingStateTests
{
    private GameObject root;
    private PlayerController player;
    private CameraController camera;
    private PlayerVisualsRotationController visuals;
    private Transform cameraTransform;
    private Transform pivot;
    private FlyingTestControls sourceControls;
    private FlyingTestControls playerControls;
    private GameObject attackAnimationObject;
    private CursorLockMode oldCursorLock;
    private bool oldCursorVisible;

    [SetUp]
    public void SetUp()
    {
        oldCursorLock = Cursor.lockState;
        oldCursorVisible = Cursor.visible;
        root = new GameObject("Flight tests");
        root.SetActive(false);
        pivot = Child("Orbit pivot");
        cameraTransform = Child("Camera");
        cameraTransform.position = new Vector3(0f, 0f, -2f);
        sourceControls = ScriptableObject.CreateInstance<FlyingTestControls>();
        camera = Child("Camera controller").gameObject.AddComponent<CameraController>();
        SetField(camera, "cameraPivot", pivot);
        SetField(camera, "cameraPosition", cameraTransform);
        SetField(camera, "buttonControls", sourceControls);
        SetField(camera, "lookAtSmoothDuration", 0f);
        Invoke(camera, "Awake");
        camera.SetState(camera.LockedCameraState);

        GameObject playerObject = Child("Player").gameObject;
        playerObject.AddComponent<Rigidbody>().isKinematic = true;
        playerObject.AddComponent<CapsuleCollider>();
        visuals = playerObject.AddComponent<PlayerVisualsRotationController>();
        player = playerObject.AddComponent<PlayerController>();
        player.visualsPivot = Child("Visuals");
        SetField(player, "directionPointer", camera);
        SetField(player, "buttonControls", sourceControls);
        Invoke(player, "Awake");
        playerControls = (FlyingTestControls)player.ButtonControls;
        Invoke(visuals, "Awake");
        player.SetState(player.IdleState);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(attackAnimationObject);
        Object.DestroyImmediate(root);
        Object.DestroyImmediate(playerControls);
        Object.DestroyImmediate(sourceControls);
        Cursor.lockState = oldCursorLock;
        Cursor.visible = oldCursorVisible;
    }

    [TestCase(0f, 0f, 0f, 0f)]
    [TestCase(0f, 0f, 135f, 65f)]
    [TestCase(1f, 0f, 90f, 33f)]
    [TestCase(0f, -1f, -90f, 45f)]
    [TestCase(1f, 1f, 180f, 65f)]
    public void SpaceStartsFlightMovementAlongCameraUpCombinedWithWASD(float x, float z, float pitch, float roll)
    {
        player.SetState(player.FlyingIdleState);
        cameraTransform.rotation = Quaternion.Euler(pitch, 37f, roll);
        playerControls.Move = new Vector2(x, z);
        playerControls.JumpPressed = true;
        player.FlyingIdleState.FixedUpdate();
        Vector3 expected = (cameraTransform.right * x + cameraTransform.up + cameraTransform.forward * z).normalized;
        Assert.That(player.IsFlyingMoving, Is.True);
        AssertVector(player.MoveInputRaw3D, new Vector3(x, 1f, z).normalized);
        AssertVector(player.MoveInputRotated3D, expected);
        AssertVector(player.FlyingVelocity, expected * (player.FlyingMoveAcceleration * Time.fixedDeltaTime));
        Vector3 expectedFacing = x == 0f && z == 0f ? player.visualsPivot.forward
            : (cameraTransform.right * x + cameraTransform.forward * z).normalized;
        SetField(visuals, "rotationSpeed", 1f / Time.fixedDeltaTime);
        visuals.FixedUpdateController();
        AssertVector(player.visualsPivot.forward, expectedFacing);
        playerControls.Move = Vector2.zero;
        playerControls.JumpPressed = false;
        player.FlyingMoveState.FixedUpdate();
        Assert.That(GetField<AbstractPlayerState>(player, "currentState"), Is.SameAs(player.FlyingIdleState));
    }

    [TestCase(false, false)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public void SpaceEntersUpwardEvadeAndKeepsItsCapturedDirection(bool startMoving, bool keepSpaceHeld)
    {
        player.SetState(startMoving ? (AbstractPlayerState)player.FlyingMoveState : player.FlyingIdleState);
        cameraTransform.rotation = Quaternion.Euler(135f, 37f, 65f);
        playerControls.JumpPressed = true;
        playerControls.EvadePressed = true;
        GetField<AbstractPlayerState>(player, "currentState").FixedUpdate();
        Assert.That(GetField<AbstractPlayerState>(player, "currentState"), Is.SameAs(player.AerialEvadeState));
        Vector3 capturedDirection = cameraTransform.up;
        AssertVector(player.FlyingVelocity, capturedDirection * AerialEvadeState.MoveSpeed);
        cameraTransform.rotation = Quaternion.Euler(15f, 120f, 5f);
        playerControls.EvadePressed = false;
        playerControls.JumpPressed = keepSpaceHeld;
        player.AerialEvadeState.FixedUpdate();
        AssertVector(player.FlyingVelocity, capturedDirection * AerialEvadeState.MoveSpeed);
        for (int step = 0; player.IsAerialEvading && step < 30; step++)
            player.AerialEvadeState.FixedUpdate();
        Assert.That(GetField<AbstractPlayerState>(player, "currentState"), Is.SameAs(keepSpaceHeld
            ? (AbstractPlayerState)player.FlyingMoveState : player.FlyingIdleState));
        AssertVector(player.MoveInputRotated3D, keepSpaceHeld ? cameraTransform.up : Vector3.zero);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void SpaceDashCapturesUpwardInputThenAllowsSteeringAndReturnsToMovingFlight(bool startMoving)
    {
        player.SetState(startMoving ? (AbstractPlayerState)player.FlyingMoveState : player.FlyingIdleState);
        cameraTransform.rotation = Quaternion.Euler(125f, 37f, 65f);
        playerControls.JumpPressed = true;
        StartDash();
        Vector3 capturedDirection = cameraTransform.up;
        AssertVector(player.FlyingDashState.MovementDirection, capturedDirection);
        cameraTransform.rotation = Quaternion.Euler(35f, 90f, 25f);
        playerControls.Move = Vector2.right;
        for (int step = 0; player.FlyingDashState.IsDirectionLocked && step < 100; step++)
        {
            player.FlyingDashState.FixedUpdate();
            AssertVector(player.FlyingVelocity.normalized, capturedDirection);
        }
        Assert.That(player.FlyingDashState.IsDirectionLocked, Is.False);
        player.FlyingDashState.FixedUpdate();
        AssertVector(player.FlyingVelocity.normalized, (cameraTransform.up + cameraTransform.right).normalized);
        playerControls.Move = Vector2.zero;
        playerControls.DashHeld = false;
        player.FlyingDashState.FixedUpdate();
        Assert.That(player.IsFlyingMoving, Is.True);
        AssertVector(player.MoveInputRotated3D, cameraTransform.up);
    }

    [Test]
    public void SpaceAndEvadeAfterDashLockChooseUpwardEvade()
    {
        SetField(player, "flyingDashLockDuration", 0f);
        player.SetState(player.FlyingIdleState);
        StartDash();
        playerControls.JumpPressed = true;
        playerControls.EvadePressed = true;
        player.FlyingDashState.FixedUpdate();
        Assert.That(GetField<AbstractPlayerState>(player, "currentState"), Is.SameAs(player.AerialEvadeState));
        AssertVector(player.FlyingVelocity, cameraTransform.up * AerialEvadeState.MoveSpeed);
    }

    [TestCase(0f, 0f, false)]
    [TestCase(1f, 1f, false)]
    [TestCase(-1f, -1f, true)]
    [TestCase(0f, 0f, true)]
    public void SpaceKeyboardBindingCombinesAxesEquallyAndDashDoesNotCancelAscent(float x, float z, bool dash)
    {
        var fixtureType = Assembly.Load("Unity.InputSystem.TestFramework")
            .GetType("UnityEngine.InputSystem.InputTestFixture");
        object fixture = System.Activator.CreateInstance(fixtureType);
        fixtureType.GetMethod("Setup").Invoke(fixture, null);
        Action3DButtonControls controls = ScriptableObject.CreateInstance<Action3DButtonControls>();
        try
        {
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            var keys = new System.Collections.Generic.List<Key> { Key.Space };
            if (x != 0f) keys.Add(x > 0f ? Key.D : Key.A);
            if (z != 0f) keys.Add(z > 0f ? Key.W : Key.S);
            if (dash) keys.Add(Key.LeftCtrl);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys.ToArray()));
            InputSystem.Update();
            AssertVector(controls.GetFlyingMove3D(), new Vector3(x, 1f, z).normalized);
            Assert.That(controls.GetMove2D(), Is.EqualTo(new Vector2(x, z).normalized));
            Assert.That(controls.IsFlyingDashPressed(), Is.EqualTo(dash));
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.LeftCtrl));
            InputSystem.Update();
            AssertVector(controls.GetFlyingMove3D(), Vector3.zero);
        }
        finally
        {
            Object.DestroyImmediate(controls);
            fixtureType.GetMethod("TearDown").Invoke(fixture, null);
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void DashStartsFromFlightWithCameraRelativeDirectionAndAcceleratesToOneHundred(bool moving)
    {
        Assert.That(player.FlyingMoveAcceleration, Is.EqualTo(40f));
        Assert.That(FlyingMoveState.MaxFlyingSpeed, Is.EqualTo(50f));
        Assert.That(player.FlyingDashLockDuration, Is.EqualTo(1.5f));
        player.SetState(moving ? (AbstractPlayerState)player.FlyingMoveState : player.FlyingIdleState);
        cameraTransform.rotation = Quaternion.Euler(125f, 37f, 65f);
        playerControls.Move = new Vector2(1f, 1f).normalized;
        player.SetFlyingVelocity(Vector3.up * 20f);
        StartDash();
        Vector3 expected = moving ? (cameraTransform.forward + cameraTransform.right).normalized
            : -cameraTransform.forward;
        Assert.That(player.IsFlyingDashing, Is.True);
        Assert.That(player.IsFlying, Is.True);
        Assert.That(player.PlayerBody.useGravity, Is.False);
        Assert.That(player.CurrentDisplacementApplyMode, Is.EqualTo(PlayerController.DisplacementApplyMode.Inertia));
        Assert.That(camera.CurrentState, Is.SameAs(camera.LookAtFlyingCameraState));
        AssertVector(player.FlyingVelocity, expected * 20f);
        Invoke(player, "FixedUpdateV1");
        AssertVector(player.FlyingVelocity, expected * (20f + 300f * Time.fixedDeltaTime));
        AssertVector(GetField<Vector3>(player, "localVelocity"), player.FlyingVelocity * Time.fixedDeltaTime);
        for (int i = 0; i < 60; i++)
            player.FlyingDashState.FixedUpdate();
        AssertVector(player.FlyingVelocity, expected * 100f);
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    public void DashLocksWorldDirectionAndRejectsReleaseEvadeAndFlightToggle(int exitInput)
    {
        player.SetState(player.FlyingIdleState);
        StartDash();
        Vector3 direction = player.FlyingDashState.MovementDirection;
        playerControls.DashHeld = false;
        playerControls.Move = Vector2.right;
        int steps = 0;
        while (player.FlyingDashState.IsDirectionLocked)
        {
            Assert.That(steps++, Is.LessThan(100));
            cameraTransform.rotation = Quaternion.Euler(steps * 3f, steps * 7f, steps * 2f);
            playerControls.EvadePressed = exitInput == 1;
            if (exitInput == 2)
                ToggleFlight();
            player.FlyingDashState.FixedUpdate();
            Assert.That(player.IsFlyingDashing, Is.True);
            AssertVector(player.FlyingVelocity.normalized, direction);
        }
        Assert.That(steps * Time.fixedDeltaTime, Is.InRange(1.5f, 1.5f + Time.fixedDeltaTime));
        if (exitInput == 2)
            ToggleFlight();
        else
            player.FlyingDashState.FixedUpdate();
        Assert.That(GetField<AbstractPlayerState>(player, "currentState"), Is.SameAs(exitInput == 0
            ? (AbstractPlayerState)player.FlyingMoveState : exitInput == 1 ? player.AerialEvadeState : player.IdleState));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void DashCanSteerAfterConfiguredLockAndReturnsToCurrentMovementOnRelease(bool movingOnRelease)
    {
        SetField(player, "flyingDashLockDuration", Time.fixedDeltaTime * 2.5f);
        player.SetState(player.FlyingIdleState);
        StartDash();
        for (int i = 0; i < 3; i++)
            player.FlyingDashState.FixedUpdate();
        Assert.That(player.FlyingDashState.IsDirectionLocked, Is.False);
        cameraTransform.rotation = Quaternion.Euler(75f, 120f, 30f);
        playerControls.Move = Vector2.right;
        player.FlyingDashState.FixedUpdate();
        Assert.That(player.IsFlyingDashing, Is.True);
        AssertVector(player.FlyingVelocity.normalized, cameraTransform.right);
        playerControls.DashHeld = false;
        playerControls.Move = movingOnRelease ? Vector2.up : Vector2.zero;
        player.FlyingDashState.FixedUpdate();
        Assert.That(GetField<AbstractPlayerState>(player, "currentState"), Is.SameAs(movingOnRelease
            ? (AbstractPlayerState)player.FlyingMoveState : player.FlyingIdleState));
        Assert.That(player.FlyingVelocity.magnitude, Is.LessThanOrEqualTo(50f));
        StartDash();
        Assert.That(player.FlyingDashState.IsDirectionLocked, Is.True);
    }

    [Test]
    public void TargetedDashFacesItsMovementAndRestoresTargetFacingAfterExit()
    {
        player.SetState(player.FlyingIdleState);
        Transform target = Child("Dash target");
        target.position = Vector3.right * 10f;
        visuals.target = target.gameObject;
        SetField(visuals, "rotationSpeed", 1f / Time.fixedDeltaTime);
        visuals.FixedUpdateController();
        AssertVector(player.visualsPivot.forward, Vector3.right);
        StartDash();
        for (int i = 0; i < 80; i++)
        {
            target.position += Vector3.up;
            player.FlyingDashState.FixedUpdate();
            visuals.FixedUpdateController();
            Assert.That(player.IsTargeted, Is.True);
            Assert.That(visuals.target, Is.SameAs(target.gameObject));
            AssertVector(player.visualsPivot.forward, player.FlyingVelocity.normalized);
        }
        playerControls.DashHeld = false;
        player.FlyingDashState.FixedUpdate();
        visuals.FixedUpdateController();
        visuals.FixedUpdateController();
        AssertVector(player.visualsPivot.forward, (target.position - player.visualsPivot.position).normalized);
    }

    [Test]
    public void EnteringDashKeepsGunAimingAndAnAttackInProgress()
    {
        PrepareFlyingAttack(true, 1);
        playerControls.AimPressed = true;
        player.FlyingMoveState.Update();
        playerControls.AttackPressed = true;
        player.FlyingMoveState.FixedUpdate();
        player.animator.Update(0.2f);
        float progress = player.animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
        StartDash();
        player.FlyingDashState.FixedUpdate();
        player.animator.Update(0.05f);
        Assert.That(player.IsAiming, Is.True);
        Assert.That(camera.CurrentState, Is.SameAs(camera.FirstPersonFlyingCameraState));
        Assert.That(player.animator.GetCurrentAnimatorStateInfo(0).IsName("Shoot"), Is.True);
        Assert.That(player.animator.GetCurrentAnimatorStateInfo(0).normalizedTime, Is.GreaterThan(progress));
    }

    [Test]
    public void DashCannotStartFromGroundOrInterruptAnEvade()
    {
        StartDash();
        Assert.That(player.IsFlyingDashing, Is.False);
        playerControls.Move = Vector2.up;
        player.SetState(player.AerialEvadeState);
        StartDash();
        Assert.That(player.IsAerialEvading, Is.True);
    }

    [Test]
    public void OnlyLeftControlStartsAndHoldsTheFlyingDash()
    {
        var fixtureType = Assembly.Load("Unity.InputSystem.TestFramework")
            .GetType("UnityEngine.InputSystem.InputTestFixture");
        object fixture = System.Activator.CreateInstance(fixtureType);
        fixtureType.GetMethod("Setup").Invoke(fixture, null);
        Action3DButtonControls controls = ScriptableObject.CreateInstance<Action3DButtonControls>();
        try
        {
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.RightCtrl));
            InputSystem.Update();
            Assert.That(controls.WasFlyingDashPressed(), Is.False);
            Assert.That(controls.IsFlyingDashPressed(), Is.False);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.LeftCtrl));
            InputSystem.Update();
            Assert.That(controls.WasFlyingDashPressed(), Is.True);
            Assert.That(controls.IsFlyingDashPressed(), Is.True);
            InputSystem.Update();
            Assert.That(controls.WasFlyingDashPressed(), Is.False);
            Assert.That(controls.IsFlyingDashPressed(), Is.True);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            InputSystem.Update();
            Assert.That(controls.IsFlyingDashPressed(), Is.False);
        }
        finally
        {
            Object.DestroyImmediate(controls);
            fixtureType.GetMethod("TearDown").Invoke(fixture, null);
        }
    }

    private void StartDash()
    {
        playerControls.DashPressed = true;
        playerControls.DashHeld = true;
        Invoke(player, "UpdateFlyingDash");
        playerControls.DashPressed = false;
    }

    [TestCase(-1f)]
    [TestCase(1f)]
    public void PitchContinuesThroughTwoFullTurns(float sign)
    {
        Quaternion rotation = Quaternion.identity;
        for (int step = 1; step <= 144; step++)
        {
            Quaternion previous = rotation;
            rotation = CameraFacingCalc.RotateOrbitLocal(rotation, new Vector2(0f, sign * 5f), 1f);
            Assert.That(Quaternion.Angle(previous, rotation), Is.EqualTo(5f).Within(0.01f));
            Vector3 expected = Quaternion.AngleAxis(-sign * step * 5f, Vector3.right) * Vector3.forward;
            AssertVector(rotation * Vector3.forward, expected);
        }
        AssertVector(rotation * Vector3.up, Vector3.up);
    }

    [TestCase(130f, 65f)]
    [TestCase(90f, 0f)]
    [TestCase(180f, 180f)]
    public void HorizontalLookTurnsAroundCurrentCameraUp(float pitch, float roll)
    {
        Quaternion before = Quaternion.Euler(pitch, 37f, roll);
        Quaternion after = CameraFacingCalc.RotateOrbitLocal(before, new Vector2(30f, 0f), 1f);
        AssertVector(after * Vector3.up, before * Vector3.up);
        Assert.That(Vector3.Dot(after * Vector3.forward, before * Vector3.forward),
            Is.EqualTo(Mathf.Cos(30f * Mathf.Deg2Rad)).Within(0.0001f));
        Assert.That(Vector3.Dot(after * Vector3.forward, before * Vector3.right),
            Is.EqualTo(0.5f).Within(0.0001f));
    }

    [Test]
    public void FlyingCameraKeepsMovingPivotCenteredAfterPitchAndLocalYaw()
    {
        camera.SetState(CameraController.CameraStateKind.LookAtFlying);
        sourceControls.Look = new Vector2(0f, -1800f);
        camera.Update();
        sourceControls.Look = new Vector2(450f, 0f);
        pivot.position = new Vector3(12f, 7f, -5f);
        camera.Update();
        Assert.That(Vector3.Distance(cameraTransform.position, pivot.position), Is.EqualTo(2f).Within(0.0001f));
        AssertVector(cameraTransform.forward, (pivot.position - cameraTransform.position).normalized);
        AssertVector(cameraTransform.up, Vector3.down);
    }

    [TestCase(1f, 0f, 135f, 65f)]
    [TestCase(-1f, 0f, 135f, 65f)]
    [TestCase(0f, 1f, 135f, 65f)]
    [TestCase(0f, -1f, 135f, 65f)]
    [TestCase(1f, 1f, 135f, 65f)]
    [TestCase(0f, 1f, 90f, 33f)]
    [TestCase(0f, 1f, -90f, 33f)]
    [TestCase(0f, 1f, 180f, 0f)]
    [TestCase(1f, 0f, 180f, 180f)]
    public void FlightFacesMovementLikeSwimmingWithCameraRelativeUp(float x, float y, float pitch, float roll)
    {
        cameraTransform.rotation = Quaternion.Euler(pitch, 37f, roll);
        player.SetState(player.FlyingIdleState);
        playerControls.Move = new Vector2(x, y).normalized;
        player.FlyingIdleState.FixedUpdate();
        Vector3 expected = (cameraTransform.right * x + cameraTransform.forward * y).normalized;
        AssertVector(player.MoveInputRotated3D, expected);
        Assert.That(Vector3.Dot(player.MoveInputRotated3D, cameraTransform.up), Is.EqualTo(0f).Within(0.0001f));
        AssertVector(GetField<Vector3>(player, "localVelocity"),
            expected * (player.FlyingMoveAcceleration * Time.fixedDeltaTime * Time.fixedDeltaTime));

        SetField(visuals, "rotationSpeed", 1f / Time.fixedDeltaTime);
        visuals.FixedUpdateController();
        Vector3 movement = GetField<Vector3>(player, "localVelocity");
        Transform swimmingVisuals = Child("Swimming visuals");
        FacingCalc.RotateToFacing3D(swimmingVisuals, movement.normalized, null, 1f / Time.fixedDeltaTime);
        AssertVector(swimmingVisuals.forward, movement.normalized);
        AssertVector(player.visualsPivot.forward, swimmingVisuals.forward);
        AssertVector(player.visualsPivot.up, cameraTransform.up);
    }

    [TestCase(50f)]
    [TestCase(100f)]
    public void FlyingMoveFacesCachedInputRegardlessOfInertia(float initialSpeed)
    {
        player.SetState(player.FlyingMoveState);
        cameraTransform.rotation = Quaternion.Euler(135f, 37f, 65f);
        player.SetFlyingVelocity(cameraTransform.forward * initialSpeed);
        playerControls.Move = Vector2.right;
        player.FlyingMoveState.FixedUpdate();
        Vector3 inputDirection = player.MoveInputRotated3D;
        Assert.That(Vector3.Angle(player.FlyingVelocity, inputDirection), Is.GreaterThan(45f));

        // Facing uses the input already converted for this tick, including at the speed cap.
        playerControls.Move = Vector2.zero;
        cameraTransform.rotation = Quaternion.Euler(25f, 170f, 45f);
        SetField(visuals, "rotationSpeed", 1f / Time.fixedDeltaTime);
        visuals.FixedUpdateController();
        AssertVector(player.visualsPivot.forward, inputDirection.normalized);
    }

    [Test]
    public void FlyingIdleKeepsItsFacingWhenThereIsNoMovement()
    {
        player.SetState(player.FlyingMoveState);
        playerControls.Move = Vector2.up;
        player.FlyingMoveState.FixedUpdate();
        visuals.FixedUpdateController();
        Quaternion lastFacing = player.visualsPivot.rotation;

        playerControls.Move = Vector2.zero;
        player.FlyingMoveState.FixedUpdate();
        for (int i = 0; i < 200; i++)
        {
            player.FlyingIdleState.FixedUpdate();
            visuals.FixedUpdateController();
        }
        AssertVector(player.FlyingVelocity, Vector3.zero);
        lastFacing = player.visualsPivot.rotation;
        cameraTransform.rotation = Quaternion.Euler(125f, 20f, 60f);
        visuals.FixedUpdateController();
        Assert.That(Quaternion.Angle(player.visualsPivot.rotation, lastFacing), Is.LessThan(0.01f));
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    public void AerialStatesTrackTargetsUnlessRotationIsFrozen(int stateIndex)
    {
        playerControls.Move = Vector2.right;
        player.SetState(GetAerialState(stateIndex));
        player.visualsPivot.position = new Vector3(7f, 11f, -4f);
        Transform target = Child("Target");
        visuals.target = target.gameObject;
        SetField(visuals, "rotationSpeed", 1f / Time.fixedDeltaTime);
        Vector3 velocity = player.FlyingVelocity;
        Vector3 entryFacing = player.visualsPivot.forward;

        foreach (Vector3 offset in new[] { new Vector3(-8f, 12f, 5f), new Vector3(9f, -6f, -7f) })
        {
            target.position = player.visualsPivot.position + offset;
            visuals.FixedUpdateController();
            Invoke(visuals, "LateUpdate");
            AssertVector(player.visualsPivot.forward, stateIndex < 2 ? offset.normalized : entryFacing);
            AssertVector(player.FlyingVelocity, velocity);
            Assert.That(GetField<AbstractPlayerVisualsRotationState>(visuals, "currentState"),
                Is.SameAs(stateIndex < 2
                    ? (AbstractPlayerVisualsRotationState)visuals.FlyingTargetedState
                    : visuals.FreezeRotationState));
        }
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    public void SelectingAndClearingTargetRespectsAerialRotationFreeze(int stateIndex)
    {
        playerControls.Move = Vector2.right;
        player.SetState(GetAerialState(stateIndex));
        player.UpdateFlyingMoveInput();
        player.SetFlyingVelocity(Vector3.forward * 10f);
        SetField(visuals, "rotationSpeed", 1f / Time.fixedDeltaTime);
        Vector3 entryFacing = player.visualsPivot.forward;
        visuals.FixedUpdateController();
        Assert.That(GetField<AbstractPlayerVisualsRotationState>(visuals, "currentState"),
            Is.SameAs(stateIndex < 2
                ? (AbstractPlayerVisualsRotationState)visuals.FlyingNotTargetedState
                : visuals.FreezeRotationState));

        Transform target = Child("Target");
        target.position = new Vector3(-10f, 10f, -10f);
        visuals.target = target.gameObject;
        // Like ground targeting, one tick switches states and the next turns the visuals.
        visuals.FixedUpdateController();
        visuals.FixedUpdateController();
        Invoke(visuals, "LateUpdate");
        Vector3 targetedFacing = target.position.normalized;
        AssertVector(player.visualsPivot.forward, stateIndex < 2 ? targetedFacing : entryFacing);

        visuals.target = null;
        visuals.FixedUpdateController();
        visuals.FixedUpdateController();
        Invoke(visuals, "LateUpdate");
        Assert.That(GetField<AbstractPlayerVisualsRotationState>(visuals, "currentState"),
            Is.SameAs(stateIndex < 2
                ? (AbstractPlayerVisualsRotationState)visuals.FlyingNotTargetedState
                : visuals.FreezeRotationState));
        Vector3 expected = stateIndex == 0 ? Vector3.forward
            : stateIndex == 1 ? Vector3.right : entryFacing;
        AssertVector(player.visualsPivot.forward, expected);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void TargetedEvadesFreezeAcrossPhysicsAndLateUpdatesThenResumeTracking(bool downwards)
    {
        playerControls.Move = Vector2.right;
        AerialEvadeState evade = downwards ? player.AerialEvadeDownwardsState : player.AerialEvadeState;
        player.SetState(player.FlyingIdleState);
        Transform target = Child("Target");
        target.position = new Vector3(-10f, 10f, 0f);
        visuals.target = target.gameObject;
        visuals.FixedUpdateController();
        Quaternion entryRotation = player.visualsPivot.rotation;
        player.SetState(evade);

        for (int i = 0; i < 3; i++)
        {
            target.position += Vector3.up * 3f;
            player.visualsPivot.rotation = Quaternion.Euler(10f * i, 90f, 30f);
            evade.FixedUpdate();
            visuals.FixedUpdateController();
            Assert.That(Quaternion.Angle(player.visualsPivot.rotation, entryRotation), Is.LessThan(0.01f));
            player.visualsPivot.rotation = Quaternion.identity;
            Invoke(visuals, "LateUpdate");
            Assert.That(Quaternion.Angle(player.visualsPivot.rotation, entryRotation), Is.LessThan(0.01f));
        }

        player.SetState(player.FlyingIdleState);
        Assert.That(GetField<AbstractPlayerVisualsRotationState>(visuals, "currentState"),
            Is.SameAs(visuals.FlyingTargetedState));
        SetField(visuals, "rotationSpeed", 1f / Time.fixedDeltaTime);
        visuals.FixedUpdateController();
        AssertVector(player.visualsPivot.forward, target.position.normalized);
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    public void RotationFreezeTimesOutAndRestoresTheExactPreviousFacingState(int stateIndex)
    {
        var previousState = new AbstractPlayerVisualsRotationState[]
        {
            visuals.SurfaceNotTargetedState, visuals.SurfaceTargetedState, visuals.WaterNotTargetedState,
            visuals.FlyingNotTargetedState, visuals.FlyingTargetedState
        }[stateIndex];
        visuals.SetState(previousState);
        player.visualsPivot.SetParent(player.transform);
        player.visualsPivot.rotation = Quaternion.Euler(23f, 47f, 81f);
        Quaternion entryRotation = player.visualsPivot.rotation;
        visuals.EnterFreezeRotationState(Time.fixedDeltaTime * 2.5f);
        // Even a locomotion mode change must wait for the freeze to finish.
        player.SetState(player.FlyingIdleState);
        player.transform.rotation = Quaternion.Euler(70f, 10f, 20f);

        for (int i = 0; i < 2; i++)
        {
            visuals.FixedUpdateController();
            Assert.That(GetField<AbstractPlayerVisualsRotationState>(visuals, "currentState"),
                Is.SameAs(visuals.FreezeRotationState));
            Assert.That(Quaternion.Angle(player.visualsPivot.rotation, entryRotation), Is.LessThan(0.01f));
            player.visualsPivot.rotation = Quaternion.identity;
            Invoke(visuals, "LateUpdate");
            Assert.That(Quaternion.Angle(player.visualsPivot.rotation, entryRotation), Is.LessThan(0.01f));
        }

        visuals.FixedUpdateController();
        Assert.That(GetField<AbstractPlayerVisualsRotationState>(visuals, "currentState"), Is.SameAs(previousState));
        visuals.ExitFreezeRotationState();
        Assert.That(GetField<AbstractPlayerVisualsRotationState>(visuals, "currentState"), Is.SameAs(previousState));
        player.visualsPivot.rotation = Quaternion.identity;
        Invoke(visuals, "LateUpdate");
        Assert.That(Quaternion.Angle(player.visualsPivot.rotation, Quaternion.identity), Is.LessThan(0.01f));
    }

    [Test]
    public void RefreshingRotationFreezeRestartsDurationWithoutLosingOriginalStateOrRotation()
    {
        visuals.SetState(visuals.SurfaceTargetedState);
        Quaternion entryRotation = Quaternion.Euler(23f, 47f, 81f);
        player.visualsPivot.rotation = entryRotation;
        visuals.EnterFreezeRotationState(Time.fixedDeltaTime * 2.5f);
        visuals.FixedUpdateController();
        visuals.FixedUpdateController();
        player.visualsPivot.rotation = Quaternion.identity;
        visuals.EnterFreezeRotationState(Time.fixedDeltaTime * 2.5f);
        visuals.FixedUpdateController();
        visuals.FixedUpdateController();
        Assert.That(GetField<AbstractPlayerVisualsRotationState>(visuals, "currentState"),
            Is.SameAs(visuals.FreezeRotationState));
        Assert.That(Quaternion.Angle(player.visualsPivot.rotation, entryRotation), Is.LessThan(0.01f));
        visuals.FixedUpdateController();
        Assert.That(GetField<AbstractPlayerVisualsRotationState>(visuals, "currentState"),
            Is.SameAs(visuals.SurfaceTargetedState));
    }

    [TestCase(0, false)]
    [TestCase(0, true)]
    [TestCase(1, false)]
    [TestCase(1, true)]
    [TestCase(2, false)]
    [TestCase(2, true)]
    public void GroundAndAerialEvadesReleaseRotationOnNormalOrEarlyExit(int evadeIndex, bool earlyExit)
    {
        playerControls.Move = Vector2.right;
        player.SetState(evadeIndex == 0 ? (AbstractPlayerState)player.IdleState : player.FlyingIdleState);
        visuals.FixedUpdateController();
        var previousState = GetField<AbstractPlayerVisualsRotationState>(visuals, "currentState");
        var evade = new AbstractPlayerState[]
        {
            player.SlideState, player.AerialEvadeState, player.AerialEvadeDownwardsState
        }[evadeIndex];
        player.visualsPivot.rotation = Quaternion.Euler(23f, 47f, 81f);
        Quaternion entryRotation = player.visualsPivot.rotation;
        player.SetState(evade);
        Assert.That(GetField<AbstractPlayerVisualsRotationState>(visuals, "currentState"),
            Is.SameAs(visuals.FreezeRotationState));
        playerControls.Move = Vector2.left;
        visuals.FixedUpdateController();
        Invoke(visuals, "LateUpdate");
        Assert.That(Quaternion.Angle(player.visualsPivot.rotation, entryRotation), Is.LessThan(0.01f));

        if (earlyExit)
        {
            player.SetState(evadeIndex == 0 ? (AbstractPlayerState)player.IdleState : player.FlyingIdleState);
        }
        else
        {
            // Advance locomotion alone so its OnExit must explicitly release the freeze.
            for (int i = 0; i < 100 && GetField<AbstractPlayerState>(player, "currentState") == evade; i++)
            {
                evade.FixedUpdate();
            }
        }

        Assert.That(GetField<AbstractPlayerState>(player, "currentState"), Is.Not.SameAs(evade));
        Assert.That(GetField<AbstractPlayerVisualsRotationState>(visuals, "currentState"), Is.SameAs(previousState));
        player.visualsPivot.rotation = Quaternion.identity;
        Invoke(visuals, "LateUpdate");
        Assert.That(Quaternion.Angle(player.visualsPivot.rotation, Quaternion.identity), Is.LessThan(0.01f));
    }

    [TestCase(0f)]
    [TestCase(-1f)]
    public void NonPositiveRotationFreezeDurationLeavesRotationUnfrozen(float duration)
    {
        visuals.SetState(visuals.SurfaceNotTargetedState);
        visuals.EnterFreezeRotationState(duration);
        Assert.That(GetField<AbstractPlayerVisualsRotationState>(visuals, "currentState"),
            Is.SameAs(visuals.SurfaceNotTargetedState));
        visuals.EnterFreezeRotationState(1f);
        visuals.EnterFreezeRotationState(duration);
        Assert.That(GetField<AbstractPlayerVisualsRotationState>(visuals, "currentState"),
            Is.SameAs(visuals.SurfaceNotTargetedState));
    }

    [TestCase(1f)]
    [TestCase(-1f)]
    [TestCase(0f)]
    public void FlyingTargetsHandleDirectlyVerticalAndCoincidentPositions(float height)
    {
        player.SetState(player.FlyingIdleState);
        Transform target = Child("Target");
        target.position = player.visualsPivot.position + Vector3.up * height;
        visuals.target = target.gameObject;
        SetField(visuals, "rotationSpeed", 1f / Time.fixedDeltaTime);
        Quaternion before = player.visualsPivot.rotation;
        visuals.FixedUpdateController();

        if (height == 0f)
            Assert.That(Quaternion.Angle(player.visualsPivot.rotation, before), Is.LessThan(0.01f));
        else
            AssertVector(player.visualsPivot.forward, Vector3.up * height);
    }

    [Test]
    public void DestroyingFlightTargetReturnsToUntargetedFacing()
    {
        player.SetState(player.FlyingIdleState);
        Transform target = Child("Target");
        target.position = new Vector3(10f, 10f, 0f);
        visuals.target = target.gameObject;
        SetField(visuals, "rotationSpeed", 1f / Time.fixedDeltaTime);
        visuals.FixedUpdateController();

        Object.DestroyImmediate(target.gameObject);
        player.SetFlyingVelocity(Vector3.back * 10f);
        visuals.FixedUpdateController();
        visuals.FixedUpdateController();
        AssertVector(player.visualsPivot.forward, Vector3.back);
        Assert.That(GetField<AbstractPlayerVisualsRotationState>(visuals, "currentState"),
            Is.SameAs(visuals.FlyingNotTargetedState));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void GroundAndFlightTransitionsChooseTheMatchingTargetingState(bool targeted)
    {
        Transform target = Child("Target");
        target.position = new Vector3(10f, 10f, 0f);
        visuals.target = targeted ? target.gameObject : null;
        SetField(visuals, "rotationSpeed", 1f / Time.fixedDeltaTime);
        visuals.FixedUpdateController();

        player.SetState(player.FlyingIdleState);
        visuals.FixedUpdateController();
        Assert.That(GetField<AbstractPlayerVisualsRotationState>(visuals, "currentState"),
            Is.SameAs(targeted ? (AbstractPlayerVisualsRotationState)visuals.FlyingTargetedState
                : visuals.FlyingNotTargetedState));
        if (targeted)
            AssertVector(player.visualsPivot.forward, target.position.normalized);

        player.SetState(player.IdleState);
        visuals.FixedUpdateController();
        Assert.That(GetField<AbstractPlayerVisualsRotationState>(visuals, "currentState"),
            Is.SameAs(targeted ? (AbstractPlayerVisualsRotationState)visuals.SurfaceTargetedState
                : visuals.SurfaceNotTargetedState));
        if (targeted)
            AssertVector(player.visualsPivot.forward, Vector3.right);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ToggleExitsEitherFlyingStateAndRestoresCameraAndGravity(bool exitWhileMoving)
    {
        AbstractCameraState originalCamera = camera.CurrentState;
        player.localVerticalSpeedAccumulator = -30f;
        ToggleFlight();
        Assert.That(GetField<AbstractPlayerState>(player, "currentState"), Is.SameAs(player.FlyingIdleState));
        Assert.That(player.PlayerBody.useGravity, Is.False);
        Assert.That(player.localVerticalSpeedAccumulator, Is.Zero);
        Assert.That(camera.CurrentState, Is.SameAs(camera.LookAtFlyingCameraState));

        // Multiple idle/move transitions must preserve the original camera.
        for (int i = 0; i < 2; i++)
        {
            playerControls.Move = Vector2.up;
            player.FlyingIdleState.FixedUpdate();
            Assert.That(GetField<AbstractPlayerState>(player, "currentState"), Is.SameAs(player.FlyingMoveState));
            // Build enough momentum to coast after the unchanged idle braking step.
            for (int moveStep = 0; moveStep < 3; moveStep++)
                player.FlyingMoveState.FixedUpdate();
            Assert.That(camera.CurrentState, Is.SameAs(camera.LookAtFlyingCameraState));
            playerControls.Move = Vector2.zero;
            player.FlyingMoveState.FixedUpdate();
            Assert.That(GetField<AbstractPlayerState>(player, "currentState"), Is.SameAs(player.FlyingIdleState));
            Assert.That(player.FlyingVelocity.magnitude, Is.GreaterThan(0f));
        }

        if (exitWhileMoving)
        {
            playerControls.Move = Vector2.right;
            player.FlyingIdleState.FixedUpdate();
        }
        player.visualsPivot.rotation = Quaternion.Euler(125f, 20f, 60f);
        ToggleFlight();
        Assert.That(GetField<AbstractPlayerState>(player, "currentState"), Is.SameAs(player.IdleState));
        Assert.That(player.IsFlying, Is.False);
        Assert.That(player.PlayerBody.useGravity, Is.True);
        Assert.That(camera.CurrentState, Is.SameAs(originalCamera));
        AssertVector(player.visualsPivot.up, Vector3.up);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void EvadeModifierWithMovementEntersAerialEvadeFromEitherFlyingState(bool startMoving)
    {
        player.SetState(startMoving ? (AbstractPlayerState)player.FlyingMoveState : player.FlyingIdleState);
        playerControls.Move = Vector2.right;
        playerControls.EvadePressed = true;
        if (startMoving)
            player.FlyingMoveState.FixedUpdate();
        else
            player.FlyingIdleState.FixedUpdate();

        Assert.That(GetField<AbstractPlayerState>(player, "currentState"), Is.SameAs(player.AerialEvadeState));
        Assert.That(player.IsFlying, Is.True);
        Assert.That(player.PlayerBody.useGravity, Is.False);
        Assert.That(camera.CurrentState, Is.SameAs(camera.LookAtFlyingCameraState));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void EvadeWithoutMovementEntersDownwardsStateFromEitherFlyingState(bool startMoving)
    {
        player.SetState(startMoving ? (AbstractPlayerState)player.FlyingMoveState : player.FlyingIdleState);
        playerControls.EvadePressed = true;
        playerControls.Move = Vector2.zero;
        if (startMoving)
            player.FlyingMoveState.FixedUpdate();
        else
            player.FlyingIdleState.FixedUpdate();

        Assert.That(player.IsAerialEvading, Is.True);
        Assert.That(GetField<AbstractPlayerState>(player, "currentState"), Is.SameAs(player.AerialEvadeDownwardsState));
        Assert.That(camera.CurrentState, Is.SameAs(camera.LookAtFlyingNormalizingCameraState));
    }

    [Test]
    public void AerialEvadeFreezesMovementAndFacingWhileTheCameraCanRotate()
    {
        player.SetState(player.FlyingMoveState);
        cameraTransform.rotation = Quaternion.Euler(135f, 37f, 65f);
        player.visualsPivot.rotation = Quaternion.Euler(23f, 47f, 81f);
        Quaternion initialFacing = player.visualsPivot.rotation;
        Quaternion initialCameraRotation = cameraTransform.rotation;
        playerControls.Move = new Vector2(1f, 1f).normalized;
        Vector3 initialMovement = (cameraTransform.right + cameraTransform.forward).normalized;
        playerControls.EvadePressed = true;
        player.FlyingMoveState.FixedUpdate();
        SetField(visuals, "rotationSpeed", 1f / Time.fixedDeltaTime);

        for (int i = 0; i < 5; i++)
        {
            playerControls.Move = i % 2 == 0 ? Vector2.zero : Vector2.left;
            sourceControls.Look = new Vector2(200f, -300f);
            camera.Update();
            player.AerialEvadeState.FixedUpdate();
            visuals.FixedUpdateController();
            AssertVector(GetField<Vector3>(player, "localVelocity"),
                initialMovement * (AerialEvadeState.MoveSpeed * Time.fixedDeltaTime));
            Assert.That(Quaternion.Angle(player.visualsPivot.rotation, initialFacing), Is.LessThan(0.01f));
            Assert.That(camera.CurrentState, Is.SameAs(camera.LookAtFlyingCameraState));
        }

        Assert.That(Quaternion.Angle(cameraTransform.rotation, initialCameraRotation), Is.GreaterThan(1f));
        player.visualsPivot.rotation = Quaternion.identity;
        Invoke(visuals, "LateUpdate");
        Assert.That(Quaternion.Angle(player.visualsPivot.rotation, initialFacing), Is.LessThan(0.01f));
    }

    [TestCase(false, false)]
    [TestCase(true, false)]
    [TestCase(false, true)]
    [TestCase(true, true)]
    public void AerialEvadeEndsAfterConfiguredDurationAndUsesCurrentInput(bool keepMoving, bool downwards)
    {
        player.SetState(player.FlyingIdleState);
        playerControls.Move = Vector2.up;
        AerialEvadeState evade = downwards ? player.AerialEvadeDownwardsState : player.AerialEvadeState;
        player.SetState(evade);
        Vector3 evadeDirection = player.FlyingVelocity.normalized;
        playerControls.Move = keepMoving ? Vector2.right : Vector2.zero;
        cameraTransform.rotation = Quaternion.Euler(135f, 20f, 70f);

        float duration = (float)typeof(AerialEvadeState).GetField("StateDuration",
            BindingFlags.Static | BindingFlags.NonPublic).GetRawConstantValue();
        int ticks = Mathf.CeilToInt(duration / Time.fixedDeltaTime);
        for (int i = 0; i < ticks - 1; i++)
        {
            evade.FixedUpdate();
            Assert.That(player.IsAerialEvading, Is.True);
            Assert.That(player.FlyingVelocity.magnitude, Is.EqualTo(AerialEvadeState.MoveSpeed).Within(0.001f));
        }
        evade.FixedUpdate();
        AssertVector(player.FlyingVelocity, evadeDirection * (downwards ? 5f : 20f));
        AssertVector(GetField<Vector3>(player, "localVelocity"), player.FlyingVelocity * Time.fixedDeltaTime);
        Assert.That(GetField<AbstractPlayerState>(player, "currentState"),
            Is.SameAs(keepMoving ? (AbstractPlayerState)player.FlyingMoveState : player.FlyingIdleState));
        AssertVector(player.MoveInputRotated3D, keepMoving ? cameraTransform.right : Vector3.zero);
        Assert.That(camera.CurrentState, Is.SameAs(downwards
            ? (AbstractCameraState)camera.LookAtFlyingNormalizingCameraState
            : camera.LookAtFlyingCameraState));
        Assert.That(player.PlayerBody.useGravity, Is.False);

        if (keepMoving)
        {
            // Leaving an evade retains momentum; moving flight faces the current input.
            player.FlyingMoveState.FixedUpdate();
            SetField(visuals, "rotationSpeed", 1f / Time.fixedDeltaTime);
            visuals.FixedUpdateController();
            AssertVector(player.visualsPivot.forward, player.MoveInputRotated3D.normalized);
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void LeavingFlightDuringAerialEvadeRestoresTheOriginalCamera(bool downwards)
    {
        AbstractCameraState originalCamera = camera.CurrentState;
        player.SetState(player.FlyingIdleState);
        playerControls.Move = Vector2.up;
        player.SetState(downwards ? player.AerialEvadeDownwardsState : player.AerialEvadeState);
        ToggleFlight();
        camera.LookAtFlyingNormalizingCameraState.Update(0.5f);
        Assert.That(player.IsFlying, Is.False);
        Assert.That(player.PlayerBody.useGravity, Is.True);
        Assert.That(camera.CurrentState, Is.SameAs(originalCamera));
    }

    [Test]
    public void DownwardsEvadeUsesGlobalDownAndRetainsFacingAsCameraLevels()
    {
        player.SetState(player.FlyingIdleState);
        Quaternion rotation = Quaternion.Euler(135f, 37f, 65f);
        cameraTransform.SetPositionAndRotation(pivot.position - rotation * Vector3.forward * 2f, rotation);
        player.visualsPivot.rotation = Quaternion.Euler(23f, 47f, 81f);
        Quaternion initialFacing = player.visualsPivot.rotation;
        playerControls.EvadePressed = true;
        player.FlyingIdleState.FixedUpdate();

        for (int i = 0; i < 5; i++)
        {
            playerControls.Move = Vector2.right;
            camera.LookAtFlyingNormalizingCameraState.Update(0.05f);
            player.AerialEvadeDownwardsState.FixedUpdate();
            visuals.FixedUpdateController();
            Invoke(visuals, "LateUpdate");
            AssertVector(GetField<Vector3>(player, "localVelocity"),
                Vector3.down * (AerialEvadeState.MoveSpeed * Time.fixedDeltaTime));
            Assert.That(Quaternion.Angle(player.visualsPivot.rotation, initialFacing), Is.LessThan(0.01f));
            Assert.That(player.PlayerBody.useGravity, Is.False);
        }
    }

    [TestCase(35f, 350f, 340f)]
    [TestCase(135f, 37f, 65f)]
    [TestCase(0f, 180f, 180f)]
    [TestCase(90f, 20f, 45f)]
    [TestCase(-90f, 20f, 45f)]
    public void CameraNormalizesAlongShortestArcAndReturnsToFlightAfterHalfSecond(float pitch, float yaw, float roll)
    {
        Quaternion start = Quaternion.Euler(pitch, yaw, roll);
        cameraTransform.SetPositionAndRotation(pivot.position - start * Vector3.forward * 2f, start);
        camera.SetState(CameraController.CameraStateKind.LookAtFlyingNormalizing);
        var samples = new System.Collections.Generic.List<Quaternion>();
        sourceControls.Look = new Vector2(200f, -300f);
        for (int i = 0; i < 5; i++)
        {
            Assert.That(camera.CurrentState, Is.SameAs(camera.LookAtFlyingNormalizingCameraState));
            pivot.position += Vector3.down;
            camera.LookAtFlyingNormalizingCameraState.Update(0.1f);
            samples.Add(cameraTransform.rotation);
            AssertVector(cameraTransform.forward, (pivot.position - cameraTransform.position).normalized);
            Assert.That(Vector3.Distance(cameraTransform.position, pivot.position), Is.EqualTo(2f).Within(0.0001f));
        }

        Quaternion end = cameraTransform.rotation;
        AssertVector(cameraTransform.up, Vector3.up);
        Assert.That(cameraTransform.forward.y, Is.EqualTo(0f).Within(0.0001f));
        Assert.That(cameraTransform.position.y, Is.EqualTo(pivot.position.y).Within(0.0001f));
        Assert.That(camera.CurrentState, Is.SameAs(camera.LookAtFlyingCameraState));
        float totalAngle = Quaternion.Angle(start, end);
        float previousRemaining = totalAngle;
        foreach (Quaternion sample in samples)
        {
            float remaining = Quaternion.Angle(sample, end);
            Assert.That(remaining, Is.LessThanOrEqualTo(previousRemaining + 0.01f));
            Assert.That(Quaternion.Angle(start, sample) + remaining, Is.EqualTo(totalAngle).Within(0.1f));
            previousRemaining = remaining;
        }

        if (Mathf.Abs(pitch) == 90f)
        {
            for (int candidateYaw = 0; candidateYaw < 360; candidateYaw += 15)
                Assert.That(totalAngle, Is.LessThanOrEqualTo(
                    Quaternion.Angle(start, Quaternion.Euler(0f, candidateYaw, 0f)) + 0.01f));
        }

        camera.Update();
        Assert.That(Quaternion.Angle(end, cameraTransform.rotation), Is.GreaterThan(1f));
    }

    [Test]
    public void PlayerAnimatorContainsTheAerialEvadeClip()
    {
        var controller = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(
            "Assets/Animation/DefaultPlayer.controller");
        var clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Animation/AerialEvade.anim");
        Assert.That(clip, Is.Not.Null);
        var state = System.Array.Find(controller.layers[0].stateMachine.states,
            child => child.state.name == "AerialEvade").state;
        Assert.That(state, Is.Not.Null);
        Assert.That(state.motion, Is.SameAs(clip));
    }

    [Test]
    public void FlyingMovementAcceleratesAndTurnsUsingPreviousWorldVelocity()
    {
        cameraTransform.rotation = Quaternion.Euler(35f, 40f, 60f);
        player.SetState(player.FlyingMoveState);
        playerControls.Move = Vector2.up;
        AssertVector(player.FlyingVelocity, Vector3.zero);
        player.FlyingMoveState.FixedUpdate();
        AssertVector(player.FlyingVelocity,
            cameraTransform.forward * (player.FlyingMoveAcceleration * Time.fixedDeltaTime));

        float firstSpeed = player.FlyingVelocity.magnitude;
        player.FlyingMoveState.FixedUpdate();
        Assert.That(player.FlyingVelocity.magnitude, Is.GreaterThan(firstSpeed));
        Assert.That(player.FlyingVelocity.magnitude, Is.LessThan(FlyingMoveState.MaxFlyingSpeed));

        Vector3 previousVelocity = player.FlyingVelocity;
        cameraTransform.rotation = Quaternion.Euler(-35f, 132f, 15f);
        float acceleration = player.FlyingMoveAcceleration *
            (1f - previousVelocity.magnitude / FlyingMoveState.MaxFlyingSpeed);
        player.FlyingMoveState.FixedUpdate();
        AssertVector(player.FlyingVelocity, previousVelocity + cameraTransform.forward * (acceleration * Time.fixedDeltaTime));
        AssertVector(GetField<Vector3>(player, "localVelocity"), player.FlyingVelocity * Time.fixedDeltaTime);
        SetField(visuals, "rotationSpeed", 1f / Time.fixedDeltaTime);
        visuals.FixedUpdateController();
        AssertVector(player.visualsPivot.forward, player.MoveInputRotated3D.normalized);
        Assert.That(Vector3.Angle(player.visualsPivot.forward, player.FlyingVelocity), Is.GreaterThan(1f));
    }

    [Test]
    public void FlyingIdleDeceleratesWithoutAStopOnEntryAndEventuallyStopsExactly()
    {
        player.SetState(player.FlyingMoveState);
        playerControls.Move = Vector2.up;
        for (int i = 0; i < 30; i++)
            player.FlyingMoveState.FixedUpdate();
        Vector3 previousVelocity = player.FlyingVelocity;
        playerControls.Move = Vector2.zero;
        player.FlyingMoveState.FixedUpdate();
        AssertVector(player.FlyingVelocity, previousVelocity - previousVelocity.normalized *
            (FlyingIdleState.BrakingAcceleration * Time.fixedDeltaTime));
        AssertVector(GetField<Vector3>(player, "localVelocity"), player.FlyingVelocity * Time.fixedDeltaTime);

        cameraTransform.rotation = Quaternion.Euler(80f, 140f, 75f);
        previousVelocity = player.FlyingVelocity;
        player.FlyingIdleState.FixedUpdate();
        AssertVector(player.FlyingVelocity, previousVelocity - previousVelocity.normalized *
            (FlyingIdleState.BrakingAcceleration * Time.fixedDeltaTime));
        AssertVector(player.MoveInputRotated3D, Vector3.zero);
        SetField(visuals, "rotationSpeed", 1f / Time.fixedDeltaTime);
        visuals.FixedUpdateController();
        AssertVector(player.visualsPivot.forward, previousVelocity.normalized);

        float previousSpeed = player.FlyingVelocity.magnitude;
        for (int i = 0; i < 200; i++)
        {
            player.FlyingIdleState.FixedUpdate();
            Assert.That(player.FlyingVelocity.magnitude, Is.LessThanOrEqualTo(previousSpeed));
            previousSpeed = player.FlyingVelocity.magnitude;
        }
        AssertVector(player.FlyingVelocity, Vector3.zero);
        AssertVector(GetField<Vector3>(player, "localVelocity"), Vector3.zero);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void FlyingIdleBrakesFromLimitedEvadeExitSpeed(bool downwards)
    {
        player.SetState(player.FlyingIdleState);
        playerControls.Move = Vector2.right;
        AerialEvadeState evade = downwards ? player.AerialEvadeDownwardsState : player.AerialEvadeState;
        player.SetState(evade);
        evade.FixedUpdate();
        Vector3 incomingVelocity = player.FlyingVelocity;
        Assert.That(incomingVelocity.magnitude, Is.EqualTo(AerialEvadeState.MoveSpeed).Within(0.001f));
        playerControls.Move = Vector2.zero;
        for (int i = 0; i < 100 && player.IsAerialEvading; i++)
            evade.FixedUpdate();
        Assert.That(GetField<AbstractPlayerState>(player, "currentState"), Is.SameAs(player.FlyingIdleState));
        Vector3 exitVelocity = incomingVelocity.normalized * (downwards ? 5f : 20f);
        AssertVector(player.FlyingVelocity, exitVelocity);
        player.FlyingIdleState.FixedUpdate();
        AssertVector(player.FlyingVelocity, exitVelocity - exitVelocity.normalized *
            (FlyingIdleState.BrakingAcceleration * Time.fixedDeltaTime));
        AssertVector(GetField<Vector3>(player, "localVelocity"), player.FlyingVelocity * Time.fixedDeltaTime);
    }

    [TestCase(false, 0f)]
    [TestCase(false, 3f)]
    [TestCase(false, 20f)]
    [TestCase(false, 100f)]
    [TestCase(true, 0f)]
    [TestCase(true, 3f)]
    [TestCase(true, 5f)]
    [TestCase(true, 100f)]
    public void EvadeExitPreservesDirectionAndNeverIncreasesCollisionReducedSpeed(bool downwards, float speed)
    {
        playerControls.Move = Vector2.up;
        AerialEvadeState evade = downwards ? player.AerialEvadeDownwardsState : player.AerialEvadeState;
        player.SetState(evade);
        playerControls.Move = Vector2.zero;
        float duration = (float)typeof(AerialEvadeState).GetField("StateDuration",
            BindingFlags.Static | BindingFlags.NonPublic).GetRawConstantValue();
        int ticks = Mathf.CeilToInt(duration / Time.fixedDeltaTime);
        for (int i = 0; i < ticks - 1; i++)
            evade.FixedUpdate();

        // Simulate the collision solver changing velocity just before the exit tick.
        Vector3 direction = new Vector3(1f, -2f, 3f).normalized;
        player.SetFlyingVelocity(direction * speed);
        evade.FixedUpdate();
        Assert.That(GetField<AbstractPlayerState>(player, "currentState"), Is.SameAs(player.FlyingIdleState));
        AssertVector(player.FlyingVelocity, direction * Mathf.Min(speed, downwards ? 5f : 20f));
        AssertVector(GetField<Vector3>(player, "localVelocity"), player.FlyingVelocity * Time.fixedDeltaTime);
    }

    [Test]
    public void ReenteringFlyingMoveAddsAccelerationToRemainingMomentum()
    {
        player.SetState(player.FlyingMoveState);
        playerControls.Move = Vector2.up;
        for (int i = 0; i < 4; i++)
            player.FlyingMoveState.FixedUpdate();
        playerControls.Move = Vector2.zero;
        player.FlyingMoveState.FixedUpdate();
        Assert.That(player.FlyingVelocity.magnitude, Is.GreaterThan(0f));
        Vector3 previousVelocity = player.FlyingVelocity;
        playerControls.Move = Vector2.left;
        player.FlyingIdleState.FixedUpdate();
        Assert.That(GetField<AbstractPlayerState>(player, "currentState"), Is.SameAs(player.FlyingMoveState));
        float acceleration = player.FlyingMoveAcceleration *
            (1f - previousVelocity.magnitude / FlyingMoveState.MaxFlyingSpeed);
        AssertVector(player.FlyingVelocity, previousVelocity - cameraTransform.right * (acceleration * Time.fixedDeltaTime));
    }

    [Test]
    public void FlightToggleClearsMomentumBeforeStartingANewFlight()
    {
        player.SetState(player.FlyingMoveState);
        playerControls.Move = Vector2.up;
        player.FlyingMoveState.FixedUpdate();
        ToggleFlight();
        AssertVector(player.FlyingVelocity, Vector3.zero);
        playerControls.Move = Vector2.zero;
        ToggleFlight();
        player.FlyingIdleState.FixedUpdate();
        AssertVector(player.FlyingVelocity, Vector3.zero);
        AssertVector(GetField<Vector3>(player, "localVelocity"), Vector3.zero);
    }

    [TestCase(0f)]
    [TestCase(25f)]
    [TestCase(75f)]
    [TestCase(100f)]
    [TestCase(150f)]
    public void FlyingAccelerationFallsWithSpeedAndCapsTheWholeVelocity(float initialSpeed)
    {
        player.SetState(player.FlyingMoveState);
        cameraTransform.rotation = Quaternion.Euler(115f, 37f, 65f);
        playerControls.Move = new Vector2(1f, 1f).normalized;
        Vector3 initialVelocity = Vector3.up * initialSpeed;
        player.SetFlyingVelocity(initialVelocity);
        player.FlyingMoveState.FixedUpdate();
        float acceleration = player.FlyingMoveAcceleration *
            Mathf.Clamp01(1f - initialSpeed / FlyingMoveState.MaxFlyingSpeed);
        Vector3 direction = (cameraTransform.right + cameraTransform.forward).normalized;
        Vector3 expected = Vector3.ClampMagnitude(initialVelocity + direction *
            (acceleration * Time.fixedDeltaTime), FlyingMoveState.MaxFlyingSpeed);
        AssertVector(player.FlyingVelocity, expected);
        Assert.That(player.FlyingVelocity.magnitude, Is.LessThanOrEqualTo(FlyingMoveState.MaxFlyingSpeed + 0.0001f));
    }

    [Test]
    public void FlyingSpeedCapPreventsOvershootWithALargePhysicsStep()
    {
        float previousDeltaTime = Time.fixedDeltaTime;
        try
        {
            Time.fixedDeltaTime = 10f;
            player.SetState(player.FlyingMoveState);
            playerControls.Move = Vector2.up;
            player.SetFlyingVelocity(cameraTransform.forward * (FlyingMoveState.MaxFlyingSpeed * 0.9f));
            player.FlyingMoveState.FixedUpdate();
            AssertVector(player.FlyingVelocity, cameraTransform.forward * FlyingMoveState.MaxFlyingSpeed);
        }
        finally
        {
            Time.fixedDeltaTime = previousDeltaTime;
        }
    }

    [Test]
    public void EnteringFlyingMoveCapsInheritedSpeedWithoutChangingItsDirection()
    {
        player.SetState(player.FlyingIdleState);
        Vector3 direction = new Vector3(2f, -3f, 1f).normalized;
        player.SetFlyingVelocity(direction * (FlyingMoveState.MaxFlyingSpeed * 2f));
        player.SetState(player.FlyingMoveState);
        AssertVector(player.FlyingVelocity, direction * FlyingMoveState.MaxFlyingSpeed);
    }

    [TestCase(0.005f)]
    [TestCase(0.2f)]
    [TestCase(100f)]
    public void FlyingIdleBrakesAtMaximumSpeedAndStopsWithoutReversing(float initialSpeed)
    {
        player.SetState(player.FlyingIdleState);
        Vector3 direction = new Vector3(2f, -3f, 1f).normalized;
        player.SetFlyingVelocity(direction * initialSpeed);
        int ticks = Mathf.CeilToInt(initialSpeed / (FlyingIdleState.BrakingAcceleration * Time.fixedDeltaTime)) + 2;
        float previousSpeed = initialSpeed;
        for (int i = 0; i < ticks; i++)
        {
            player.FlyingIdleState.FixedUpdate();
            Assert.That(player.FlyingVelocity.magnitude, Is.LessThanOrEqualTo(previousSpeed));
            Assert.That(Vector3.Dot(player.FlyingVelocity, direction), Is.GreaterThanOrEqualTo(0f));
            previousSpeed = player.FlyingVelocity.magnitude;
        }
        AssertVector(player.FlyingVelocity, Vector3.zero);
        AssertVector(GetField<Vector3>(player, "localVelocity"), Vector3.zero);
        AssertVector(GetField<Vector3>(player, "previousAppliedDisplacement"), Vector3.zero);
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    public void EveryAerialStateUsesInertiaAndRestoresSimpleOnExit(int stateIndex)
    {
        AbstractPlayerState[] aerialStates = {
            player.FlyingMoveState, player.FlyingIdleState, player.AerialEvadeState, player.AerialEvadeDownwardsState
        };
        playerControls.Move = Vector2.right;
        player.SetState(aerialStates[stateIndex]);
        Assert.That(player.CurrentDisplacementApplyMode, Is.EqualTo(PlayerController.DisplacementApplyMode.Inertia));
        player.SetFlyingVelocity(new Vector3(10f, 20f, 30f));
        player.SetState(player.FlyingIdleState);
        Assert.That(player.CurrentDisplacementApplyMode, Is.EqualTo(PlayerController.DisplacementApplyMode.Inertia));
        Vector3 expectedVelocity = new Vector3(10f, 20f, 30f);
        if (stateIndex >= 2)
            expectedVelocity = expectedVelocity.normalized * (stateIndex == 2 ? 20f : 5f);
        AssertVector(player.FlyingVelocity, expectedVelocity);
        player.SetState(aerialStates[stateIndex]);
        player.SetState(player.IdleState);
        Assert.That(player.CurrentDisplacementApplyMode, Is.EqualTo(PlayerController.DisplacementApplyMode.Simple));
        AssertVector(player.FlyingVelocity, Vector3.zero);
        player.MoveWASDKinematic(Vector3.right, 7f);
        AssertVector(GetField<Vector3>(player, "localVelocity"), Vector3.right * (7f * Time.fixedDeltaTime));
        AssertVector(player.FlyingVelocity, Vector3.zero);
    }

    [Test]
    public void FullPhysicsPipelineRetainsVelocityAndIntegratesAccelerationExactlyOncePerTick()
    {
        SetField(player, "playerCollider", null);
        player.SetState(player.FlyingMoveState);
        playerControls.Move = Vector2.up;
        float expectedSpeed = 0f;
        for (int i = 0; i < 20; i++)
        {
            expectedSpeed += player.FlyingMoveAcceleration *
                (1f - expectedSpeed / FlyingMoveState.MaxFlyingSpeed) * Time.fixedDeltaTime;
            Invoke(player, "FixedUpdateV1");
            AssertVector(player.FlyingVelocity, cameraTransform.forward * expectedSpeed);
            AssertVector(GetField<Vector3>(player, "previousAppliedDisplacement"),
                player.FlyingVelocity * Time.fixedDeltaTime);
        }
        playerControls.Move = Vector2.zero;
        Invoke(player, "FixedUpdateV1");
        expectedSpeed -= FlyingIdleState.BrakingAcceleration * Time.fixedDeltaTime;
        AssertVector(player.FlyingVelocity, cameraTransform.forward * expectedSpeed);
        Assert.That(player.CurrentDisplacementApplyMode, Is.EqualTo(PlayerController.DisplacementApplyMode.Inertia));
    }

    [Test]
    public void CollisionSolverClipsAccumulatedFlightVelocityBeforeItIsApplied()
    {
        GameObject bodyObject = new GameObject("Flight collision body");
        GameObject wallObject = new GameObject("Flight collision wall");
        try
        {
            Vector3 origin = new Vector3(10000f, 10000f, 10000f);
            bodyObject.transform.position = origin;
            Rigidbody body = bodyObject.AddComponent<Rigidbody>();
            body.isKinematic = true;
            CapsuleCollider capsule = bodyObject.AddComponent<CapsuleCollider>();
            wallObject.transform.position = origin + Vector3.right * 1.25f;
            wallObject.AddComponent<BoxCollider>().size = new Vector3(0.2f, 10f, 10f);
            SetField(player, "playerBody", body);
            SetField(player, "playerCollider", capsule);
            Physics.SyncTransforms();
            player.SetState(player.FlyingMoveState);
            playerControls.Move = Vector2.up;
            player.SetFlyingVelocity(new Vector3(60f, 0f, 30f));
            Invoke(player, "FixedUpdateV1");
            Vector3 applied = GetField<Vector3>(player, "previousAppliedDisplacement");
            Assert.That(applied.x, Is.InRange(0f, 0.65f));
            Assert.That(applied.z, Is.GreaterThan(0.4f));
            AssertVector(player.FlyingVelocity, applied / Time.fixedDeltaTime);
            SetField(visuals, "rotationSpeed", 1f / Time.fixedDeltaTime);
            visuals.FixedUpdateController();
            AssertVector(player.visualsPivot.forward, player.MoveInputRotated3D.normalized);

            // Feed the reachable pose into the next sweep without simulating the user's scene.
            bodyObject.transform.position = origin + applied;
            body.position = origin + applied;
            Physics.SyncTransforms();
            AssertVector(bodyObject.transform.position, body.position);
            Invoke(player, "FixedUpdateV1");
            applied = GetField<Vector3>(player, "previousAppliedDisplacement");
            Assert.That(applied.x, Is.LessThan(0.01f));
            Assert.That(applied.z, Is.GreaterThan(0.4f));
            AssertVector(player.FlyingVelocity, applied / Time.fixedDeltaTime);
        }
        finally
        {
            Object.DestroyImmediate(wallObject);
            Object.DestroyImmediate(bodyObject);
        }
    }

    [Test]
    public void PausedControlsCannotEnterFlight()
    {
        player.PauseControls();
        ToggleFlight();
        Assert.That(player.IsFlying, Is.False);
        Assert.That(GetField<AbstractPlayerState>(player, "currentState"), Is.SameAs(player.IdlePauseState));
    }

    [Test]
    public void TabBindingTriggersOncePerPress()
    {
        // The Input System's test assembly is not referenced by the project's
        // default editor assembly. Use its harness without changing assembly layout.
        var fixtureType = Assembly.Load("Unity.InputSystem.TestFramework")
            .GetType("UnityEngine.InputSystem.InputTestFixture");
        object fixture = System.Activator.CreateInstance(fixtureType);
        fixtureType.GetMethod("Setup").Invoke(fixture, null);
        Action3DButtonControls controls = ScriptableObject.CreateInstance<Action3DButtonControls>();
        try
        {
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            InputSystem.Update();
            Assert.That(controls.WasFlightTogglePressed(), Is.False);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Tab));
            InputSystem.Update();
            Assert.That(controls.WasFlightTogglePressed(), Is.True);
            InputSystem.Update();
            Assert.That(controls.WasFlightTogglePressed(), Is.False);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            InputSystem.Update();
            Assert.That(controls.WasFlightTogglePressed(), Is.False);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Tab));
            InputSystem.Update();
            Assert.That(controls.WasFlightTogglePressed(), Is.True);
        }
        finally
        {
            Object.DestroyImmediate(controls);
            fixtureType.GetMethod("TearDown").Invoke(fixture, null);
        }
    }
    [TestCase(false, 0, "Punch")]
    [TestCase(false, 1, "Shoot")]
    [TestCase(false, 2, "ForwardSwordAttack")]
    [TestCase(true, 0, "Punch")]
    [TestCase(true, 1, "Shoot")]
    [TestCase(true, 2, "ForwardSwordAttack")]
    [TestCase(false, -1, "Punch")]
    public void FlyingAttacksUseSelectedWeaponAndReturnToFlight(bool moving, int weaponIndex, string animationName)
    {
        PrepareFlyingAttack(moving, weaponIndex);
        AbstractPlayerState flightState = GetField<AbstractPlayerState>(player, "currentState");
        playerControls.AttackPressed = true;
        flightState.FixedUpdate();
        player.animator.Update(0.2f);

        Assert.That(player.animator.GetCurrentAnimatorStateInfo(0).IsName(animationName), Is.True);
        Assert.That(player.animator.speed, Is.EqualTo(1.5f));
        Assert.That(GetField<AbstractPlayerState>(player, "currentState"), Is.SameAs(flightState));
        Assert.That(player.IsFlying, Is.True);
        Assert.That(player.CurrentDisplacementApplyMode, Is.EqualTo(PlayerController.DisplacementApplyMode.Inertia));
        Assert.That(player.PlayerBody.useGravity, Is.False);
        Assert.That(camera.CurrentState, Is.SameAs(camera.LookAtFlyingCameraState));
        Assert.That(player.FlyingVelocity.magnitude, moving ? Is.GreaterThan(10f) : Is.InRange(0.01f, 9.99f));

        playerControls.AttackPressed = false;
        player.animator.Update(5f);
        flightState.FixedUpdate();
        player.animator.Update(0.2f);
        Assert.That(player.animator.GetCurrentAnimatorStateInfo(0).IsName(moving ? "FlyingMove" : "SwimIdle"), Is.True);
        Assert.That(player.animator.speed, Is.EqualTo(1f));
        Assert.That(player.IsFlying, Is.True);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ChangingFlyingMovementDoesNotRestartShooting(bool initiallyMoving)
    {
        PrepareFlyingAttack(initiallyMoving, 1);
        playerControls.AttackPressed = true;
        GetField<AbstractPlayerState>(player, "currentState").FixedUpdate();
        player.animator.Update(0.2f);
        float attackProgress = player.animator.GetCurrentAnimatorStateInfo(0).normalizedTime;

        playerControls.Move = initiallyMoving ? Vector2.zero : Vector2.up;
        GetField<AbstractPlayerState>(player, "currentState").FixedUpdate();
        player.animator.Update(0.05f);
        Assert.That(player.IsFlyingMoving, Is.EqualTo(!initiallyMoving));
        var animationState = player.animator.GetCurrentAnimatorStateInfo(0);
        Assert.That(animationState.IsName("Shoot"), Is.True);
        Assert.That(animationState.normalizedTime, Is.GreaterThan(attackProgress));

        playerControls.AttackPressed = false;
        player.animator.Update(5f);
        GetField<AbstractPlayerState>(player, "currentState").FixedUpdate();
        player.animator.Update(0.2f);
        Assert.That(player.animator.GetCurrentAnimatorStateInfo(0).IsName(initiallyMoving ? "SwimIdle" : "FlyingMove"), Is.True);
    }

    [Test]
    public void HeldFlyingAttackRepeatsAndUsesTheNewlySelectedWeapon()
    {
        PrepareFlyingAttack(false, 1);
        playerControls.AttackPressed = true;
        player.FlyingIdleState.FixedUpdate();
        player.animator.Update(5f);
        player.FlyingIdleState.FixedUpdate();
        player.animator.Update(0.2f);
        var animationState = player.animator.GetCurrentAnimatorStateInfo(0);
        Assert.That(animationState.IsName("Shoot"), Is.True);
        Assert.That(animationState.normalizedTime, Is.LessThan(1f));

        GetField<TogglerOfGameObjectKeySwitch>(player, "handWeaponSwitch").SwitchActive(0);
        player.animator.Update(5f);
        player.FlyingIdleState.FixedUpdate();
        player.animator.Update(0.2f);
        Assert.That(player.animator.GetCurrentAnimatorStateInfo(0).IsName("Punch"), Is.True);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void FlyingAttackRequiresBattleReady(bool moving)
    {
        PrepareFlyingAttack(moving, 1);
        SetField(player, "BattleReady", false);
        playerControls.AttackPressed = true;
        GetField<AbstractPlayerState>(player, "currentState").FixedUpdate();
        player.animator.Update(0.2f);
        Assert.That(player.animator.GetCurrentAnimatorStateInfo(0).IsName(moving ? "FlyingMove" : "SwimIdle"), Is.True);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void EvadeInterruptsFlyingAttackAndRestoresFlyingPose(bool moving)
    {
        PrepareFlyingAttack(moving, 1);
        playerControls.AttackPressed = true;
        GetField<AbstractPlayerState>(player, "currentState").FixedUpdate();
        player.animator.Update(0.2f);
        playerControls.EvadePressed = true;
        GetField<AbstractPlayerState>(player, "currentState").FixedUpdate();
        player.animator.Update(0.2f);
        Assert.That(player.IsAerialEvading, Is.True);
        Assert.That(player.animator.GetCurrentAnimatorStateInfo(0).IsName("AerialEvade"), Is.True);

        playerControls.EvadePressed = false;
        playerControls.AttackPressed = false;
        for (int i = 0; i < 30; i++)
            GetField<AbstractPlayerState>(player, "currentState").FixedUpdate();
        player.animator.Update(0.2f);
        Assert.That(player.IsAerialEvading, Is.False);
        Assert.That(player.IsFlying, Is.True);
        Assert.That(player.animator.GetCurrentAnimatorStateInfo(0).IsName(moving ? "FlyingMove" : "SwimIdle"), Is.True);
    }

    [Test]
    public void LeavingFlightClearsTheAttackBeforeFlyingAgain()
    {
        PrepareFlyingAttack(false, 1);
        playerControls.AttackPressed = true;
        player.FlyingIdleState.FixedUpdate();
        player.animator.Update(0.2f);
        ToggleFlight();
        Assert.That(player.IsFlying, Is.False);
        playerControls.AttackPressed = false;
        ToggleFlight();
        player.animator.Update(0.2f);
        Assert.That(player.animator.GetCurrentAnimatorStateInfo(0).IsName("SwimIdle"), Is.True);
    }

    [TestCase(false, -1)]
    [TestCase(false, 0)]
    [TestCase(false, 1)]
    [TestCase(false, 2)]
    [TestCase(true, -1)]
    [TestCase(true, 0)]
    [TestCase(true, 1)]
    [TestCase(true, 2)]
    public void GunSelectionBlocksAirHitFromJumpAndFall(bool falling, int weaponIndex)
    {
        PrepareFlyingAttack(false, weaponIndex);
        player.transform.position = new Vector3(1000f, 700f, -900f);
        player.PlayerBody.position = player.transform.position;
        AbstractPlayerState aerialState = falling ? (AbstractPlayerState)player.FallState : player.JumpUpState;
        player.SetState(aerialState);
        playerControls.AttackPressed = true;

        Assert.That(player.IsGunSelected(), Is.EqualTo(weaponIndex == 1));
        aerialState.FixedUpdate();
        Assert.That(GetField<AbstractPlayerState>(player, "currentState"),
            Is.SameAs(weaponIndex == 1 ? aerialState : player.AirHitState));

        if (weaponIndex == 1)
        {
            // Changing weapons must allow the attack during the same jump or fall.
            GetField<TogglerOfGameObjectKeySwitch>(player, "handWeaponSwitch").SwitchActive(0);
            aerialState.FixedUpdate();
            Assert.That(GetField<AbstractPlayerState>(player, "currentState"), Is.SameAs(player.AirHitState));
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void PlayerFindsInactiveWeaponSwitchOnlyWhenReferenceIsMissing(bool alreadyAssigned)
    {
        GameObject candidateObject = Child("Weapon reference player").gameObject;
        candidateObject.AddComponent<Rigidbody>().isKinematic = true;
        candidateObject.AddComponent<CapsuleCollider>();
        var weaponPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player/HandWeaponSwitch.prefab");
        var childWeapons = Object.Instantiate(weaponPrefab, candidateObject.transform);
        childWeapons.SetActive(false);
        var childSwitch = childWeapons.GetComponent<TogglerOfGameObjectKeySwitch>();
        childSwitch.SwitchActive(1);
        var assignedSwitch = Child("Assigned switch").gameObject.AddComponent<TogglerOfGameObjectKeySwitch>();
        var candidate = candidateObject.AddComponent<PlayerController>();
        SetField(candidate, "buttonControls", sourceControls);
        if (alreadyAssigned)
            SetField(candidate, "handWeaponSwitch", assignedSwitch);

        Invoke(candidate, "Awake");
        try
        {
            Assert.That(GetField<TogglerOfGameObjectKeySwitch>(candidate, "handWeaponSwitch"),
                Is.SameAs(alreadyAssigned ? assignedSwitch : childSwitch));
            Assert.That(candidate.IsGunSelected(), Is.EqualTo(!alreadyAssigned));
            childSwitch.SwitchActive(2);
            Assert.That(candidate.IsGunSelected(), Is.False);
        }
        finally
        {
            Object.DestroyImmediate(candidate.ButtonControls);
        }
    }

    private void PrepareFlyingAttack(bool moving, int weaponIndex)
    {
        attackAnimationObject = new GameObject("Flight attack animator");
        player.animator = attackAnimationObject.AddComponent<Animator>();
        player.animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
            "Assets/Animation/DefaultPlayer.controller");
        player.animator.fireEvents = false;
        player.animator.Update(0f);
        SetField(player, "BattleReady", true);

        if (weaponIndex >= 0)
        {
            var weaponSwitch = Child("Weapons").gameObject.AddComponent<TogglerOfGameObjectKeySwitch>();
            typeof(TogglerOfGameObjectSwitch).GetField("gameObjects", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(weaponSwitch, new[] { Child("Empty").gameObject, Child("Gun").gameObject, Child("Sword").gameObject });
            weaponSwitch.SwitchActive(weaponIndex);
            SetField(player, "handWeaponSwitch", weaponSwitch);
        }

        playerControls.Move = moving ? Vector2.up : Vector2.zero;
        player.SetState(moving ? (AbstractPlayerState)player.FlyingMoveState : player.FlyingIdleState);
        player.SetFlyingVelocity(Vector3.forward * 10f);
        player.animator.Update(0.2f);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void FirstPersonCamerasMatchExistingLookControlsWithAForwardHalfMeterOffset(bool flying)
    {
        Quaternion start = Quaternion.Euler(35f, 65f, 0f);
        cameraTransform.SetPositionAndRotation(pivot.position - start * Vector3.forward * 2f, start);
        camera.SetState(flying ? CameraController.CameraStateKind.LookAtFlying : CameraController.CameraStateKind.LookAt);
        sourceControls.Look = new Vector2(25f, -15f);
        camera.Update();
        Quaternion expectedRotation = cameraTransform.rotation;

        cameraTransform.SetPositionAndRotation(pivot.position - start * Vector3.forward * 2f, start);
        camera.SetState(flying ? CameraController.CameraStateKind.FirstPersonFlying : CameraController.CameraStateKind.FirstPerson);
        camera.Update();
        Assert.That(camera.CurrentState, Is.SameAs(flying
            ? (AbstractCameraState)camera.FirstPersonFlyingCameraState : camera.FirstPersonCameraState));
        AssertVector(cameraTransform.forward, expectedRotation * Vector3.forward);
        AssertVector(cameraTransform.up, expectedRotation * Vector3.up);
        AssertVector(cameraTransform.position, pivot.position + cameraTransform.forward * 0.5f);
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    [TestCase(5)]
    [TestCase(6)]
    [TestCase(7)]
    [TestCase(8)]
    public void LocomotionAimingRequiresHeldInputAndAGunAndReleasesOnEitherChange(int stateIndex)
    {
        TogglerOfGameObjectKeySwitch weapons = SetupAimingWeapons(0);
        AbstractPlayerState state = GetAimingState(stateIndex);
        player.SetState(state);
        state.Update();
        Assert.That(player.IsAiming, Is.False);
        playerControls.AimPressed = true;
        state.Update();
        Assert.That(player.IsAiming, Is.False);

        weapons.SwitchActive(1);
        state.Update();
        Assert.That(player.IsAiming, Is.True);
        Assert.That(camera.CurrentState, Is.SameAs(player.IsFlying
            ? (AbstractCameraState)camera.FirstPersonFlyingCameraState : camera.FirstPersonCameraState));

        playerControls.AimPressed = false;
        state.Update();
        Assert.That(camera.CurrentState, Is.SameAs(player.IsFlying
            ? (AbstractCameraState)camera.LookAtFlyingCameraState : camera.LookAtCameraState));
        playerControls.AimPressed = true;
        state.Update();
        Assert.That(player.IsAiming, Is.True);
        weapons.SwitchActive(2);
        state.Update();
        Assert.That(camera.CurrentState, Is.SameAs(player.IsFlying
            ? (AbstractCameraState)camera.LookAtFlyingCameraState : camera.LookAtCameraState));
    }

    [Test]
    public void AimingSurvivesMovementJumpingFallingShootingAndFlightTransitions()
    {
        SetupAimingWeapons(1);
        playerControls.AimPressed = true;
        player.IdleState.Update();
        var groundCamera = new AimingTransitionGroundCamera(camera);
        var flyingCamera = new AimingTransitionFlyingCamera(camera);
        SetField(camera, "<LookAtCameraState>k__BackingField", groundCamera);
        SetField(camera, "<LookAtFlyingCameraState>k__BackingField", flyingCamera);

        foreach (int stateIndex in new[] { 1, 2, 0, 3, 4, 5, 8, 6, 7, 6, 0, 2 })
        {
            player.SetState(GetAimingState(stateIndex));
            if (stateIndex == 8)
            {
                SetField(player, "BattleReady", true);
                playerControls.AttackPressed = true;
                player.SwitchToAttackState();
                Assert.That(player.UpperBodyVisualsController.CurrentState,
                    Is.SameAs(player.UpperBodyVisualsController.ShootTweakState));
                playerControls.AttackPressed = false;
            }
            Assert.That(player.IsAiming, Is.True);
            Assert.That(camera.CurrentState, Is.SameAs(player.IsFlying
                ? (AbstractCameraState)camera.FirstPersonFlyingCameraState : camera.FirstPersonCameraState));
            AssertVector(cameraTransform.position, pivot.position + cameraTransform.forward * 0.5f);
        }

        Assert.That(groundCamera.EnterCount, Is.Zero, "Aiming must not briefly enter the third-person ground camera.");
        Assert.That(flyingCamera.EnterCount, Is.Zero, "Aiming must not briefly enter the third-person flying camera.");
    }

    [Test]
    public void EnteringFirstPersonFlightPreservesTheForwardOffsetWhileSmoothingIsEnabled()
    {
        SetupAimingWeapons(1);
        playerControls.AimPressed = true;
        player.IdleState.Update();
        Vector3 entryPosition = cameraTransform.position;
        SetField(camera, "lookAtSmoothDuration", 1f);
        player.SetState(player.FlyingIdleState);
        AssertVector(cameraTransform.position, entryPosition);
        AssertVector(cameraTransform.position, pivot.position + cameraTransform.forward * 0.5f);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void GroundShootingKeepsAimingInputDrivenAndStopsWhenJumping(bool swapWeapon)
    {
        var weapons = SetupAimingWeapons(1);
        SetField(player, "BattleReady", true);
        playerControls.AttackPressed = true;
        playerControls.AimPressed = true;
        player.SetState(player.RunState);
        player.SwitchToAttackState();
        var upper = player.UpperBodyVisualsController;
        Assert.That(upper.CurrentState, Is.SameAs(upper.ShootTweakState));
        Assert.That(player.IsAiming, Is.True);

        if (swapWeapon)
            weapons.SwitchActive(2);
        else
            playerControls.AimPressed = false;
        player.RunState.Update();
        Invoke(upper, "Update");
        Assert.That(player.IsAiming, Is.False);
        Assert.That(upper.IsAttacking, Is.EqualTo(!swapWeapon));

        weapons.SwitchActive(1);
        playerControls.AimPressed = true;
        player.RunState.Update();
        player.SwitchToAttackState();
        player.SetState(player.JumpUpState);
        Assert.That(upper.IsAttacking, Is.False);
        Assert.That(player.IsAiming, Is.True);
    }

    [Test]
    public void ShootingUpperBodyStillAppliesVerticalCameraPitchToTheAssignedBones()
    {
        SetupAimingWeapons(1);
        SetField(player, "BattleReady", true);
        playerControls.AttackPressed = true;
        Transform bone = Child("Gun aim bone");
        SetField(player, "verticalRotationBones", new[] { bone });
        cameraTransform.rotation = Quaternion.Euler(-30f, 0f, 0f);
        player.SwitchToAttackState();
        player.UpperBodyVisualsController.CurrentState.LateUpdate();
        AssertVector(bone.forward, new Vector3(0f, 0.5f, Mathf.Sqrt(0.75f)));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ReleasingAimInFlightDoesNotRestoreFirstPersonWhenReturningToGround(bool changeWeapon)
    {
        var weapons = SetupAimingWeapons(1);
        playerControls.AimPressed = true;
        player.IdleState.Update();
        ToggleFlight();
        Assert.That(camera.CurrentState, Is.SameAs(camera.FirstPersonFlyingCameraState));
        if (changeWeapon)
            weapons.SwitchActive(2);
        else
            playerControls.AimPressed = false;
        player.FlyingIdleState.Update();
        Assert.That(camera.CurrentState, Is.SameAs(camera.LookAtFlyingCameraState));
        ToggleFlight();
        Assert.That(camera.CurrentState, Is.SameAs(camera.LookAtCameraState));
    }

    [TestCase(0, false)]
    [TestCase(0, true)]
    [TestCase(1, false)]
    [TestCase(1, true)]
    [TestCase(2, false)]
    [TestCase(2, true)]
    public void EvadesStopAimingAndOnlyAllowItAgainAfterExit(int evadeIndex, bool keepHolding)
    {
        SetupAimingWeapons(1);
        playerControls.AimPressed = true;
        player.SetState(evadeIndex == 0 ? (AbstractPlayerState)player.RunState : player.FlyingIdleState);
        Assert.That(player.IsAiming, Is.True);
        var evade = new AbstractPlayerState[]
        {
            player.SlideState, player.AerialEvadeState, player.AerialEvadeDownwardsState
        }[evadeIndex];
        player.SetState(evade);
        Assert.That(player.IsAiming, Is.False);
        Assert.That(camera.CurrentState, Is.SameAs(evadeIndex == 0
            ? (AbstractCameraState)camera.LookAtCameraState
            : evadeIndex == 1 ? camera.LookAtFlyingCameraState : camera.LookAtFlyingNormalizingCameraState));

        for (int i = 0; i < 3; i++)
        {
            playerControls.AimPressed = i % 2 == 0;
            evade.Update();
            player.UpdateAiming();
            evade.FixedUpdate();
            Assert.That(player.IsAiming, Is.False);
        }

        playerControls.AimPressed = keepHolding;
        for (int i = 0; i < 100 && GetField<AbstractPlayerState>(player, "currentState") == evade; i++)
            evade.FixedUpdate();
        Assert.That(GetField<AbstractPlayerState>(player, "currentState"), Is.Not.SameAs(evade));
        Assert.That(player.IsAiming, Is.EqualTo(keepHolding));
        camera.LookAtFlyingNormalizingCameraState.Update(0.5f);
        Assert.That(player.IsAiming, Is.EqualTo(keepHolding));
    }

    [Test]
    public void DownwardEvadeFromFirstPersonLevelsWithoutReversingTheCamera()
    {
        SetupAimingWeapons(1);
        playerControls.AimPressed = true;
        cameraTransform.rotation = Quaternion.Euler(25f, 40f, 0f);
        player.SetState(player.FlyingIdleState);
        Vector3 expectedForward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
        SetField(camera, "lookAtSmoothDuration", 1f);
        player.SetState(player.AerialEvadeDownwardsState);
        camera.LookAtFlyingNormalizingCameraState.Update(0.5f);
        Assert.That(player.IsAiming, Is.False);
        AssertVector(cameraTransform.forward, expectedForward);
        AssertVector(cameraTransform.position, pivot.position - cameraTransform.forward * 2f);
    }

    [Test]
    public void RightMouseAimsWithAGunAndStillBlocksWithOtherWeapons()
    {
        var weapons = SetupAimingWeapons(1);
        SetField(player, "BattleReady", true);
        playerControls.AimPressed = true;
        player.IdleState.Update();
        Assert.That(player.IsAiming, Is.True);
        Assert.That(player.IsBlocking(), Is.False);
        weapons.SwitchActive(2);
        player.IdleState.Update();
        Assert.That(player.IsAiming, Is.False);
        Assert.That(player.IsBlocking(), Is.True);
    }

    private TogglerOfGameObjectKeySwitch SetupAimingWeapons(int selectedIndex)
    {
        var weapons = Child("Aiming weapons").gameObject.AddComponent<TogglerOfGameObjectKeySwitch>();
        typeof(TogglerOfGameObjectSwitch).GetField("gameObjects", BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(weapons, new[] { Child("Empty").gameObject, Child("Gun").gameObject, Child("Sword").gameObject });
        weapons.SwitchActive(selectedIndex);
        SetField(player, "handWeaponSwitch", weapons);
        return weapons;
    }

    private AbstractPlayerState GetAimingState(int index)
    {
        return new AbstractPlayerState[]
        {
            player.IdleState, player.WalkState, player.RunState, player.JumpUpState, player.AerialJumpUpState,
            player.FallState, player.FlyingIdleState, player.FlyingMoveState, player.GroundMeleeMoveState
        }[index];
    }

    private sealed class AimingTransitionGroundCamera : LookAtCameraState
    {
        public int EnterCount;
        public AimingTransitionGroundCamera(CameraController controller) : base(controller) { }
        public override void OnEnter() { EnterCount++; base.OnEnter(); }
    }

    private sealed class AimingTransitionFlyingCamera : LookAtFlyingCameraState
    {
        public int EnterCount;
        public AimingTransitionFlyingCamera(CameraController controller) : base(controller) { }
        public override void OnEnter() { EnterCount++; base.OnEnter(); }
    }

    private AbstractPlayerState GetAerialState(int index)
    {
        return new AbstractPlayerState[]
        {
            player.FlyingIdleState, player.FlyingMoveState, player.AerialEvadeState, player.AerialEvadeDownwardsState
        }[index];
    }

    private void ToggleFlight()
    {
        playerControls.ToggleFlight = true;
        Invoke(player, "UpdateFlightToggle");
    }

    private Transform Child(string name)
    {
        Transform child = new GameObject(name).transform;
        child.SetParent(root.transform);
        return child;
    }

    private static void AssertVector(Vector3 actual, Vector3 expected)
    {
        Assert.That(Vector3.Distance(actual, expected), Is.LessThan(0.0001f));
    }

    private static void SetField(object target, string name, object value)
    {
        target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);
    }

    private static T GetField<T>(object target, string name)
    {
        return (T)target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(target);
    }

    private static void Invoke(object target, string name)
    {
        target.GetType().GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(target, null);
    }
}

public class FlyingTestControls : AbstractUnitControls
{
    public Vector2 Move;
    public Vector2 Look;
    public bool ToggleFlight;
    public bool EvadePressed;
    public bool AttackPressed;
    public bool AimPressed;
    public bool DashPressed;
    public bool DashHeld;
    public bool JumpPressed;
    public override Vector2 GetMove2D() => Move;
    public override Vector3 GetMove3D() => new Vector3(Move.x, 0f, Move.y);
    public override Vector2 GetMouseMove2D() => Look;
    public override bool IsJumpPressed() => JumpPressed;
    public override bool IsInteractButtonPressed() => false;
    public override bool IsCancelButtonPressed() => false;
    public override bool IsPunchButtonPresssed() => AttackPressed;
    public override bool IsBlockButtonPressed() => AimPressed;
    public override bool IsAimButtonPressed() => AimPressed;
    public override bool IsEvadeModifierPressed() => EvadePressed;
    public override bool WasFlyingDashPressed() => DashPressed;
    public override bool IsFlyingDashPressed() => DashHeld;
    public override bool WasRunWalkTogglePressed() => false;
    public override bool WasFlightTogglePressed()
    {
        bool pressed = ToggleFlight;
        ToggleFlight = false;
        return pressed;
    }
}


public class UpperBodyVisualsControllerTests
{
    private GameObject root;
    private PlayerController player;
    private UpperBodyTestControls sourceControls;
    private UpperBodyTestControls controls;
    private TogglerOfGameObjectKeySwitch weapons;
    private Transform direction;
    private BoxCollider floor;

    [SetUp]
    public void SetUp()
    {
        root = new GameObject("Upper body tests");
        root.transform.position = new Vector3(10000f, 10000f, 10000f);
        floor = Child("Floor").AddComponent<BoxCollider>();
        floor.size = new Vector3(20f, 0.5f, 20f);
        floor.transform.localPosition = Vector3.down * 0.25f;
        floor.transform.SetParent(null, true); // Ground must not be filtered out as part of the player's hierarchy.
        GameObject bodyObject = Child("Body");
        bodyObject.transform.localPosition = Vector3.up * 1.02f;
        var body = bodyObject.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
        var collider = bodyObject.AddComponent<CapsuleCollider>();
        collider.height = 2f;
        collider.radius = 0.5f;

        GameObject playerObject = Child("Controller");
        playerObject.SetActive(false);
        player = playerObject.AddComponent<PlayerController>();
        sourceControls = ScriptableObject.CreateInstance<UpperBodyTestControls>();
        var pointer = Child("Direction").AddComponent<UpperBodyTestDirection>();
        direction = pointer.transform;
        SetField(player, "buttonControls", sourceControls);
        SetField(player, "directionPointer", pointer);
        SetField(player, "playerBody", body);
        SetField(player, "playerCollider", collider);
        Invoke(player, "Awake");
        controls = (UpperBodyTestControls)player.ButtonControls;
        Invoke(player.VisualsRotationController, "Awake");
        SetField(player, "isRunMode", true);
        SetField(player, "BattleReady", true);

        weapons = Child("Weapons").AddComponent<TogglerOfGameObjectKeySwitch>();
        typeof(TogglerOfGameObjectSwitch).GetField("gameObjects", BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(weapons, new[] { Child("Unarmed"), Child("Gun"), Child("Sword") });
        weapons.SwitchActive(2);
        SetField(player, "handWeaponSwitch", weapons);

        player.animator = Child("Animator").AddComponent<Animator>();
        player.animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
            "Assets/Animation/DefaultPlayer.controller");
        player.animator.fireEvents = false;
        player.animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        player.animator.Update(0f);
        Physics.SyncTransforms();
        Assert.That(player.CheckIsGrounded(), Is.True);
        controls.Move = Vector2.up;
        player.SetState(player.RunState);
        player.animator.Update(0.2f);
    }

    [TearDown]
    public void TearDown()
    {
        if (floor != null)
            Object.DestroyImmediate(floor.gameObject);
        Object.DestroyImmediate(root);
        Object.DestroyImmediate(controls);
        Object.DestroyImmediate(sourceControls);
    }

    [TestCase(0, 0)]
    [TestCase(0, 1)]
    [TestCase(0, 2)]
    [TestCase(1, 0)]
    [TestCase(1, 1)]
    [TestCase(1, 2)]
    [TestCase(2, 0)]
    [TestCase(2, 1)]
    [TestCase(2, 2)]
    public void GroundAttacksChooseTheWeaponAndKeepLocomotionIndependent(int weapon, int locomotion)
    {
        weapons.SwitchActive(weapon);
        SetField(player, "isRunMode", locomotion != 2);
        controls.Move = locomotion == 0 ? Vector2.zero : Vector2.up;
        player.SetState(locomotion == 0 ? (AbstractPlayerState)player.IdleState
            : locomotion == 1 ? player.RunState : player.WalkState);
        controls.Attack = true;
        Tick();
        AbstractUpperBodyState expectedAttack = weapon == 0 ? (AbstractUpperBodyState)Upper.PunchUpperBodyState
            : weapon == 1 ? Upper.ShootTweakState : Upper.StabUpperBodyState;
        Assert.That(Upper.CurrentState, Is.SameAs(expectedAttack));
        AbstractPlayerState expectedMovement = locomotion == 0 ? (AbstractPlayerState)player.IdleState
            : weapon != 1 ? player.GroundMeleeMoveState : player.RunOrWalkState;
        Assert.That(CurrentState, Is.SameAs(expectedMovement));
        player.animator.Update(0.2f);
        string clip = weapon == 0 ? "Punch" : weapon == 1 ? "Shoot" : "ForwardSwordAttack";
        Assert.That(player.animator.GetCurrentAnimatorStateInfo(UpperLayer).IsName(clip), Is.True);
        string baseClip = locomotion == 0 ? "Idle" : weapon == 1 && locomotion == 2 ? "Walk" : "Run";
        Assert.That(player.animator.GetCurrentAnimatorStateInfo(0).IsName(baseClip), Is.True);
        Assert.That(player.animator.GetLayerWeight(UpperLayer), Is.EqualTo(1f));
        Assert.That(player.animator.speed, Is.EqualTo(1f));
        if (locomotion != 0)
        {
            Tick();
            Assert.That(PlanarDisplacement.magnitude / Time.fixedDeltaTime,
                Is.EqualTo(weapon == 1 && locomotion == 1 ? 10f : 5f).Within(0.0001f));
        }
    }

    [Test]
    public void AttackingStillRequiresBattleReady()
    {
        SetField(player, "BattleReady", false);
        controls.Attack = true;
        Tick();
        Assert.That(CurrentState, Is.SameAs(player.RunState));
        Assert.That(Upper.IsAttacking, Is.False);
        Assert.That(PlanarDisplacement.magnitude / Time.fixedDeltaTime, Is.EqualTo(10f).Within(0.0001f));
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    public void StoppingAndResumingMovementDoesNotRestartTheAttackOrItsTimer(int weapon)
    {
        SetDuration(weapon, Time.fixedDeltaTime * 4.5f);
        BeginAttack(weapon);
        AbstractUpperBodyState attack = Upper.CurrentState;
        player.animator.Update(0.2f);
        float progress = player.animator.GetCurrentAnimatorStateInfo(UpperLayer).normalizedTime;
        controls.Move = Vector2.zero;
        Tick();
        Assert.That(CurrentState, Is.SameAs(player.IdleState));
        Assert.That(Upper.CurrentState, Is.SameAs(attack));
        player.animator.Update(0.1f);
        controls.Move = Vector2.right;
        Tick();
        Assert.That(CurrentState, Is.SameAs(weapon == 1 ? (AbstractPlayerState)player.RunState : player.GroundMeleeMoveState));
        Assert.That(Upper.CurrentState, Is.SameAs(attack));
        player.animator.Update(0.1f);
        Assert.That(player.animator.GetCurrentAnimatorStateInfo(UpperLayer).normalizedTime, Is.GreaterThan(progress));
        Tick();
        Assert.That(Upper.IsAttacking, Is.True);
        Tick();
        Assert.That(Upper.CurrentState, Is.SameAs(Upper.DefaultUpperBodyState));
        AssertUpperLayerDisabled();
        Tick();
        Assert.That(CurrentState, Is.SameAs(player.RunState));
    }

    [Test]
    public void MeleeMovementSteersRelativeToCameraAtSpeedFive()
    {
        BeginAttack(2);
        direction.rotation = Quaternion.Euler(0f, 90f, 0f);
        Tick();
        Assert.That(Vector3.Distance(PlanarDisplacement, Vector3.right * (5f * Time.fixedDeltaTime)), Is.LessThan(0.0001f));
        controls.Move = Vector2.right;
        Tick();
        Assert.That(Vector3.Distance(PlanarDisplacement, Vector3.back * (5f * Time.fixedDeltaTime)), Is.LessThan(0.0001f));
    }

    [TestCase(false, false)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public void MeleeCompletionReturnsToTheSelectedRunOrWalkModeOrIdle(bool running, bool moving)
    {
        SetField(player, "isRunMode", running);
        SetDuration(2, Time.fixedDeltaTime * 2.5f);
        BeginAttack(2);
        controls.Move = moving ? Vector2.up : Vector2.zero;
        Tick();
        Assert.That(Upper.IsAttacking, Is.True);
        Tick();
        Assert.That(Upper.IsAttacking, Is.False);
        AssertUpperLayerDisabled();
        Tick();
        Assert.That(CurrentState, Is.SameAs(moving ? player.RunOrWalkState : player.IdleState));
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    public void TimersCompleteEvenWithoutAnAnimator(int weapon)
    {
        player.animator = null;
        SetDuration(weapon, Time.fixedDeltaTime * 2.5f);
        BeginAttack(weapon);
        Tick();
        Assert.That(Upper.IsAttacking, Is.True);
        Tick();
        Assert.That(Upper.IsAttacking, Is.False);
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    public void HeldInputDoesNotResetTheTimerAndStartsAnotherAttackAfterCompletion(int weapon)
    {
        SetDuration(weapon, Time.fixedDeltaTime * 2.5f);
        BeginAttack(weapon);
        controls.Attack = true;
        Tick();
        Assert.That(Upper.IsAttacking, Is.True);
        Tick();
        Assert.That(Upper.IsAttacking, Is.False);
        Tick();
        Assert.That(Upper.IsAttacking, Is.True);
    }

    [Test]
    public void SwordFollowUpRunsOnUpperLayerAndSurvivesMovementChanges()
    {
        BeginAttack(2);
        for (int i = 0; i < Mathf.CeilToInt(0.76f / Time.fixedDeltaTime); i++)
            Tick();
        controls.Move = Vector2.zero;
        controls.Attack = true;
        Tick();
        controls.Attack = false;
        player.animator.Update(0f);
        Assert.That(CurrentState, Is.SameAs(player.IdleState));
        Assert.That(Upper.CurrentState, Is.SameAs(Upper.StabUpperBodyState));
        Assert.That(player.animator.GetCurrentAnimatorStateInfo(UpperLayer).IsName("Stab2"), Is.True);
        controls.Move = Vector2.up;
        Tick();
        Assert.That(CurrentState, Is.SameAs(player.GroundMeleeMoveState));
        for (int i = 0; i < Mathf.CeilToInt(Upper.StabFollowUpDuration / Time.fixedDeltaTime) + 1; i++)
            Tick();
        Assert.That(Upper.IsAttacking, Is.False);
        AssertUpperLayerDisabled();
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    [TestCase(5)]
    public void LeavingGroundLocomotionCancelsTheAttackImmediatelyAndRejectsNewRequests(int target)
    {
        BeginAttack(2);
        var states = new AbstractPlayerState[]
        {
            player.JumpUpState, player.FallState, player.BlockingState,
            player.SlideState, player.SwimIdleState, player.FlyingIdleState
        };
        player.SetState(states[target]);
        Assert.That(Upper.IsAttacking, Is.False);
        AssertUpperLayerDisabled();
        controls.Attack = true;
        player.SwitchToAttackState();
        Assert.That(Upper.IsAttacking, Is.False);
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    public void MeleeMovementKeepsRunStateJumpEvadeBlockFallAndFlightControls(int input)
    {
        BeginAttack(2);
        switch (input)
        {
            case 0: controls.Jump = true; break;
            case 1: controls.Evade = true; break;
            case 2: controls.Block = true; break;
            case 3: floor.enabled = false; Physics.SyncTransforms(); break;
            case 4: controls.Flight = true; Invoke(player, "UpdateFlightToggle"); break;
        }
        if (input != 4)
            Tick();
        var expected = new AbstractPlayerState[]
        {
            player.JumpUpState, player.SlideState, player.BlockingState, player.FallState, player.FlyingIdleState
        };
        Assert.That(CurrentState, Is.SameAs(expected[input]));
        Assert.That(Upper.IsAttacking, Is.False);
        AssertUpperLayerDisabled();
        if (input == 2)
        {
            Tick();
            Assert.That(PlanarDisplacement, Is.EqualTo(Vector3.zero));
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void SwappingFromSwordToGunCancelsMeleeAndRestoresRunning(bool keepAttacking)
    {
        BeginAttack(2);
        weapons.SwitchActive(1);
        controls.Attack = keepAttacking;
        Invoke(Upper, "Update");
        Assert.That(Upper.IsAttacking, Is.False);
        Tick();
        Assert.That(CurrentState, Is.SameAs(player.RunState));
        Assert.That(Upper.CurrentState, Is.SameAs(keepAttacking
            ? (AbstractUpperBodyState)Upper.ShootTweakState : Upper.DefaultUpperBodyState));
        Tick();
        Assert.That(PlanarDisplacement.magnitude / Time.fixedDeltaTime, Is.EqualTo(10f).Within(0.0001f));
    }

    [Test]
    public void DisablingTheUpperControllerRestoresTheLayerAndRejectsAttackRequests()
    {
        BeginAttack(2);
        Upper.enabled = false;
        // The test Player is inactive, so explicitly deliver Unity's lifecycle callback.
        Invoke(Upper, "OnDisable");
        Assert.That(Upper.IsAttacking, Is.False);
        AssertUpperLayerDisabled();
        controls.Attack = true;
        Tick();
        Assert.That(Upper.IsAttacking, Is.False);
        Assert.That(CurrentState, Is.SameAs(player.RunState));
    }

    [Test]
    public void PrefabAndAnimatorHaveTheUpperBodyControllerAndMaskedAttackStates()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player/Player.prefab");
        var prefabPlayer = prefab.GetComponent<PlayerController>();
        var prefabUpper = prefab.GetComponent<UpperBodyVisualsController>();
        Assert.That(prefabUpper, Is.Not.Null);
        Assert.That(prefabPlayer.UpperBodyVisualsController, Is.SameAs(prefabUpper));
        Assert.That(prefabUpper.PlayerController, Is.SameAs(prefabPlayer));
        var animatorController = (UnityEditor.Animations.AnimatorController)player.animator.runtimeAnimatorController;
        var layer = animatorController.layers[UpperLayer];
        Assert.That(layer.avatarMask, Is.Not.Null);
        Assert.That(layer.defaultWeight, Is.Zero);
        Assert.That(layer.blendingMode, Is.EqualTo(UnityEditor.Animations.AnimatorLayerBlendingMode.Override));
        foreach (string name in new[] { "Punch", "ForwardSwordAttack", "Stab2", "Shoot" })
        {
            var state = System.Array.Find(layer.stateMachine.states, child => child.state.name == name).state;
            Assert.That(state, Is.Not.Null, name);
            Assert.That(state.motion, Is.Not.Null, name);
            Assert.That(state.speed, Is.EqualTo(name == "ForwardSwordAttack" ? 1f : 1.5f), name);
        }
    }

    private UpperBodyVisualsController Upper => player.UpperBodyVisualsController;
    private int UpperLayer => player.animator.GetLayerIndex("UpperBodyLayer");

    private void SetDuration(int weapon, float seconds) =>
        SetField(Upper, weapon == 0 ? "punchDuration" : weapon == 1 ? "shootDuration" : "stabDuration", seconds);

    private void BeginAttack(int weapon)
    {
        weapons.SwitchActive(weapon);
        controls.Attack = true;
        Tick();
        controls.Attack = false;
        Assert.That(Upper.IsAttacking, Is.True);
    }

    private void Tick()
    {
        SetField(player, "localVelocity", Vector3.zero);
        CurrentState.FixedUpdate();
        Upper.FixedUpdateController();
    }

    private AbstractPlayerState CurrentState => GetField<AbstractPlayerState>(player, "currentState");
    private Vector3 PlanarDisplacement => Vector3.ProjectOnPlane(GetField<Vector3>(player, "localVelocity"), Vector3.up);

    private void AssertUpperLayerDisabled() =>
        Assert.That(player.animator.GetLayerWeight(player.animator.GetLayerIndex("UpperBodyLayer")), Is.EqualTo(0f));

    private GameObject Child(string name)
    {
        var child = new GameObject(name);
        child.transform.SetParent(root.transform, false);
        return child;
    }

    private static void SetField(object target, string name, object value) =>
        target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);

    private static T GetField<T>(object target, string name) =>
        (T)target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(target);

    private static void Invoke(object target, string name) =>
        target.GetType().GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(target, null);
}

public class UpperBodyTestDirection : DirectionPointer
{
    public override Transform GetDirection() => transform;
}

public class UpperBodyTestControls : AbstractUnitControls
{
    public Vector2 Move;
    public bool Attack;
    public bool Block;
    public bool Jump;
    public bool Evade;
    public bool Flight;
    public override Vector2 GetMove2D() => Move;
    public override Vector3 GetMove3D() => new Vector3(Move.x, 0f, Move.y);
    public override Vector2 GetMouseMove2D() => Vector2.zero;
    public override bool IsJumpPressed() => Jump;
    public override bool IsInteractButtonPressed() => false;
    public override bool IsCancelButtonPressed() => false;
    public override bool IsPunchButtonPresssed() => Attack;
    public override bool IsBlockButtonPressed() => Block;
    public override bool IsEvadeModifierPressed() => Evade;
    public override bool WasRunWalkTogglePressed() => false;
    public override bool WasFlightTogglePressed() => Flight;
}
