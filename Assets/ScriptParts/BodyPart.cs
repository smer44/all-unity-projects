using System;
using System.Collections.Generic;
using UnityEngine;

public class BodyPart : MonoBehaviour
{
    [Serializable]
    public class BodyPartSlot
    {
        public string bodyPartId;
        public Transform pivot;
    }

    [SerializeField] private List<BodyPartSlot> children = new List<BodyPartSlot>();

    private bool spawnedChildren;

    private void Start()
    {
        SpawnChildren();
    }

    public void SpawnChildren()
    {
        if (spawnedChildren)
        {
            return;
        }

        spawnedChildren = true;

        foreach (BodyPartSlot child in children)
        {
            if (child == null)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(child.bodyPartId))
            {
                Debug.LogWarning($"{name} has a body part slot without BodyPartSO id.", this);
                continue;
            }

            if (child.pivot == null)
            {
                Debug.LogWarning($"{name} has a body part slot without pivot for '{child.bodyPartId}'.", this);
                continue;
            }

            if (!BodyPartSO.TryGetById(child.bodyPartId, out BodyPartSO bodyPart))
            {
                Debug.LogWarning($"{name} could not find BodyPartSO with id '{child.bodyPartId}'. Make sure the asset is loaded before spawning.", this);
                continue;
            }

            GameObject prefab = bodyPart.Prefab;
            if (prefab == null)
            {
                Debug.LogWarning($"{bodyPart.name} has no prefab assigned.", bodyPart);
                continue;
            }

            GameObject spawned = Instantiate(prefab, child.pivot, false);
            if (spawned.GetComponent<BodyPart>() == null)
            {
                Debug.LogWarning($"{prefab.name} was spawned by {name}, but its root has no BodyPart component.", spawned);
            }
        }
    }
}
