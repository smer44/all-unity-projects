using System;
using System.Collections.Generic;
using UnityEngine;
using BoneSelection = SkeletonAvatarMask.BoneSelection;

/// <summary>Common authoring API for scene components and Project mask assets.</summary>
public interface ISkeletonAvatarMaskSource
{
    GameObject SkeletonRoot { get; }
    IReadOnlyList<BoneSelection> Bones { get; }
    AvatarMask GeneratedMask { get; }
    Transform BindingRoot { get; }
    void SetSkeletonRoot(GameObject root);
    void SetGeneratedMask(AvatarMask mask);
    void RefreshHierarchy();
    void SetBoneActive(int index, bool active, bool includeChildren = false);
    void SetAllActive(bool active);
    bool HierarchyMatches();
    bool TryValidate(out string error);
    AvatarMask CreateMask();
    void ApplyTo(AvatarMask mask);
}

internal static class SkeletonAvatarMaskUtility
{
    internal static Transform GetBindingRoot(GameObject skeletonRoot)
    {
        if (skeletonRoot == null)
            return null;
        Animator animator = skeletonRoot.GetComponentInParent<Animator>(true);
        return animator != null ? animator.transform : skeletonRoot.transform;
    }

    internal static List<BoneSelection> RefreshHierarchy(ISkeletonAvatarMaskSource source)
    {
        var byBone = new Dictionary<Transform, bool>();
        var byPath = new Dictionary<string, bool>(StringComparer.Ordinal);
        foreach (BoneSelection selection in source.Bones)
        {
            if (selection.Bone != null)
                byBone[selection.Bone] = selection.Active;
            byPath[selection.Path] = selection.Active;
        }

        var refreshed = new List<BoneSelection>();
        if (source.SkeletonRoot != null)
            CollectSelections(source.SkeletonRoot.transform, "", true, byBone, byPath, refreshed);
        return refreshed;
    }

    private static void CollectSelections(Transform bone, string path, bool inheritedActive,
        Dictionary<Transform, bool> byBone, Dictionary<string, bool> byPath, List<BoneSelection> result)
    {
        if (!byBone.TryGetValue(bone, out bool active) && !byPath.TryGetValue(path, out active))
            active = inheritedActive;
        result.Add(new BoneSelection(bone, path, active));
        for (int i = 0; i < bone.childCount; i++)
        {
            Transform child = bone.GetChild(i);
            string childPath = string.IsNullOrEmpty(path) ? child.name : path + "/" + child.name;
            CollectSelections(child, childPath, active, byBone, byPath, result);
        }
    }

    internal static void SetBoneActive(IReadOnlyList<BoneSelection> bones, int index, bool active, bool includeChildren)
    {
        BoneSelection selected = bones[index];
        selected.SetActive(active);
        if (!includeChildren)
            return;
        string prefix = selected.Path + "/";
        foreach (BoneSelection bone in bones)
        {
            if (selected.Path.Length == 0 || bone.Path.StartsWith(prefix, StringComparison.Ordinal))
                bone.SetActive(active);
        }
    }

    internal static void SetAllActive(IReadOnlyList<BoneSelection> bones, bool active)
    {
        foreach (BoneSelection bone in bones)
            bone.SetActive(active);
    }

    internal static bool HierarchyMatches(ISkeletonAvatarMaskSource source, Transform bindingRoot, string prefix,
        bool compareTransforms = true)
    {
        if (source.SkeletonRoot == null)
            return source.Bones.Count == 0;
        if ((compareTransforms && bindingRoot != source.BindingRoot) || prefix != GetSkeletonPrefix(source))
            return false;
        int index = 0;
        return Matches(source.Bones, source.SkeletonRoot.transform, "", ref index, compareTransforms)
            && index == source.Bones.Count;
    }

    internal static string GetSkeletonPrefix(ISkeletonAvatarMaskSource source)
    {
        if (source.SkeletonRoot == null)
            return "";
        var names = new Stack<string>();
        Transform bindingRoot = source.BindingRoot;
        for (Transform bone = source.SkeletonRoot.transform; bone != bindingRoot; bone = bone.parent)
            names.Push(bone.name);
        return string.Join("/", names);
    }

