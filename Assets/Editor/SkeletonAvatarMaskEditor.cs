using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

[CustomEditor(typeof(SkeletonAvatarMask))]
public class SkeletonAvatarMaskEditor : Editor
{
    private readonly HashSet<string> collapsedPaths = new HashSet<string>(StringComparer.Ordinal);
    private Vector2 scroll;
    private string search = "";
    private bool includeChildren = true;
    private string operationError;

    private ISkeletonAvatarMaskSource Source => (ISkeletonAvatarMaskSource)target;

    protected virtual void OnEnable()
    {
        EditorApplication.hierarchyChanged += Repaint;
        Undo.undoRedoPerformed += Repaint;
    }

    protected virtual void OnDisable()
    {
        EditorApplication.hierarchyChanged -= Repaint;
        Undo.undoRedoPerformed -= Repaint;
    }

    public override void OnInspectorGUI()
    {
        bool assetSource = target is SkeletonAvatarMaskAsset;
        var maskAsset = target as SkeletonAvatarMaskAsset;
        if (maskAsset != null)
            SkeletonAvatarMaskSceneInput.Restore(maskAsset);
        EditorGUI.BeginChangeCheck();
        var root = (GameObject)EditorGUILayout.ObjectField(
            new GUIContent("Skeleton Root", "Drop a scene, prefab or model GameObject. No Avatar asset is required."),
            Source.SkeletonRoot, typeof(GameObject), assetSource || !EditorUtility.IsPersistent(target));
        if (EditorGUI.EndChangeCheck())
        {
            Change("Change mask skeleton", () =>
            {
                if (maskAsset != null)
                    SkeletonAvatarMaskSceneInput.Assign(maskAsset, root);
                else
                    Source.SetSkeletonRoot(root);
            });
            collapsedPaths.Clear();
        }

        if (!Source.HierarchyMatches())
            Change("Refresh mask hierarchy", Source.RefreshHierarchy);

        if (maskAsset != null && maskAsset.HasSceneSnapshot)
        {
            EditorGUILayout.LabelField("Imported skeleton", maskAsset.RootName);
            EditorGUILayout.HelpBox(Source.SkeletonRoot != null
                ? "The scene hierarchy is imported into this asset. Bone paths and selections remain available when the scene is closed."
                : "Using the saved hierarchy. Open the source scene or drop a Skeleton Root to refresh its bones.", MessageType.Info);
            if (Source.SkeletonRoot == null && GUILayout.Button("Clear Imported Skeleton"))
                Change("Clear mask skeleton", () => SkeletonAvatarMaskSceneInput.Assign(maskAsset, null));
        }

        if (Source.Bones.Count > 0)
        {
            Transform bindingRoot = Source.BindingRoot;
            EditorGUILayout.LabelField("Animation path root", bindingRoot != null ? bindingRoot.name : maskAsset.BindingRootName);
            if (bindingRoot != null)
                EditorGUILayout.HelpBox(bindingRoot.GetComponent<Animator>() != null
                    ? "Paths are relative to the nearest parent Animator. Bones outside the selected skeleton are excluded."
                    : "No parent Animator found: paths are relative to Skeleton Root. Use the same hierarchy root for animation playback.",
                    MessageType.Info);

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(Source.SkeletonRoot == null))
                    if (GUILayout.Button("Refresh Hierarchy"))
                        Change("Refresh mask hierarchy", Source.RefreshHierarchy);
                if (GUILayout.Button("Include All"))
                    Change("Include all mask bones", () => Source.SetAllActive(true));
                if (GUILayout.Button("Exclude All"))
                    Change("Exclude all mask bones", () => Source.SetAllActive(false));
            }

