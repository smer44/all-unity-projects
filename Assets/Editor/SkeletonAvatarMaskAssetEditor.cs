using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SkeletonAvatarMaskAsset))]
public sealed class SkeletonAvatarMaskAssetEditor : SkeletonAvatarMaskEditor
{
}

/// <summary>Reconnects imported scene inputs without serializing scene object references in assets.</summary>
public static class SkeletonAvatarMaskSceneInput
{
    public static void Assign(SkeletonAvatarMaskAsset asset, GameObject root)
    {
        asset.SetSkeletonRoot(root);
        if (asset.HasSceneSnapshot)
            asset.SetSceneRootId(GlobalObjectId.GetGlobalObjectIdSlow(root).ToString());
    }

    public static void Restore(SkeletonAvatarMaskAsset asset)
    {
        if (!asset.HasSceneSnapshot || asset.SkeletonRoot != null || string.IsNullOrEmpty(asset.SceneRootId))
            return;
        if (GlobalObjectId.TryParse(asset.SceneRootId, out GlobalObjectId id))
            asset.RestoreSceneRoot(GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id) as GameObject);
    }
}
