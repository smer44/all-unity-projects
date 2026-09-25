using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class PlayerVisualsShootingTweaksTests
{
    private GameObject root;
    private PlayerController player;
    private PlayerVisualsShootingTweaks tweaks;
    private Transform[] spines;
    private Transform hips;
    private Transform leftLeg;
    private Transform rightLeg;

    [SetUp]
    public void SetUp()
    {
        root = new GameObject("Shooting tweaks tests");
        var owner = new GameObject("Player");
        owner.transform.SetParent(root.transform, false);
        owner.SetActive(false);
        player = owner.AddComponent<PlayerController>();
        var visuals = new GameObject("Visuals").transform;
        visuals.SetParent(root.transform, false);
        visuals.rotation = Quaternion.Euler(0f, 37f, 0f);
        player.visualsPivot = visuals;

        var prefabPlayer = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player/Player.prefab")
            .GetComponent<PlayerController>();
        Transform skeleton = CopyTransforms(prefabPlayer.animator.transform, visuals);
        player.animator = skeleton.gameObject.AddComponent<Animator>();
        player.animator.runtimeAnimatorController = prefabPlayer.animator.runtimeAnimatorController;
        player.animator.fireEvents = false;
        player.animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        player.animator.Update(0f);
        Invoke(player, "Awake");
        tweaks = player.ShootingTweaks;
        hips = skeleton.Find("mixamorig:Hips");
        Transform spine = hips.Find("mixamorig:Spine");
        Transform spine1 = spine.Find("mixamorig:Spine1");
        spines = new[] { spine, spine1, spine1.Find("mixamorig:Spine2") };
        leftLeg = hips.Find("mixamorig:LeftUpLeg");
        rightLeg = hips.Find("mixamorig:RightUpLeg");
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(player.ButtonControls);
        Object.DestroyImmediate(root);
    }

    [TestCase(-2f, 153f)]
    [TestCase(-1f, 153f)]
    [TestCase(-0.5f, 115.5f)]
    [TestCase(0f, 78f)]
    [TestCase(0.5f, 39f)]
    [TestCase(1f, 0f)]
    [TestCase(2f, 0f)]
    public void ShootingCorrectionInterpolatesAndSpreadsAcrossSpines(float velocityRight, float expectedAngle)
    {
        SampleShooting(velocityRight);
        Quaternion[] animatedSpines = CaptureSpineRotations();
        var localPositions = System.Array.ConvertAll(spines, bone => bone.localPosition);
        Quaternion animatedHips = hips.rotation;
        Quaternion animatedLeftLeg = leftLeg.rotation;
        Quaternion animatedRightLeg = rightLeg.rotation;
        Vector3 spinePosition = spines[0].position;
        tweaks.TurnOn();
        Invoke(tweaks, "Update");
        AssertSpineRotations(animatedSpines, 0f);
        Invoke(tweaks, "LateUpdate");
        Assert.That(tweaks.CorrectionAngle, Is.EqualTo(expectedAngle).Within(0.001f));
        AssertSpineRotations(animatedSpines, expectedAngle);
        AssertRotation(hips.rotation, animatedHips);
        AssertRotation(leftLeg.rotation, animatedLeftLeg);
        AssertRotation(rightLeg.rotation, animatedRightLeg);
        Assert.That(Vector3.Distance(spines[0].position, spinePosition), Is.LessThan(0.0001f));
        for (int i = 0; i < spines.Length; i++)
            Assert.That(Vector3.Distance(spines[i].localPosition, localPositions[i]), Is.LessThan(0.0001f));
    }

    [Test]
    public void CorrectionDoesNotAccumulateWhenAnimationDoesNotEvaluateAndTurnOffRestoresPose()
    {
        SampleShooting(0f);
        Quaternion[] animated = CaptureSpineRotations();
        tweaks.TurnOn();
        for (int frame = 0; frame < 60; frame++)
        {
            Invoke(tweaks, "Update");
            Invoke(tweaks, "LateUpdate");
            Invoke(tweaks, "LateUpdate");
            AssertSpineRotations(animated, 78f);
        }
        tweaks.TurnOff();
        Assert.That(tweaks.IsOn, Is.False);
        AssertSpineRotations(animated, 0f);
        Invoke(tweaks, "LateUpdate");
        AssertSpineRotations(animated, 0f);
    }

    [Test]
    public void AnimationCanReplaceThePoseBeforeCorrectionIsRemoved()
    {
        SampleShooting(0f);
        tweaks.TurnOn();
        Invoke(tweaks, "Update");
        Invoke(tweaks, "LateUpdate");
        player.animator.Update(0.13f);
        Quaternion[] nextAnimatedPose = CaptureSpineRotations();
        Invoke(tweaks, "Update");
        AssertSpineRotations(nextAnimatedPose, 0f);
        Invoke(tweaks, "LateUpdate");
        AssertSpineRotations(nextAnimatedPose, 78f);
        Invoke(tweaks, "OnDisable");
        Assert.That(tweaks.IsOn, Is.False);
        AssertSpineRotations(nextAnimatedPose, 0f);
    }

    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    public void CorrectionUsesConfiguredArrayLength(int count)
    {
        System.Array.Resize(ref spines, count);
        SetSpineBones(spines);
        SampleShooting(0f);
        Quaternion[] animated = CaptureSpineRotations();
        tweaks.TurnOn();
        Invoke(tweaks, "LateUpdate");
        AssertSpineRotations(animated, 78f);
        tweaks.TurnOff();
        AssertSpineRotations(animated, 0f);
    }

    [Test]
    public void ReplacingArrayRestoresPreviousBonesAndHandlesMissingEntries()
    {
        SampleShooting(0f);
        Quaternion[] animated = CaptureSpineRotations();
        tweaks.TurnOn();
        Invoke(tweaks, "LateUpdate");

        SetSpineBones(new[] { spines[0], null, spines[2] });
        Invoke(tweaks, "LateUpdate");
        AssertRotation(spines[0].rotation, Quaternion.AngleAxis(26f, player.visualsPivot.up) * animated[0]);
        AssertRotation(spines[2].rotation, Quaternion.AngleAxis(78f, player.visualsPivot.up) * animated[2]);

        SetSpineBones(System.Array.Empty<Transform>());
        Invoke(tweaks, "LateUpdate");
        AssertSpineRotations(animated, 0f);
    }

    [Test]
    public void PlayerPrefabAssignsFirstThreeSpinesAndKeepsHipsOutOfUpperBodyMask()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player/Player.prefab");
        var component = prefab.GetComponent<PlayerVisualsShootingTweaks>();
        Assert.That(component, Is.Not.Null);
        var serialized = new SerializedObject(component);
        SerializedProperty bones = serialized.FindProperty("spineBones");
        Assert.That(bones.arraySize, Is.EqualTo(3));
        Transform spine = prefab.GetComponent<PlayerController>().animator.transform.Find("mixamorig:Hips/mixamorig:Spine");
        Transform spine1 = spine.Find("mixamorig:Spine1");
        var expected = new[] { spine, spine1, spine1.Find("mixamorig:Spine2") };
        for (int i = 0; i < expected.Length; i++)
            Assert.That(bones.GetArrayElementAtIndex(i).objectReferenceValue, Is.SameAs(expected[i]));
        var mask = AssetDatabase.LoadAssetAtPath<AvatarMask>("Assets/Prefabs/Player/UpperBodyAvatarMask.mask");
        for (int i = 0; i < mask.transformCount; i++)
            if (mask.GetTransformPath(i) == "mixamorig:Hips")
                Assert.That(mask.GetTransformActive(i), Is.False);
        int tweaksOrder = typeof(PlayerVisualsShootingTweaks).GetCustomAttribute<DefaultExecutionOrder>().order;
        int aimingOrder = typeof(UpperBodyVisualsController).GetCustomAttribute<DefaultExecutionOrder>().order;
        Assert.That(tweaksOrder, Is.LessThan(aimingOrder));
    }

    private Quaternion[] CaptureSpineRotations() => System.Array.ConvertAll(spines, bone => bone.rotation);

    private void AssertSpineRotations(Quaternion[] animated, float fullAngle)
    {
        for (int i = 0; i < spines.Length; i++)
            AssertRotation(spines[i].rotation,
                Quaternion.AngleAxis(fullAngle * (i + 1f) / spines.Length, player.visualsPivot.up) * animated[i]);
    }

    private void SetSpineBones(Transform[] bones)
    {
        var serialized = new SerializedObject(tweaks);
        SerializedProperty property = serialized.FindProperty("spineBones");
        property.arraySize = bones.Length;
        for (int i = 0; i < bones.Length; i++)
            property.GetArrayElementAtIndex(i).objectReferenceValue = bones[i];
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private void SampleShooting(float velocityRight)
    {
        int upperLayer = player.animator.GetLayerIndex("UpperBodyLayer");
        player.animator.SetFloat("VelocityRight", velocityRight);
        player.animator.SetFloat("VelocityForward", Mathf.Sqrt(Mathf.Max(0f, 1f - velocityRight * velocityRight)));
        player.animator.Play("GroundMoveBlendTree", 0, 0.25f);
        player.animator.Play("Shoot", upperLayer, 0.2f);
        player.animator.SetLayerWeight(upperLayer, 1f);
        player.animator.Update(0f);
    }

    private static Transform CopyTransforms(Transform source, Transform parent)
    {
        var copy = new GameObject(source.name).transform;
        copy.SetParent(parent, false);
        copy.localPosition = source.localPosition;
        copy.localRotation = source.localRotation;
        copy.localScale = source.localScale;
        foreach (Transform child in source)
            CopyTransforms(child, copy);
        return copy;
    }

    private static void AssertRotation(Quaternion actual, Quaternion expected) =>
        Assert.That(Quaternion.Angle(actual, expected), Is.LessThan(0.1f));

    private static void Invoke(object target, string method) =>
        target.GetType().GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(target, null);
}
