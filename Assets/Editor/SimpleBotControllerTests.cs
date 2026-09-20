using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public class SimpleBotControllerTests
{
    private GameObject root;
    private GameObject target;
    private PlayerController bot;
    private SimpleBotController sourceControls;
    private SimpleBotController controls;
    private TogglerOfGameObjectKeySwitch weapons;
    private TargetSelector selector;
    private PlayerVisualsRotationController visuals;
    private LookAtPointer pointer;

    [SetUp]
    public void SetUp()
    {
        root = new GameObject("Debug AI tests");
        root.SetActive(false);
        root.transform.position = new Vector3(1000f, 700f, -900f);
        target = new GameObject("Debug AI test target");
        target.transform.position = root.transform.position + Vector3.forward * 10f;
        var botObject = Child("Bot", root.transform);
        botObject.AddComponent<Rigidbody>().isKinematic = true;
        botObject.AddComponent<CapsuleCollider>();
        visuals = botObject.AddComponent<PlayerVisualsRotationController>();
        bot = botObject.AddComponent<PlayerController>();
        bot.visualsPivot = Child("Visuals", bot.transform).transform;
        var weaponPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player/HandWeaponSwitch.prefab");
        weapons = Object.Instantiate(weaponPrefab, bot.visualsPivot).GetComponent<TogglerOfGameObjectKeySwitch>();
        selector = Child("TargetSelector", bot.transform).AddComponent<TargetSelector>();
        SetField(selector, "rotationController", visuals);
        SetField(selector, "highlightSelection", false);
        pointer = Child("Direction", root.transform).AddComponent<LookAtPointer>();
        SetField(pointer, "origin", bot.transform);
        SetField(pointer, "targetSelector", selector);
        sourceControls = ScriptableObject.CreateInstance<SimpleBotController>();
        SetField(sourceControls, "targetObjectName", target.name);
        SetField(bot, "buttonControls", sourceControls);
        SetField(bot, "directionPointer", pointer);
        SetField(bot, "visualsRotationController", visuals);
        SetField(bot, "BattleReady", true);
        Invoke(bot, "Awake");
        Invoke(visuals, "Awake");
        controls = (SimpleBotController)bot.ButtonControls;
        bot.SetState(bot.IdleState);
        controls.UpdateControls(0f);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(root);
        Object.DestroyImmediate(target);
        Object.DestroyImmediate(controls);
        Object.DestroyImmediate(sourceControls);
    }

    [TestCase(-25f, false)]
    [TestCase(19f, false)]
    [TestCase(20f, true)]
    [TestCase(25f, true)]
    public void FlightThresholdUsesGlobalHeightAndDoesNotToggleBackOut(float height, bool expected)
    {
        bot.transform.rotation = Quaternion.Euler(80f, 35f, 70f);
        target.transform.position = bot.transform.position + new Vector3(100f, height, 0f);
        Assert.That(controls.WasFlightTogglePressed(), Is.EqualTo(expected));
        Invoke(bot, "UpdateFlightToggle");
        Assert.That(bot.IsFlying, Is.EqualTo(expected));
        if (expected)
        {
            Assert.That(bot.IsGunSelected(), Is.True);
            Assert.That(controls.IsPunchButtonPresssed(), Is.True);
            Assert.That(controls.WasFlightTogglePressed(), Is.False);
            target.transform.position = bot.transform.position - Vector3.up * 30f;
            Invoke(bot, "UpdateFlightToggle");
            Assert.That(bot.IsFlying, Is.True);
        }
    }

    [Test]
    public void GunFiresForThreeSecondsThenRestsAndRestartsWithoutPollingAdvancingTheClock()
    {
        Assert.That(bot.IsGunSelected(), Is.True);
        Assert.That(controls.IsPunchButtonPresssed(), Is.True);
        controls.UpdateControls(3f);
        for (int i = 0; i < 100; i++)
            Assert.That(controls.IsPunchButtonPresssed(), Is.False);
        controls.UpdateControls(3f);
        Assert.That(controls.IsPunchButtonPresssed(), Is.True);
    }

    [Test]
    public void EnteringFlightRestartsFiringEvenDuringTheRestPeriod()
    {
        controls.UpdateControls(3f);
        Assert.That(controls.IsPunchButtonPresssed(), Is.False);
        target.transform.position = bot.transform.position + Vector3.up * 21f;
        Invoke(bot, "UpdateFlightToggle");
        Assert.That(bot.IsFlying, Is.True);
        Assert.That(controls.IsPunchButtonPresssed(), Is.True);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void DistanceSelectsSwordInMeleeRangeAndGunOutsideIt(bool flying)
    {
        if (flying)
            bot.SetState(bot.FlyingIdleState);
        target.transform.position = bot.transform.position + Vector3.forward * 1.5f;
        controls.UpdateControls(0f);
        Assert.That(weapons.GetActiveIndex(), Is.EqualTo(2));
        Assert.That(controls.IsPunchButtonPresssed(), Is.True);
        target.transform.position += Vector3.forward * 0.1f;
        controls.UpdateControls(0f);
        Assert.That(bot.IsGunSelected(), Is.True);
        Assert.That(controls.IsPunchButtonPresssed(), Is.True);
    }

    [Test]
    public void BotKeepsThePlayerSelectedAndReacquiresAfterSelectionIsCleared()
    {
        Assert.That(selector.Selected, Is.SameAs(target));
        Assert.That(visuals.target, Is.SameAs(target));
        Assert.That(bot.IsTargeted, Is.True);
        selector.SetSelectedTarget(null);
        controls.UpdateControls(0f);
        Assert.That(selector.Selected, Is.SameAs(target));
        Assert.That(visuals.target, Is.SameAs(target));
        Object.DestroyImmediate(target);
        controls.UpdateControls(0f);
        Assert.That(controls.IsPunchButtonPresssed(), Is.False);
        Assert.That(controls.WasFlightTogglePressed(), Is.False);
        Assert.That(controls.GetMove2D(), Is.EqualTo(Vector2.zero));
    }

    [TestCase(30f)]
    [TestCase(-30f)]
    public void FlightDirectionAndFacingTrackHeightWithoutTiltingPhysicsRoot(float height)
    {
        target.transform.position = bot.transform.position + new Vector3(10f, height, 15f);
        Vector3 flat = target.transform.position - bot.transform.position;
        flat.y = 0f;
        AssertVector(pointer.GetDirection().forward, flat.normalized);
        Quaternion rootRotation = bot.transform.rotation;
        bot.SetState(bot.FlyingIdleState);
        SetField(visuals, "rotationSpeed", 1f / Time.fixedDeltaTime);
        visuals.FixedUpdateController();
        bot.FlyingIdleState.FixedUpdate();
        Vector3 direction = (target.transform.position - bot.transform.position).normalized;
        Assert.That(bot.IsFlyingMoving, Is.True);
        AssertVector(bot.visualsPivot.forward, direction);
        AssertVector(bot.FlyingVelocity.normalized, direction);
        Assert.That(Quaternion.Angle(rootRotation, bot.transform.rotation), Is.LessThan(0.001f));
    }

    [Test]
    public void BotInputIgnoresHumanWeaponSelectionTargetToggleAndTrigger()
    {
        var fixtureType = Assembly.Load("Unity.InputSystem.TestFramework").GetType("UnityEngine.InputSystem.InputTestFixture");
        object fixture = System.Activator.CreateInstance(fixtureType);
        fixtureType.GetMethod("Setup").Invoke(fixture, null);
        try
        {
            Mouse mouse = InputSystem.AddDevice<Mouse>();
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            SetField(weapons, "keys", new[] { Key.Digit1, Key.Digit2, Key.Digit3 });
            var spawner = weapons.GetComponentInChildren<SpawnOnMouseClickTimerLoop>(true);
            SetField(spawner, "useUnitControls", true);
            Invoke(spawner, "Awake");
            InputSystem.QueueStateEvent(mouse, new MouseState());
            InputSystem.Update();
            Assert.That((bool)Invoke(spawner, "IsFireRequested"), Is.True);

            controls.UpdateControls(3f);
            InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Left).WithButton(MouseButton.Middle));
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Digit3));
            InputSystem.Update();
            Invoke(selector, "Update");
            Invoke(weapons, "Update");
            Assert.That(selector.Selected, Is.SameAs(target));
            Assert.That(bot.IsGunSelected(), Is.True);
            Assert.That((bool)Invoke(spawner, "IsFireRequested"), Is.False);

            // The existing player-input mode still responds to the mouse.
            SetField(spawner, "useUnitControls", false);
            Assert.That((bool)Invoke(spawner, "IsFireRequested"), Is.True);
        }
        finally
        {
            fixtureType.GetMethod("TearDown").Invoke(fixture, null);
        }
    }

    [Test]
    public void BotBulletSpawnsTowardItsSelectedPlayerWithNoCameraRequired()
    {
        var aim = weapons.GetComponentInChildren<LookOnTargetOrMiddle>(true);
        var spawner = aim.GetComponent<SpawnOnMouseClickTimerLoop>();
        SetField(aim, "targetSelector", selector);
        Invoke(aim, "Awake");
        Invoke(aim, "OnEnable");
        GameObject projectile = new GameObject("Debug AI projectile");
        GameObject spawned = null;
        try
        {
            typeof(SpawnTimerLoop).GetField("prefab", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(spawner, projectile);
            target.transform.position = aim.transform.position + new Vector3(-10f, 30f, 20f);
            typeof(SpawnTimerLoop).GetMethod("Spawn", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(spawner, null);
            spawned = GameObject.Find(projectile.name + "(Clone)");
            Assert.That(spawned, Is.Not.Null);
            AssertVector(spawned.transform.forward, (target.transform.position - spawned.transform.position).normalized);
        }
        finally
        {
            Object.DestroyImmediate(spawned);
            Object.DestroyImmediate(projectile);
        }
    }

    [Test]
    public void Intro8BotHasMatchingPlayerWeaponPlacementAndIndependentTargetAndFiring()
    {
        var scene = EditorSceneManager.OpenPreviewScene("Assets/Scenes/Intro 8/Intro 8.unity");
        try
        {
            var players = scene.GetRootGameObjects().SelectMany(obj => obj.GetComponentsInChildren<PlayerController>(true));
            var enemy = players.Single(player => player.name == "Bot");
            var human = players.Single(player => player.name == "Player");
            var enemyWeapons = enemy.GetComponentInChildren<TogglerOfGameObjectKeySwitch>(true);
            var humanWeapons = human.GetComponentInChildren<TogglerOfGameObjectKeySwitch>(true);
            Assert.That(enemyWeapons, Is.Not.Null);
            Assert.That(enemyWeapons.transform.IsChildOf(enemy.animator.transform), Is.True);
            Assert.That(enemyWeapons.transform.parent.name, Is.EqualTo(humanWeapons.transform.parent.name));
            Assert.That(new SerializedObject(enemy).FindProperty("handWeaponSwitch").objectReferenceValue, Is.SameAs(enemyWeapons));
            foreach (Transform humanPart in humanWeapons.GetComponentsInChildren<Transform>(true))
            {
                string path = AnimationUtility.CalculateTransformPath(humanPart, humanWeapons.transform);
                Transform enemyPart = string.IsNullOrEmpty(path) ? enemyWeapons.transform : enemyWeapons.transform.Find(path);
                Assert.That(enemyPart, Is.Not.Null, path);
                AssertVector(enemyPart.localPosition, humanPart.localPosition);
                AssertVector(enemyPart.localScale, humanPart.localScale);
                Assert.That(Quaternion.Angle(enemyPart.localRotation, humanPart.localRotation), Is.LessThan(0.01f), path);
            }
            var enemySelector = enemy.GetComponentInChildren<TargetSelector>(true);
            Assert.That(enemySelector.Selected, Is.SameAs(human.gameObject));
            Assert.That(enemySelector.UseMouseInput, Is.False);
            Assert.That(enemyWeapons.UseKeyboardInput, Is.False);
            Assert.That(enemy.GetComponent<PlayerVisualsRotationController>().target, Is.SameAs(human.gameObject));
            var aim = enemyWeapons.GetComponentInChildren<LookOnTargetOrMiddle>(true);
            Assert.That(new SerializedObject(aim).FindProperty("targetSelector").objectReferenceValue, Is.SameAs(enemySelector));
            Assert.That(new SerializedObject(aim.GetComponent<SpawnOnMouseClickTimerLoop>()).FindProperty("useUnitControls").boolValue, Is.True);
            var humanSpawner = humanWeapons.GetComponentInChildren<SpawnOnMouseClickTimerLoop>(true);
            Assert.That(new SerializedObject(humanSpawner).FindProperty("useUnitControls").boolValue, Is.False);
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }

    private static GameObject Child(string name, Transform parent)
    {
        var child = new GameObject(name);
        child.transform.SetParent(parent, false);
        return child;
    }

    private static void SetField(object target, string name, object value)
    {
        target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    }

    private static object Invoke(object target, string name)
    {
        return target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);
    }

    private static void AssertVector(Vector3 actual, Vector3 expected)
    {
        Assert.That(Vector3.Distance(actual, expected), Is.LessThan(0.001f));
    }
}
