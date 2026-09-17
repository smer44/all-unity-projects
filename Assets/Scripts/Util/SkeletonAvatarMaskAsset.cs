using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using BoneSelection = SkeletonAvatarMask.BoneSelection;

/// <summary>Authors a native AvatarMask from a scene, prefab or model hierarchy.</summary>
[CreateAssetMenu(fileName = "New Skeleton Avatar Mask", menuName = "Animation/Skeleton Avatar Mask")]
public sealed class SkeletonAvatarMaskAsset : ScriptableObject, ISkeletonAvatarMaskSource
{
    [SerializeField] private GameObject skeletonRoot;
    [SerializeField, HideInInspector] private List<BoneSelection> bones = new List<BoneSelection>();
    [SerializeField, HideInInspector] private AvatarMask generatedMask;
    [SerializeField, HideInInspector] private Transform hierarchyBindingRoot;
    [SerializeField, HideInInspector] private string hierarchyPrefix;
    [SerializeField, HideInInspector] private bool sceneSnapshot;
    [SerializeField, HideInInspector] private string sceneRootId;
    [SerializeField, HideInInspector] private string importedRootName;
    [SerializeField, HideInInspector] private string importedBindingRootName;
    [SerializeField, HideInInspector] private string importedHierarchyError;
    [SerializeField, HideInInspector] private List<string> importedBindingPaths = new List<string>();

    // Scene object references cannot be serialized in a Project asset. Store a path snapshot instead.
    [NonSerialized] private GameObject liveSceneRoot;
    [NonSerialized] private string liveSceneRootId;

    public GameObject SkeletonRoot => sceneSnapshot
        ? (liveSceneRootId == sceneRootId ? liveSceneRoot : null) : skeletonRoot;
    public IReadOnlyList<BoneSelection> Bones => bones;
    public AvatarMask GeneratedMask => generatedMask;
    public bool HasSceneSnapshot => sceneSnapshot;
    public string SceneRootId => sceneRootId;
    public string RootName => SkeletonRoot != null ? SkeletonRoot.name : importedRootName;
    public string BindingRootName => BindingRoot != null ? BindingRoot.name : importedBindingRootName;

    // Clip bindings are relative to the Animator, not necessarily the selected first bone.
    public Transform BindingRoot => SkeletonAvatarMaskUtility.GetBindingRoot(SkeletonRoot);

    public void SetSkeletonRoot(GameObject root)
    {
        sceneSnapshot = root != null && root.scene.IsValid();
        skeletonRoot = sceneSnapshot ? null : root;
        liveSceneRoot = sceneSnapshot ? root : null;
        sceneRootId = liveSceneRootId = "";
        importedRootName = importedBindingRootName = importedHierarchyError = null;
        importedBindingPaths.Clear();
        RefreshHierarchy();
    }

    // The editor uses GlobalObjectId to reconnect the scene input after a domain/asset reload.
    public void SetSceneRootId(string id) => sceneRootId = liveSceneRootId = id;

    public void RestoreSceneRoot(GameObject root)
    {
        liveSceneRoot = root;
        liveSceneRootId = sceneRootId;
    }

    public void SetGeneratedMask(AvatarMask mask) => generatedMask = mask;

    public void RefreshHierarchy()
    {
        // A closed scene must not erase an imported hierarchy or its selections.
        if (sceneSnapshot && SkeletonRoot == null)
            return;
        bones = SkeletonAvatarMaskUtility.RefreshHierarchy(this);
        hierarchyPrefix = SkeletonAvatarMaskUtility.GetSkeletonPrefix(this);
        hierarchyBindingRoot = sceneSnapshot ? null : BindingRoot;
        if (sceneSnapshot)
        {
            importedRootName = SkeletonRoot.name;
            importedBindingRootName = BindingRoot.name;
            importedBindingPaths = SkeletonAvatarMaskUtility.GetTransformPaths(BindingRoot);
            SkeletonAvatarMaskUtility.ValidateHierarchy(BindingRoot, out importedHierarchyError);
            // Keep no scene references in serialized fields, including individual bone entries.
            bones = bones.Select(bone => new BoneSelection(null, bone.Path, bone.Active)).ToList();
        }
    }

    public void SetBoneActive(int index, bool active, bool includeChildren = false) =>
        SkeletonAvatarMaskUtility.SetBoneActive(bones, index, active, includeChildren);

    public void SetAllActive(bool active) => SkeletonAvatarMaskUtility.SetAllActive(bones, active);

    public bool HierarchyMatches()
    {
        if (!sceneSnapshot)
            return SkeletonAvatarMaskUtility.HierarchyMatches(this, hierarchyBindingRoot, hierarchyPrefix);
        return SkeletonRoot == null ||
            (SkeletonAvatarMaskUtility.HierarchyMatches(this, null, hierarchyPrefix, false)
                && importedBindingPaths.SequenceEqual(SkeletonAvatarMaskUtility.GetTransformPaths(BindingRoot)));
    }

    public bool TryValidate(out string error)
    {
        if (!sceneSnapshot)
            return SkeletonAvatarMaskUtility.TryValidate(this, out error);
        error = importedHierarchyError;
        if (!HierarchyMatches())
            error = "The skeleton changed. Refresh Hierarchy before generating the mask.";
        if (bones.Count == 0 || importedBindingPaths.Count == 0)
            error = "Assign a Skeleton Root GameObject to import its bones.";
        return string.IsNullOrEmpty(error);
    }

    public AvatarMask CreateMask()
    {
        if (!TryValidate(out string error))
            throw new InvalidOperationException(error);
        var mask = new AvatarMask { name = RootName + " Mask" };
        ApplyTo(mask);
        return mask;
    }

    public void ApplyTo(AvatarMask mask)
    {
        if (!sceneSnapshot)
        {
            SkeletonAvatarMaskUtility.ApplyTo(this, mask);
            return;
        }
        if (mask == null)
            throw new ArgumentNullException(nameof(mask));
        if (!TryValidate(out string error))
            throw new InvalidOperationException(error);
        var selections = new Dictionary<string, bool>(StringComparer.Ordinal);
        foreach (BoneSelection bone in bones)
        {
            string path = hierarchyPrefix.Length == 0 ? bone.Path
                : bone.Path.Length == 0 ? hierarchyPrefix : hierarchyPrefix + "/" + bone.Path;
            selections[path] = bone.Active;
        }
        var entries = importedBindingPaths.Select(path =>
            new BoneSelection(null, path, selections.TryGetValue(path, out bool active) && active)).ToList();
        SkeletonAvatarMaskUtility.ApplyEntriesTo(mask, entries);
    }
}
