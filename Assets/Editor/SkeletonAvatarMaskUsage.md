# Skeleton Avatar Mask

## Create and use a mask

1. In the **Project** window, right-click and choose **Create > Animation > Skeleton Avatar Mask**. This creates an authoring `.asset` with the bone-selection Inspector.
2. Drag the skeleton GameObject into **Skeleton Root**. Scene objects are supported: for the player, drag **Player > MarkusVisuals > Armature** from the Hierarchy. Prefab/model GameObjects and their bones are also supported.
3. Use the bone checkboxes to include or exclude animation on individual transforms. By default a toggle also changes that bone's descendants; turn off **Bone toggles also affect children** for independent selections.
4. For an upper-body mask, choose **Exclude All**, then enable the spine branch. Keep the hips and legs excluded. Search and foldouts help navigate larger skeletons.
5. Click **Create Mask Asset...** and choose a location under `Assets`.
6. Drag the generated `.mask` asset from the Project window into the Animator layer's **Mask** field. **Show in Project** locates it. For an upper-body attack layer, use **Override** blending and an appropriate layer weight.
7. Further bone changes update the same native mask asset, preserving Animator layer references. **Update and Save Mask** explicitly saves it. Save the project to retain the authoring asset and its selections.

The root object is the only required input, and inactive bones are included. No Humanoid Avatar is required. The authoring `.asset` stores your setup; the generated `.mask` is what you assign to the Animator layer.

### Scene skeletons

Dropping a scene GameObject imports its actual hierarchy, including bones added to that scene instance. The asset saves bone paths and selections, so you can keep editing or generating masks after closing the scene. With a saved source scene open, the Inspector reconnects the root after script or asset reloads. An unsaved scene's input may need to be dropped again after reloading; the imported bone selections remain saved in the asset.

When the source is unavailable, the Inspector shows the imported skeleton name and saved bone tree. Reopen its scene or drop the root again to refresh the hierarchy. **Clear Imported Skeleton** removes a disconnected import.

### Component workflow

Select a scene GameObject and use the Inspector's **Add Component > Animation > Skeleton Avatar Mask**. Assign a scene or prefab skeleton, then follow steps 3–6 above. Save the scene or prefab to retain that component's selections.

**Add Component** and the Project window's **Create** are separate Unity menus. `SkeletonAvatarMask` registers the component; `SkeletonAvatarMaskAsset` registers the Project asset. Scene inputs in Project assets are stored as imported paths plus an editor scene identifier, rather than direct scene object references.

## Animation paths

Unity matches transform mask paths to animation bindings relative to the Animator's GameObject. Both authoring types automatically find the nearest Animator on or above the selected skeleton root. For example, choosing `Hips` beneath an Animator produces paths such as `Hips/Spine/Hand`, not merely `Spine/Hand` and not absolute scene paths.

Without an Animator ancestor, the selected Skeleton Root becomes the path root. Use that same object as the root for animation playback. The Inspector displays the effective animation path root.

Transforms outside the selected skeleton, including its ancestors and sibling branches under the Animator, receive excluded entries. The root transform itself has an empty binding path. Toggling a parent independently does not automatically disable its children; branch propagation is an authoring convenience controlled by the checkbox above the tree.

**Refresh Hierarchy** preserves choices for surviving transform references in component/prefab workflows. Scene imports and replacement skeletons preserve choices by relative path. New bones inherit their parent's selection. Hierarchy changes are also refreshed when the Inspector redraws. Duplicate paths, empty bone names, and names containing `/` are rejected because they cannot be addressed unambiguously by animation bindings.

## Why separate authoring and output assets

Unity's `AvatarMask` has two independent kinds of data: Humanoid body-part flags and transform paths with enabled/disabled weights. The built-in Inspector's skeleton-import picker uses an Avatar/ModelImporter to populate those paths. It cannot import an arbitrary GameObject hierarchy through that picker.

`AvatarMask` is sealed, and `AnimatorControllerLayer.avatarMask` requires that native type. A MonoBehaviour or unrelated ScriptableObject cannot occupy the layer's Mask field. This tool therefore provides a custom authoring Inspector and exports a real `AvatarMask`; the generated asset works directly with Unity's Animator layers and does not need the authoring component at playback time.

These masks target transform-keyed Generic or non-Humanoid clips. Humanoid muscle animation uses Humanoid body-part masking instead. The output disables Humanoid body-part flags and explicitly writes the transform selections.

## Verification

Editor tests cover the registered Project Create menu, both custom Inspectors, authoring asset persistence, native Animator-layer playback with no Avatar and with a Generic Avatar, matching the existing player sword/gun clip paths, inactive bones, branch toggles, refresh, validation, Undo, and preserving layer references during updates.

References: [Unity AvatarMask API](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/AvatarMask.html), [Unity Avatar Mask Inspector](https://docs.unity3d.com/6000.4/Documentation/Manual/class-AvatarMask.html), [Unity C# AvatarMask binding](https://github.com/Unity-Technologies/UnityCsReference/blob/master/Modules/Animation/ScriptBindings/AvatarMask.bindings.cs), [Unity C# AvatarMask Inspector](https://github.com/Unity-Technologies/UnityCsReference/blob/master/Editor/Mono/Inspector/AvatarMaskInspector.cs).