    private static bool Matches(IReadOnlyList<BoneSelection> bones, Transform bone, string path, ref int index,
        bool compareTransforms)
    {
        if (index >= bones.Count || (compareTransforms && bones[index].Bone != bone) || bones[index].Path != path)
            return false;
        index++;
        for (int i = 0; i < bone.childCount; i++)
        {
            Transform child = bone.GetChild(i);
            if (!Matches(bones, child, path.Length == 0 ? child.name : path + "/" + child.name, ref index, compareTransforms))
                return false;
        }
        return true;
    }

    internal static bool TryValidate(ISkeletonAvatarMaskSource source, out string error)
    {
        if (source.SkeletonRoot == null)
        {
            error = "Assign a Skeleton Root GameObject to import its bones.";
            return false;
        }
        if (!source.HierarchyMatches())
        {
            error = "The skeleton changed. Refresh Hierarchy before generating the mask.";
            return false;
        }
        return ValidateHierarchy(source.BindingRoot, out error);
    }

    internal static bool ValidateHierarchy(Transform root, out string error) =>
        ValidatePaths(root, "", new HashSet<string>(StringComparer.Ordinal), out error);

    internal static List<string> GetTransformPaths(Transform root)
    {
        var paths = new List<string>();
        CollectPaths(root, "", paths);
        return paths;
    }

    private static void CollectPaths(Transform bone, string path, List<string> paths)
    {
        paths.Add(path);
        for (int i = 0; i < bone.childCount; i++)
        {
            Transform child = bone.GetChild(i);
            CollectPaths(child, path.Length == 0 ? child.name : path + "/" + child.name, paths);
        }
    }

    private static bool ValidatePaths(Transform bone, string path, HashSet<string> paths, out string error)
    {
        if (!paths.Add(path))
        {
            error = "Duplicate animation path '" + path + "'. Give sibling bones unique names.";
            return false;
        }
        for (int i = 0; i < bone.childCount; i++)
        {
            Transform child = bone.GetChild(i);
            if (child.name.Length == 0 || child.name.Contains("/"))
            {
                error = "Bone names must be non-empty and cannot contain '/': '" + child.name + "'.";
                return false;
            }
            if (!ValidatePaths(child, path.Length == 0 ? child.name : path + "/" + child.name, paths, out error))
                return false;
        }
        error = null;
        return true;
    }

    internal static AvatarMask CreateMask(ISkeletonAvatarMaskSource source)
    {
        if (!source.TryValidate(out string error))
            throw new InvalidOperationException(error);
        var mask = new AvatarMask { name = source.SkeletonRoot.name + " Mask" };
        ApplyTo(source, mask);
        return mask;
    }

    internal static void ApplyTo(ISkeletonAvatarMaskSource source, AvatarMask mask)
    {
        if (mask == null)
            throw new ArgumentNullException(nameof(mask));
        // Validate before touching an existing asset so invalid hierarchies cannot corrupt it.
        if (!source.TryValidate(out string error))
            throw new InvalidOperationException(error);

        var selections = new Dictionary<Transform, bool>();
        foreach (BoneSelection bone in source.Bones)
            selections[bone.Bone] = bone.Active;
        var entries = new List<BoneSelection>();
        CollectMaskEntries(source.BindingRoot, "", selections, entries);
        ApplyEntriesTo(mask, entries);
    }

    internal static void ApplyEntriesTo(AvatarMask mask, IReadOnlyList<BoneSelection> entries)
    {
        for (int i = 0; i < (int)AvatarMaskBodyPart.LastBodyPart; i++)
            mask.SetHumanoidBodyPartActive((AvatarMaskBodyPart)i, false);
        mask.transformCount = entries.Count;
        for (int i = 0; i < entries.Count; i++)
        {
            mask.SetTransformPath(i, entries[i].Path);
            mask.SetTransformActive(i, entries[i].Active);
        }
    }

    private static void CollectMaskEntries(Transform bone, string path,
        Dictionary<Transform, bool> selections, List<BoneSelection> result)
    {
        // Explicitly exclude ancestors and siblings outside the chosen skeleton.
        result.Add(new BoneSelection(bone, path, selections.TryGetValue(bone, out bool active) && active));
        for (int i = 0; i < bone.childCount; i++)
        {
            Transform child = bone.GetChild(i);
            CollectMaskEntries(child, path.Length == 0 ? child.name : path + "/" + child.name, selections, result);
        }
    }
}
