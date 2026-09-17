using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class LookOnTargetOrMiddleTests
{
    private GameObject root;
    private Camera camera;
    private TargetSelector selector;
    private LookOnTargetOrMiddle aim;
    private SpawnTimerLoop spawner;
    private Transform weapon;
    private GameObject projectile;
    private GameObject spawnedProjectile;

    [SetUp]
    public void SetUp()
    {
        root = new GameObject("Projectile aim tests");
        root.transform.position = new Vector3(1000f, 700f, -900f);
        camera = Child("Aim camera").gameObject.AddComponent<Camera>();
        camera.transform.rotation = Quaternion.Euler(-25f, 35f, 20f);
        selector = Child("Target selector").gameObject.AddComponent<TargetSelector>();
        SetField(selector, "cameraTransform", camera.transform);
        weapon = Child("Weapon");
        weapon.position = camera.transform.position + camera.transform.right * 2f
            - camera.transform.up + camera.transform.forward * 3f;
        weapon.rotation = Quaternion.Euler(75f, 100f, -40f);
        Transform muzzle = new GameObject("BulletSpawnPivot").transform;
        muzzle.SetParent(weapon, false);
        spawner = muzzle.gameObject.AddComponent<SpawnTimerLoop>();
        aim = muzzle.gameObject.AddComponent<LookOnTargetOrMiddle>();
        SetField(aim, "targetSelector", selector);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(spawnedProjectile);
        Object.DestroyImmediate(projectile);
        Object.DestroyImmediate(root);
    }

    [TestCase(8f)]
    [TestCase(-8f)]
    public void SelectedTargetOverridesCameraAimIncludingVerticalAngle(float height)
    {
        Transform target = Child("Target");
        target.position = aim.transform.position + new Vector3(-12f, height, -5f);
        SetField(selector, "selected", target.gameObject);
        Vector3 localPosition = aim.transform.localPosition;
        Quaternion weaponRotation = weapon.rotation;

        aim.UpdateAim();

        AssertForward(target.position);
        Assert.That(aim.transform.localPosition, Is.EqualTo(localPosition));
        Assert.That(Quaternion.Angle(weapon.rotation, weaponRotation), Is.LessThan(0.01f));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void FreeAimConvergesOnScreenCenterSurfaceFromAnOffsetMuzzle(bool orthographic)
    {
        camera.orthographic = orthographic;
        camera.rect = new Rect(0.2f, 0.1f, 0.5f, 0.7f);
        Transform surface = Child("Center surface");
        surface.position = camera.transform.position + camera.transform.forward * 20f;
        surface.rotation = camera.transform.rotation;
        surface.gameObject.AddComponent<BoxCollider>().size = Vector3.one * 2f;
        Physics.SyncTransforms();

        aim.UpdateAim();

        // Use the viewport's actual center ray, including its camera-rect projection.
        Ray centerRay = camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        var frontFace = new Plane(camera.transform.forward, surface.position - camera.transform.forward);
        Assert.That(frontFace.Raycast(centerRay, out float distance), Is.True);
        AssertForward(centerRay.GetPoint(distance));
    }

    [Test]
    public void FreeAimUsesDistantCenterRayWhenNothingIsHit()
    {
        aim.UpdateAim();
        AssertForward(camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f)).GetPoint(1000f));
    }

    [Test]
    public void ClearingOrDestroyingTargetImmediatelyRestoresCameraAim()
    {
        Transform target = Child("Target");
        target.position = aim.transform.position + Vector3.up * 10f;
        SetField(selector, "selected", target.gameObject);
        aim.UpdateAim();
        AssertForward(target.position);

        SetField(selector, "selected", null);
        aim.UpdateAim();
        AssertForward(camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f)).GetPoint(1000f));

        SetField(selector, "selected", target.gameObject);
        aim.UpdateAim();
        Object.DestroyImmediate(target.gameObject);
        aim.UpdateAim();
        AssertForward(camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f)).GetPoint(1000f));
    }

    [Test]
    public void CameraReferenceChangesAreUsedOnTheNextAimUpdate()
    {
        aim.UpdateAim();
        Camera otherCamera = Child("Other camera").gameObject.AddComponent<Camera>();
        otherCamera.transform.rotation = Quaternion.Euler(60f, -75f, -25f);
        SetField(selector, "cameraTransform", otherCamera.transform);

        aim.UpdateAim();

        AssertForward(otherCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f)).GetPoint(1000f));
    }

    [Test]
    public void FreeAimIgnoresPlayerCollidersAndTriggersAndChoosesNearestSurface()
    {
        Transform owner = Child("Player collider");
        owner.position = camera.transform.position + camera.transform.forward * 5f;
        owner.gameObject.AddComponent<SphereCollider>();
        SetField(aim, "playerRoot", owner);
        Transform trigger = Child("Trigger");
        trigger.position = camera.transform.position + camera.transform.forward * 8f;
        trigger.gameObject.AddComponent<SphereCollider>().isTrigger = true;
        foreach (float distance in new[] { 30f, 20f })
        {
            Transform surface = Child("Surface");
            surface.position = camera.transform.position + camera.transform.forward * distance;
            surface.rotation = camera.transform.rotation;
            surface.gameObject.AddComponent<BoxCollider>().size = Vector3.one * 2f;
        }
        Physics.SyncTransforms();

        aim.UpdateAim();

        AssertForward(camera.transform.position + camera.transform.forward * 19f);
    }

    [Test]
    public void TargetAtMuzzlePositionKeepsAValidUnchangedRotation()
    {
        Transform target = Child("Coincident target");
        target.position = aim.transform.position;
        SetField(selector, "selected", target.gameObject);
        Quaternion before = aim.transform.rotation;

        aim.UpdateAim();

        Assert.That(Quaternion.Angle(aim.transform.rotation, before), Is.LessThan(0.01f));
    }

    [Test]
    public void BulletUsesFreshAimEvenIfWeaponOrTargetMovedAfterLateUpdate()
    {
        // Edit Mode tests invoke runtime lifecycle callbacks explicitly.
        typeof(LookOnTargetOrMiddle).GetMethod("OnEnable", BindingFlags.NonPublic | BindingFlags.Instance)
            .Invoke(aim, null);
        Transform target = Child("Target");
        target.position = root.transform.position + new Vector3(10f, 15f, 30f);
        SetField(selector, "selected", target.gameObject);
        aim.UpdateAim();
        weapon.rotation = Quaternion.Euler(-40f, 180f, 90f);
        target.position += new Vector3(-8f, -12f, 4f);
        projectile = new GameObject("Projectile aim test source");
        SetField(spawner, "prefab", projectile);

        typeof(SpawnTimerLoop).GetMethod("Spawn", BindingFlags.NonPublic | BindingFlags.Instance)
            .Invoke(spawner, null);

        spawnedProjectile = GameObject.Find(projectile.name + "(Clone)");
        Assert.That(spawnedProjectile, Is.Not.Null);
        Assert.That(Vector3.Distance(spawnedProjectile.transform.position, aim.transform.position), Is.LessThan(0.001f));
        Assert.That(Vector3.Angle(spawnedProjectile.transform.forward,
            target.position - spawnedProjectile.transform.position), Is.LessThan(0.05f));
    }

    [Test]
    public void CommonSetupWiresMuzzleToItsPlayerTargetSelector()
    {
        GameObject setup = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Common/CommonSceneSetup.prefab");
        LookOnTargetOrMiddle muzzleAim = setup.GetComponentInChildren<LookOnTargetOrMiddle>(true);
        Assert.That(muzzleAim, Is.Not.Null);
        TargetSelector configured = new SerializedObject(muzzleAim).FindProperty("targetSelector").objectReferenceValue as TargetSelector;
        Assert.That(configured, Is.SameAs(setup.GetComponentInChildren<TargetSelector>(true)));
        Assert.That(muzzleAim.GetComponent<SpawnOnMouseClickTimerLoop>(), Is.Not.Null);
    }

    [Test]
    public void Intro7WiresMuzzleToItsPlayerTargetSelector()
    {
        var scene = EditorSceneManager.OpenPreviewScene("Assets/Scenes/Intro 7/Intro 7.unity");
        try
        {
            LookOnTargetOrMiddle muzzleAim = null;
            foreach (GameObject sceneRoot in scene.GetRootGameObjects())
            {
                muzzleAim = sceneRoot.GetComponentInChildren<LookOnTargetOrMiddle>(true);
                if (muzzleAim != null)
                    break;
            }
            Assert.That(muzzleAim, Is.Not.Null);
            PlayerController owner = muzzleAim.GetComponentInParent<PlayerController>();
            Assert.That(owner, Is.Not.Null);
            TargetSelector configured = new SerializedObject(muzzleAim).FindProperty("targetSelector").objectReferenceValue as TargetSelector;
            Assert.That(configured, Is.SameAs(owner.GetComponentInChildren<TargetSelector>(true)));
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }

    private Transform Child(string name)
    {
        Transform child = new GameObject(name).transform;
        child.SetParent(root.transform, false);
        return child;
    }

    private void AssertForward(Vector3 point)
    {
        Assert.That(Vector3.Angle(aim.transform.forward, point - aim.transform.position), Is.LessThan(0.05f));
    }

    private static void SetField(object instance, string name, object value)
    {
        instance.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(instance, value);
    }
}
