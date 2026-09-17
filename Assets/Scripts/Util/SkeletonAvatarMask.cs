using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Authors a native AvatarMask from a GameObject hierarchy without an Avatar asset.</summary>
[AddComponentMenu("Animation/Skeleton Avatar Mask")]
public sealed class SkeletonAvatarMask : MonoBehaviour, ISkeletonAvatarMaskSource
{
    [Serializable]
    public sealed class BoneSelection
    {
        [SerializeField] private Transform bone;
        [SerializeField] private string path;
        [SerializeField] private bool active;

        public Transform Bone => bone;
        public string Path => path;
        public bool Active => active;

        public BoneSelection(Transform bone, string path, bool active)
        {
            this.bone = bone;
            this.path = path;
            this.active = active;
        }

        internal void SetActive(bool value) => active = value;
    }

    [SerializeField] private GameObject skeletonRoot;
    [SerializeField, HideInInspector] private List<BoneSelection> bones = new List<BoneSelection>();
    [SerializeField, HideInInspector] private AvatarMask generatedMask;
    [SerializeField, HideInInspector] private Transform hierarchyBindingRoot;
    [SerializeField, HideInInspector] private string hierarchyPrefix;

    public GameObject SkeletonRoot => skeletonRoot;
    public IReadOnlyList<BoneSelection> Bones => bones;
    public AvatarMask GeneratedMask => generatedMask;

    // Clip bindings are relative to the Animator, not necessarily the selected first bone.
    public Transform BindingRoot => SkeletonAvatarMaskUtility.GetBindingRoot(skeletonRoot);

    public void SetSkeletonRoot(GameObject root)
    {
        skeletonRoot = root;
        RefreshHierarchy();
    }

    public void SetGeneratedMask(AvatarMask mask) => generatedMask = mask;

    public void RefreshHierarchy()
    {
        bones = SkeletonAvatarMaskUtility.RefreshHierarchy(this);
        hierarchyBindingRoot = BindingRoot;
        hierarchyPrefix = SkeletonAvatarMaskUtility.GetSkeletonPrefix(this);
    }

    public void SetBoneActive(int index, bool active, bool includeChildren = false) =>
        SkeletonAvatarMaskUtility.SetBoneActive(bones, index, active, includeChildren);

    public void SetAllActive(bool active) => SkeletonAvatarMaskUtility.SetAllActive(bones, active);

    public bool HierarchyMatches() =>
        SkeletonAvatarMaskUtility.HierarchyMatches(this, hierarchyBindingRoot, hierarchyPrefix);

    public bool TryValidate(out string error) => SkeletonAvatarMaskUtility.TryValidate(this, out error);

    public AvatarMask CreateMask() => SkeletonAvatarMaskUtility.CreateMask(this);

    public void ApplyTo(AvatarMask mask) => SkeletonAvatarMaskUtility.ApplyTo(this, mask);
}
