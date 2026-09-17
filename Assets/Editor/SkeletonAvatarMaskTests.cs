using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public class SkeletonAvatarMaskTests
{
    private GameObject root;
    private Transform hips;
    private Transform spine;
    private Transform hand;
    private Transform leg;
    private Transform prop;
    private SkeletonAvatarMask source;
    private string assetFolder;
    private Avatar avatar;

    [SetUp]
    public void SetUp()
    {
        root = new GameObject("Mask test character");
        root.AddComponent<Animator>();
        hips = Child(root.transform, "Hips");
        spine = Child(hips, "Spine");
        hand = Child(spine, "Hand");
        leg = Child(hips, "Leg");
        prop = Child(root.transform, "Prop");
        source = root.AddComponent<SkeletonAvatarMask>();
        source.SetSkeletonRoot(hips.gameObject);
        string folderName = "__SkeletonAvatarMaskTests_" + Guid.NewGuid().ToString("N");
        assetFolder = "Assets/" + folderName;
        AssetDatabase.CreateFolder("Assets", folderName);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(root);
        Object.DestroyImmediate(avatar);
        if (AssetDatabase.IsValidFolder(assetFolder))
            AssetDatabase.DeleteAsset(assetFolder);
    }

    [Test]
    public void SkeletonRootIncludesInactiveBonesAndUsesAnimatorRelativePaths()
    {
        hand.gameObject.SetActive(false);
        source.RefreshHierarchy();
        Assert.That(source.Bones.Select(bone => bone.Path), Is.EqualTo(new[] { "", "Spine", "Spine/Hand", "Leg" }));
        AvatarMask mask = source.CreateMask();
        try
        {
            Assert.That(mask.transformCount, Is.EqualTo(6));
            AssertActive(mask, "", false);
            AssertActive(mask, "Hips", true);
            AssertActive(mask, "Hips/Spine/Hand", true);
            AssertActive(mask, "Prop", false);
        }
        finally { Object.DestroyImmediate(mask); }
    }

    [Test]
    public void WithoutAnAnimatorPathsAreRelativeToTheAssignedRoot()
    {
        Object.DestroyImmediate(root.GetComponent<Animator>());
        source.RefreshHierarchy();
        AvatarMask mask = source.CreateMask();
        try
        {
            Assert.That(source.BindingRoot, Is.SameAs(hips));
            AssertActive(mask, "", true);
            AssertActive(mask, "Spine/Hand", true);
            Assert.That(mask.transformCount, Is.EqualTo(4));
        }
        finally { Object.DestroyImmediate(mask); }
    }

    [Test]
    public void IndividualAndBranchTogglesDoNotAffectSiblingBranches()
    {
        source.SetAllActive(false);
        source.SetBoneActive(1, true, true);
        Assert.That(source.Bones.Select(bone => bone.Active), Is.EqualTo(new[] { false, true, true, false }));
        source.SetBoneActive(1, false);
        Assert.That(source.Bones[2].Active, Is.True, "Parent and child masks can be independent.");
        source.SetBoneActive(0, true, true);
        Assert.That(source.Bones.All(bone => bone.Active), Is.True);
    }

    [Test]
    public void RefreshPreservesRenamedAndReparentedBonesAndNewBonesInheritTheirParent()
    {
        source.SetBoneActive(1, false, true);
        spine.name = "Chest";
        hand.SetParent(hips, false);
        Transform finger = Child(spine, "New finger");
        Object.DestroyImmediate(leg.gameObject);
        Assert.That(source.HierarchyMatches(), Is.False);
        source.RefreshHierarchy();
        Assert.That(source.HierarchyMatches(), Is.True);
        Assert.That(source.Bones.Single(bone => bone.Bone == hand).Active, Is.False);
        Assert.That(source.Bones.Single(bone => bone.Bone == finger).Active, Is.False);
        Assert.That(source.Bones.Single(bone => bone.Bone == spine).Path, Is.EqualTo("Chest"));
    }

    [Test]
    public void MatchingPathsPreserveChoicesWhenReplacingTheSkeleton()
    {
        source.SetBoneActive(1, false, true);
        Transform replacement = Child(root.transform, "Replacement");
        Transform replacementSpine = Child(replacement, "Spine");
        Child(replacementSpine, "Hand");
        source.SetSkeletonRoot(replacement.gameObject);
        Assert.That(source.Bones.Select(bone => bone.Active), Is.EqualTo(new[] { true, false, false }));
    }

    [Test]
    public void MovingTheSkeletonUnderANewWrapperUpdatesBindingPathsAndKeepsSelections()
    {
        source.SetBoneActive(1, false, true);
        Transform wrapper = Child(root.transform, "Armature");
        hips.SetParent(wrapper, false);
        Assert.That(source.HierarchyMatches(), Is.False);
        source.RefreshHierarchy();
        AvatarMask mask = source.CreateMask();
        try
        {
            AssertActive(mask, "Armature/Hips/Spine", false);
            AssertActive(mask, "Armature/Hips/Leg", true);
        }
        finally { Object.DestroyImmediate(mask); }
    }

    [TestCase("Leg")]
    [TestCase("Bad/Name")]
    [TestCase("")]
    public void AmbiguousOrInvalidPathsAreRejectedWithoutChangingAnExistingMask(string name)
    {
        AvatarMask mask = source.CreateMask();
        try
        {
            int count = mask.transformCount;
            Child(hips, name);
            source.RefreshHierarchy();
            Assert.That(source.TryValidate(out string error), Is.False);
            Assert.That(error, Is.Not.Empty);
            Assert.Throws<InvalidOperationException>(() => source.ApplyTo(mask));
            Assert.That(mask.transformCount, Is.EqualTo(count));
            AssertActive(mask, "Hips/Leg", true);
        }
        finally { Object.DestroyImmediate(mask); }
    }

    [Test]
    public void MissingRootAndStaleHierarchyCannotGenerateAMask()
    {
        source.SetSkeletonRoot(null);
        Assert.That(source.Bones, Is.Empty);
        Assert.Throws<InvalidOperationException>(() => source.CreateMask());
        source.SetSkeletonRoot(hips.gameObject);
        Child(hips, "New bone");
        Assert.Throws<InvalidOperationException>(() => source.CreateMask());
    }

    [Test]
    public void NativeAssetPersistsAndUpdatingItKeepsAnimatorLayerReferences()
    {
        string path = assetFolder + "/Upper.mask";
        AvatarMask mask = SkeletonAvatarMaskAssets.Create(source, path);
        string guid = AssetDatabase.AssetPathToGUID(path);
        var controller = CreateController(mask);
        source.SetAllActive(false);
        source.SetBoneActive(1, true, true);
        source.ApplyTo(mask);
        EditorUtility.SetDirty(mask);
        AssetDatabase.SaveAssetIfDirty(mask);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var reloaded = AssetDatabase.LoadAssetAtPath<AvatarMask>(path);
        Assert.That(AssetDatabase.AssetPathToGUID(path), Is.EqualTo(guid));
        Assert.That(controller.layers[1].avatarMask, Is.EqualTo(reloaded));
        AssertActive(reloaded, "Hips/Spine", true);
        AssertActive(reloaded, "Hips/Leg", false);
        Assert.Throws<InvalidOperationException>(() => SkeletonAvatarMaskAssets.Create(source, path));
        Assert.That(AssetDatabase.AssetPathToGUID(path), Is.EqualTo(guid));
    }

    [Test]
    public void UndoRestoresBothBoneChoicesAndTheGeneratedMask()
    {
        AvatarMask mask = SkeletonAvatarMaskAssets.Create(source, assetFolder + "/Undo.mask");
        Undo.IncrementCurrentGroup();
        Undo.RecordObjects(new Object[] { source, mask }, "Exclude mask bones");
        source.SetAllActive(false);
        source.ApplyTo(mask);
        Undo.FlushUndoRecordObjects();
        Undo.PerformUndo();
        Assert.That(source.Bones.All(bone => bone.Active), Is.True);
        AssertActive(mask, "Hips/Spine", true);
    }

    [Test]
    public void AuthoringSelectionsAndRootReferencesSurvivePrefabSerialization()
    {
        source.SetAllActive(false);
        source.SetBoneActive(1, true, true);
        SkeletonAvatarMaskAssets.Create(source, assetFolder + "/Prefab.mask");
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, assetFolder + "/Character.prefab");
        var saved = prefab.GetComponent<SkeletonAvatarMask>();
        Assert.That(saved.SkeletonRoot.transform.IsChildOf(prefab.transform), Is.True);
        Assert.That(saved.HierarchyMatches(), Is.True);
        Assert.That(saved.Bones.Select(bone => bone.Active), Is.EqualTo(new[] { false, true, true, false }));
        Assert.That(saved.GeneratedMask, Is.EqualTo(source.GeneratedMask));
    }

    [Test]
    public void AuthoringComponentUsesTheCustomInspector()
    {
        Editor editor = Editor.CreateEditor(source);
        try { Assert.That(editor, Is.TypeOf<SkeletonAvatarMaskEditor>()); }
        finally { Object.DestroyImmediate(editor); }
    }

    [Test]
    public void ProjectCreateMenuContainsSkeletonAvatarMask()
    {
        // Query the editor's actual menu registration, not just the attribute on the class.
        var menuItemExists = typeof(Menu).GetMethod("MenuItemExists",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        Assert.That(menuItemExists, Is.Not.Null);
        Assert.That(menuItemExists.Invoke(null, new object[] { "Assets/Create/Animation/Skeleton Avatar Mask" }), Is.True);
    }

    [Test]
    public void ProjectAssetUsesTheCustomInspector()
    {
        var asset = ScriptableObject.CreateInstance<SkeletonAvatarMaskAsset>();
        Editor editor = Editor.CreateEditor(asset);
        try { Assert.That(editor, Is.TypeOf<SkeletonAvatarMaskAssetEditor>()); }
        finally
        {
            Object.DestroyImmediate(editor);
            Object.DestroyImmediate(asset);
        }
    }

    [Test]
    public void ProjectAssetPersistsPrefabBonesAndGeneratesAnAnimatorLayerMask()
    {
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, assetFolder + "/AssetCharacter.prefab");
        var asset = ScriptableObject.CreateInstance<SkeletonAvatarMaskAsset>();
        string assetPath = assetFolder + "/Upper Body.asset";
        AssetDatabase.CreateAsset(asset, assetPath);
        asset.SetSkeletonRoot(prefab.transform.Find("Hips").gameObject);
        asset.SetAllActive(false);
        asset.SetBoneActive(1, true, true);
        AvatarMask mask = SkeletonAvatarMaskAssets.Create(asset, assetFolder + "/AssetUpper.mask");
        var controller = CreateController(mask);
        Resources.UnloadAsset(asset);

        var reloaded = AssetDatabase.LoadAssetAtPath<SkeletonAvatarMaskAsset>(assetPath);
        Assert.That(reloaded.SkeletonRoot, Is.EqualTo(prefab.transform.Find("Hips").gameObject));
        Assert.That(reloaded.HierarchyMatches(), Is.True);
        Assert.That(reloaded.Bones.Select(bone => bone.Active), Is.EqualTo(new[] { false, true, true, false }));
        Assert.That(reloaded.GeneratedMask, Is.EqualTo(mask));
        Assert.That(controller.layers[1].avatarMask, Is.EqualTo(mask));
        AssertActive(mask, "Hips/Spine/Hand", true);
        AssertActive(mask, "Hips/Leg", false);

        Animator animator = root.GetComponent<Animator>();
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        animator.runtimeAnimatorController = controller;
        animator.Rebind();
        animator.Update(0.1f);
        Assert.That(hand.localPosition.x, Is.EqualTo(9f).Within(0.001f));
        Assert.That(leg.localPosition.x, Is.EqualTo(1f).Within(0.001f));
    }

    [Test]
    public void SceneSkeletonImportSurvivesAssetReloadAndStillDrivesMaskedPlayback()
    {
        var asset = ScriptableObject.CreateInstance<SkeletonAvatarMaskAsset>();
        string path = assetFolder + "/Scene Skeleton.asset";
        AssetDatabase.CreateAsset(asset, path);
        SkeletonAvatarMaskSceneInput.Assign(asset, hips.gameObject);
        Assert.That(asset.SkeletonRoot, Is.EqualTo(hips.gameObject));
        asset.SetAllActive(false);
        asset.SetBoneActive(1, true, true);
        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssetIfDirty(asset);
        Resources.UnloadAsset(asset);

        asset = AssetDatabase.LoadAssetAtPath<SkeletonAvatarMaskAsset>(path);
        Assert.That(asset.SkeletonRoot, Is.Null, "Unsaved scene references must not be serialized in the asset.");
        asset.RefreshHierarchy(); // An unavailable input must not clear the imported hierarchy.
        Assert.That(asset.Bones.Select(bone => bone.Path), Is.EqualTo(new[] { "", "Spine", "Spine/Hand", "Leg" }));
        Assert.That(asset.Bones.All(bone => bone.Bone == null), Is.True);
        Assert.That(asset.TryValidate(out string error), Is.True, error);
        AvatarMask mask = SkeletonAvatarMaskAssets.Create(asset, assetFolder + "/Scene.mask");
        AssertActive(mask, "Hips/Spine/Hand", true);
        AssertActive(mask, "Hips/Leg", false);
        AssertActive(mask, "Prop", false);
        Animator animator = root.GetComponent<Animator>();
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        animator.runtimeAnimatorController = CreateController(mask);
        animator.Rebind();
        animator.Update(0.1f);
        Assert.That(hand.localPosition.x, Is.EqualTo(9f).Within(0.001f));
        Assert.That(leg.localPosition.x, Is.EqualTo(1f).Within(0.001f));

        Undo.IncrementCurrentGroup();
        Undo.RecordObject(asset, "Change imported mask");
        asset.SetAllActive(false);
        Undo.FlushUndoRecordObjects();
        Undo.PerformUndo();
        Assert.That(asset.Bones.Select(bone => bone.Active), Is.EqualTo(new[] { false, true, true, false }));
    }

    [Test]
    public void SceneSkeletonInputReconnectsAfterAssetAndSceneReload()
    {
        string scenePath = assetFolder + "/Source.unity";
        string assetPath = assetFolder + "/Saved Scene Skeleton.asset";
        // A named copy can open alongside the test runner's untitled scene. Do not replace or save that scene.
        Assert.That(EditorSceneManager.SaveScene(root.scene, scenePath, true), Is.True);
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
        try
        {
            Transform sceneHips = scene.GetRootGameObjects().Single(obj => obj.name == root.name).transform.Find("Hips");
            var asset = ScriptableObject.CreateInstance<SkeletonAvatarMaskAsset>();
            AssetDatabase.CreateAsset(asset, assetPath);
            SkeletonAvatarMaskSceneInput.Assign(asset, sceneHips.gameObject);
            asset.SetBoneActive(1, false, true);
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssetIfDirty(asset);
            Resources.UnloadAsset(asset);
            asset = AssetDatabase.LoadAssetAtPath<SkeletonAvatarMaskAsset>(assetPath);
            SkeletonAvatarMaskSceneInput.Restore(asset);
            Assert.That(asset.SkeletonRoot, Is.EqualTo(sceneHips.gameObject));

            EditorSceneManager.CloseScene(scene, true);
            SkeletonAvatarMaskSceneInput.Restore(asset);
            Assert.That(asset.SkeletonRoot, Is.Null);
            Assert.That(asset.TryValidate(out string error), Is.True, error);
            AvatarMask mask = SkeletonAvatarMaskAssets.Create(asset, assetFolder + "/Closed Scene.mask");
            AssertActive(mask, "Hips/Spine/Hand", false);
            AssertActive(mask, "Hips/Leg", true);

            scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            SkeletonAvatarMaskSceneInput.Restore(asset);
            Assert.That(asset.SkeletonRoot, Is.Not.Null);
            Assert.That(asset.SkeletonRoot.scene, Is.EqualTo(scene));
            Assert.That(asset.HierarchyMatches(), Is.True);
            Assert.That(asset.Bones[1].Active, Is.False);
        }
        finally
        {
            if (scene.IsValid() && scene.isLoaded)
                EditorSceneManager.CloseScene(scene, true);
        }
    }

    [Test]
    public void SceneHierarchyRefreshPreservesSelectionsAndRejectsInvalidPaths()
    {
        var asset = ScriptableObject.CreateInstance<SkeletonAvatarMaskAsset>();
        AvatarMask mask = null;
        try
        {
            SkeletonAvatarMaskSceneInput.Assign(asset, hips.gameObject);
            asset.SetAllActive(false);
            asset.SetBoneActive(1, true, true);
            Child(spine, "Finger");
            Assert.That(asset.HierarchyMatches(), Is.False);
            asset.RefreshHierarchy();
            Assert.That(asset.Bones.Single(bone => bone.Path == "Spine/Finger").Active, Is.True);
            mask = asset.CreateMask();
            AssertActive(mask, "Hips/Spine/Finger", true);
            AssertActive(mask, "Hips/Leg", false);

            Child(hips, "Bad/Name");
            asset.RefreshHierarchy();
            Assert.That(asset.TryValidate(out _), Is.False);
            Assert.Throws<InvalidOperationException>(() => asset.ApplyTo(mask));
            AssertActive(mask, "Hips/Spine/Finger", true);
            SkeletonAvatarMaskSceneInput.Assign(asset, null);
            Assert.That(asset.Bones, Is.Empty);
            Assert.That(asset.HasSceneSnapshot, Is.False);
        }
        finally
        {
            Object.DestroyImmediate(mask);
            Object.DestroyImmediate(asset);
        }
    }

    [Test]
    public void PlayerSceneArmatureImportsInstanceBonesWithAnimatorRelativePaths()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player/Player.prefab");
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        var asset = ScriptableObject.CreateInstance<SkeletonAvatarMaskAsset>();
        AvatarMask mask = null;
        try
        {
            Transform armature = instance.GetComponentsInChildren<Transform>(true).First(bone => bone.name == "Armature");
            Transform extraBone = Child(armature, "Scene-only bone");
            SkeletonAvatarMaskSceneInput.Assign(asset, armature.gameObject);
            Assert.That(asset.SkeletonRoot, Is.EqualTo(armature.gameObject));
            Assert.That(asset.Bones.Count, Is.GreaterThan(10));
            mask = asset.CreateMask();
            AssertActive(mask, AnimationUtility.CalculateTransformPath(extraBone, asset.BindingRoot), true);
            foreach (string clipName in new[] { "Shoot", "ForwardSwordAttack" })
            {
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Animation/" + clipName + ".anim");
                var paths = Enumerable.Range(0, mask.transformCount).Select(mask.GetTransformPath).ToHashSet();
                foreach (var binding in AnimationUtility.GetCurveBindings(clip).Where(binding => binding.type == typeof(Transform)))
                    Assert.That(paths.Contains(binding.path), Is.True, clipName + ": " + binding.path);
            }
        }
        finally
        {
            Object.DestroyImmediate(mask);
            Object.DestroyImmediate(asset);
            Object.DestroyImmediate(instance);
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void NativeAnimatorLayersMaskBonesWithoutHumanoidAvatar(bool useGenericAvatar)
    {
        source.SetAllActive(false);
        source.SetBoneActive(1, true, true);
        AvatarMask mask = SkeletonAvatarMaskAssets.Create(source, assetFolder + "/Playback.mask");
        AnimatorController controller = CreateController(mask);
        Object.DestroyImmediate(source); // Playback depends only on Unity's native mask asset.
        Animator animator = root.GetComponent<Animator>();
        if (useGenericAvatar)
        {
            avatar = AvatarBuilder.BuildGenericAvatar(root, "Hips");
            Assert.That(avatar.isValid, Is.True);
            Assert.That(avatar.isHuman, Is.False);
            animator.avatar = avatar;
        }
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        animator.runtimeAnimatorController = controller;
        animator.Rebind();
        animator.Update(0.1f);
        Assert.That(spine.localPosition.x, Is.EqualTo(9f).Within(0.001f));
        Assert.That(hand.localPosition.x, Is.EqualTo(9f).Within(0.001f));
        Assert.That(leg.localPosition.x, Is.EqualTo(1f).Within(0.001f));
        Assert.That(prop.localPosition.x, Is.EqualTo(1f).Within(0.001f));
    }

    [Test]
    public void ExistingPlayerSkeletonProducesPathsMatchingGunAndSwordClips()
    {
        var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player/Player.prefab");
        Transform prefabHips = playerPrefab.GetComponentsInChildren<Transform>(true)
            .First(bone => bone.name == "mixamorig:Hips");
        source.SetSkeletonRoot(prefabHips.gameObject);
        AvatarMask mask = source.CreateMask();
        try
        {
            var paths = Enumerable.Range(0, mask.transformCount).Select(mask.GetTransformPath).ToHashSet();
            foreach (string clipName in new[] { "Shoot", "ForwardSwordAttack" })
            {
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Animation/" + clipName + ".anim");
                var bindings = AnimationUtility.GetCurveBindings(clip).Where(binding => binding.type == typeof(Transform)).ToArray();
                Assert.That(bindings, Is.Not.Empty);
                foreach (var binding in bindings)
                    Assert.That(paths.Contains(binding.path), Is.True, clipName + ": " + binding.path);
            }
        }
        finally { Object.DestroyImmediate(mask); }
    }

    private AnimatorController CreateController(AvatarMask mask)
    {
        var controller = AnimatorController.CreateAnimatorControllerAtPath(assetFolder + "/Playback.controller");
        var baseState = controller.layers[0].stateMachine.AddState("Locomotion");
        baseState.motion = CreateClip("Base", 1f);
        controller.layers[0].stateMachine.defaultState = baseState;
        controller.AddLayer("Attack");
        var layers = controller.layers;
        layers[1].avatarMask = mask;
        layers[1].blendingMode = AnimatorLayerBlendingMode.Override;
        layers[1].defaultWeight = 1f;
        var attackState = layers[1].stateMachine.AddState("Attack");
        attackState.motion = CreateClip("Attack", 9f);
        layers[1].stateMachine.defaultState = attackState;
        controller.layers = layers;
        AssetDatabase.SaveAssetIfDirty(controller);
        return controller;
    }

    private AnimationClip CreateClip(string name, float value)
    {
        var clip = new AnimationClip { name = name };
        foreach (string path in new[] { "Hips", "Hips/Spine", "Hips/Spine/Hand", "Hips/Leg", "Prop" })
            clip.SetCurve(path, typeof(Transform), "m_LocalPosition.x", AnimationCurve.Constant(0f, 1f, value));
        AssetDatabase.CreateAsset(clip, assetFolder + "/" + name + ".anim");
        return clip;
    }

    private static Transform Child(Transform parent, string name)
    {
        var child = new GameObject(name).transform;
        child.SetParent(parent, false);
        return child;
    }

    private static void AssertActive(AvatarMask mask, string path, bool active)
    {
        int index = Enumerable.Range(0, mask.transformCount).First(i => mask.GetTransformPath(i) == path);
        Assert.That(mask.GetTransformActive(index), Is.EqualTo(active), path);
    }
}
