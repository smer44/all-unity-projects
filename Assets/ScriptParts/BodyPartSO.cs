using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "BodyPart", menuName = "Script Parts/Body Part")]
public class BodyPartSO : ScriptableObject
{
    private const string ResourcesPath = "BodyParts";

    private static readonly Dictionary<string, BodyPartSO> BodyPartsById = new Dictionary<string, BodyPartSO>();
    private static bool loadedResources;

    [SerializeField] private string id;
    [SerializeField] private GameObject prefab;

    public string Id => id;
    public GameObject Prefab => prefab;

    private void OnEnable()
    {
        Register();
    }

    private void OnDisable()
    {
        if (!string.IsNullOrWhiteSpace(id) &&
            BodyPartsById.TryGetValue(id, out BodyPartSO existingBodyPart) &&
            existingBodyPart == this)
        {
            BodyPartsById.Remove(id);
        }
    }

    public void Register()
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            Debug.LogWarning($"{name} has no BodyPartSO id.", this);
            return;
        }

        if (BodyPartsById.TryGetValue(id, out BodyPartSO existingBodyPart) && existingBodyPart != this)
        {
            Debug.LogWarning($"Duplicate BodyPartSO id '{id}' on {name}. Existing asset: {existingBodyPart.name}.", this);
            return;
        }

        BodyPartsById[id] = this;
    }

    public static bool TryGetById(string id, out BodyPartSO bodyPart)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            bodyPart = null;
            return false;
        }

        if (!BodyPartsById.ContainsKey(id))
        {
            LoadResources();
        }

        return BodyPartsById.TryGetValue(id, out bodyPart);
    }

    private static void LoadResources()
    {
        if (loadedResources)
        {
            return;
        }

        loadedResources = true;
        BodyPartSO[] bodyParts = Resources.LoadAll<BodyPartSO>(ResourcesPath);
        foreach (BodyPartSO bodyPart in bodyParts)
        {
            bodyPart.Register();
        }
    }
}
