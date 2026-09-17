using System.Collections.Generic;
using UnityEngine;

public abstract class AbstractInteractable : MonoBehaviour
{
    private readonly Dictionary<Transform, int> defaultLayersByTransform = new Dictionary<Transform, int>();

    public abstract void OnEnter(PlayerController player);
    public abstract void OnExit();
    public abstract void KillInteraction();

    public abstract bool AllowsBreak();

    public abstract void OnFocusEnter();

    public abstract void OnFocusExit();

    protected void CacheDefaultLayers()
    {
        defaultLayersByTransform.Clear();

        foreach (Transform childTransform in GetComponentsInChildren<Transform>(true))
        {
            defaultLayersByTransform[childTransform] = childTransform.gameObject.layer;
        }
    }

    protected void ApplyLayerToHierarchy(int layer)
    {
        foreach (Transform childTransform in GetComponentsInChildren<Transform>(true))
        {
            childTransform.gameObject.layer = layer;
        }
    }

    protected void RestoreDefaultLayers()
    {
        if (defaultLayersByTransform.Count == 0)
        {
            CacheDefaultLayers();
        }

        foreach (var defaultLayerByTransform in defaultLayersByTransform)
        {
            if (defaultLayerByTransform.Key != null)
            {
                defaultLayerByTransform.Key.gameObject.layer = defaultLayerByTransform.Value;
            }
        }
    }
}