            includeChildren = EditorGUILayout.ToggleLeft("Bone toggles also affect children", includeChildren);
            search = EditorGUILayout.TextField("Search bones", search);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Expand All", EditorStyles.miniButton))
                    collapsedPaths.Clear();
                if (GUILayout.Button("Collapse All", EditorStyles.miniButton))
                {
                    foreach (var bone in Source.Bones)
                        collapsedPaths.Add(bone.Path);
                }
            }

            DrawHierarchy();
        }

        bool valid = Source.TryValidate(out string error);
        if (!valid)
            EditorGUILayout.HelpBox(error, Source.SkeletonRoot == null ? MessageType.Info : MessageType.Error);
        if (!string.IsNullOrEmpty(operationError))
            EditorGUILayout.HelpBox(operationError, MessageType.Error);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Animator Layer Mask", EditorStyles.boldLabel);
        if (Source.GeneratedMask != null)
        {
            // Display an output reference, not an input that could overwrite an unrelated mask.
            EditorGUILayout.ObjectField("Generated Mask", Source.GeneratedMask, typeof(AvatarMask), false);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Show in Project"))
                    EditorGUIUtility.PingObject(Source.GeneratedMask);
                using (new EditorGUI.DisabledScope(!valid))
                {
                    if (GUILayout.Button("Update and Save Mask"))
                    {
                        Change("Update skeleton mask", () => { });
                        AssetDatabase.SaveAssetIfDirty(Source.GeneratedMask);
                    }
                }
            }
        }

        using (new EditorGUI.DisabledScope(!valid))
        {
            if (GUILayout.Button(Source.GeneratedMask == null ? "Create Mask Asset..." : "Create Mask Copy..."))
                CreateAsset();
        }
        EditorGUILayout.HelpBox(
            "Assign the generated .mask asset to an Animator layer's Mask field. " +
            "Bone changes here update that asset. " +
            (assetSource ? "Save the project to retain this authoring setup. "
                : "Save the scene or prefab to retain this authoring setup. ") +
            "This is a transform mask for Generic or non-Humanoid animation.", MessageType.Info);
    }

    private void DrawHierarchy()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.MinHeight(100f), GUILayout.MaxHeight(420f));
        var bones = Source.Bones;
        bool searching = !string.IsNullOrWhiteSpace(search);
        for (int i = 0; i < bones.Count; i++)
        {
            var bone = bones[i];
            string label = bone.Bone != null ? bone.Bone.name
                : bone.Path.Length > 0 ? bone.Path.Substring(bone.Path.LastIndexOf('/') + 1)
                : target is SkeletonAvatarMaskAsset asset ? asset.RootName : "Skeleton Root";
            if (searching)
            {
                if (label.IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0
                    && bone.Path.IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
            }
            else if (HasCollapsedParent(bone.Path))
            {
                continue;
            }

            int depth = bone.Path.Length == 0 ? 0 : 1;
            foreach (char character in bone.Path)
                if (character == '/') depth++;
            Rect row = EditorGUILayout.GetControlRect();
            row.xMin += depth * 14f;
            bool hasChildren = i + 1 < bones.Count && (bone.Path.Length == 0
                || bones[i + 1].Path.StartsWith(bone.Path + "/", StringComparison.Ordinal));
            if (hasChildren && !searching)
            {
                bool expanded = !collapsedPaths.Contains(bone.Path);
                bool newExpanded = EditorGUI.Foldout(new Rect(row.x, row.y, 14f, row.height), expanded, GUIContent.none);
                if (newExpanded) collapsedPaths.Remove(bone.Path);
                else collapsedPaths.Add(bone.Path);
            }
            row.xMin += 16f;
            bool active = EditorGUI.Toggle(new Rect(row.x, row.y, 18f, row.height), bone.Active);
            if (active != bone.Active)
            {
                int index = i;
                Change("Toggle mask bone", () => Source.SetBoneActive(index, active, includeChildren));
            }
            row.xMin += 20f;
            EditorGUI.LabelField(row, new GUIContent(label, bone.Path.Length == 0 ? "Skeleton Root" : bone.Path));
        }
        EditorGUILayout.EndScrollView();
    }

    private bool HasCollapsedParent(string path)
    {
        if (path.Length == 0)
            return false;
        if (collapsedPaths.Contains(""))
            return true;
        int slash = path.LastIndexOf('/');
        while (slash >= 0)
        {
            if (collapsedPaths.Contains(path.Substring(0, slash)))
                return true;
            slash = slash > 0 ? path.LastIndexOf('/', slash - 1) : -1;
        }
        return false;
    }

    private void Change(string label, System.Action change)
    {
        operationError = null;
        Undo.RecordObjects(Source.GeneratedMask != null
            ? new Object[] { target, Source.GeneratedMask } : new Object[] { target }, label);
        change();
        EditorUtility.SetDirty(target);
        if (target is Component)
            PrefabUtility.RecordPrefabInstancePropertyModifications(target);
        if (Source.GeneratedMask != null && Source.TryValidate(out _))
        {
            Source.ApplyTo(Source.GeneratedMask);
            EditorUtility.SetDirty(Source.GeneratedMask);
        }
    }

    private void CreateAsset()
    {
        string path = EditorUtility.SaveFilePanelInProject("Create skeleton mask",
            (target is SkeletonAvatarMaskAsset asset ? asset.RootName : Source.SkeletonRoot.name) + " Mask",
            "mask", "Choose a location for the Animator-compatible mask.");
        if (string.IsNullOrEmpty(path))
            return;
        try
        {
            SkeletonAvatarMaskAssets.Create(Source, AssetDatabase.GenerateUniqueAssetPath(path));
            operationError = null;
            EditorGUIUtility.PingObject(Source.GeneratedMask);
        }
        catch (Exception exception)
        {
            operationError = exception.Message;
        }
    }
}

public static class SkeletonAvatarMaskAssets
{
    public static AvatarMask Create(ISkeletonAvatarMaskSource source, string assetPath)
    {
        var sourceObject = source as Object;
        if (sourceObject == null)
            throw new ArgumentNullException(nameof(source));
        if (string.IsNullOrEmpty(assetPath) || !assetPath.StartsWith("Assets/", StringComparison.Ordinal)
            || !assetPath.EndsWith(".mask", StringComparison.OrdinalIgnoreCase)
            || assetPath.Contains("/../") || !AssetDatabase.IsValidFolder(Path.GetDirectoryName(assetPath).Replace('\\', '/')))
            throw new ArgumentException("Choose a .mask path in an existing Assets folder.", nameof(assetPath));
        if (File.Exists(assetPath) || AssetDatabase.LoadMainAssetAtPath(assetPath) != null)
            throw new InvalidOperationException("An asset already exists at " + assetPath);

        AvatarMask mask = source.CreateMask();
        mask.name = Path.GetFileNameWithoutExtension(assetPath);
        try
        {
            AssetDatabase.CreateAsset(mask, assetPath);
        }
        catch
        {
            Object.DestroyImmediate(mask);
            throw;
        }
        Undo.RecordObject(sourceObject, "Assign generated skeleton mask");
        source.SetGeneratedMask(mask);
        EditorUtility.SetDirty(sourceObject);
        if (sourceObject is Component)
            PrefabUtility.RecordPrefabInstancePropertyModifications(sourceObject);
        if (EditorUtility.IsPersistent(sourceObject))
            AssetDatabase.SaveAssetIfDirty(sourceObject);
        AssetDatabase.SaveAssetIfDirty(mask);
        return mask;
    }
}
