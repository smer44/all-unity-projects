using System;
using System.Collections.Generic;
using UnityEngine;

public class SpawnByName : MonoBehaviour
{
    [Serializable]
    private struct NamedPrefab
    {
        public string name;
        public GameObject prefab;
    }

    [SerializeField] private NamedPrefab[] prefabs;
    [SerializeField] private Transform spawnRoot;

    private readonly Dictionary<string, GameObject> prefabByName = new Dictionary<string, GameObject>();

    private void Awake()
    {
        prefabByName.Clear();

        if (prefabs == null)
            return;

        for (int i = 0; i < prefabs.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(prefabs[i].name))
                continue;

            if (prefabByName.ContainsKey(prefabs[i].name))
            {
                Debug.LogWarning($"{nameof(SpawnByName)} on '{gameObject.name}' has duplicate prefab name '{prefabs[i].name}'.", this);
                continue;
            }

            prefabByName.Add(prefabs[i].name, prefabs[i].prefab);
        }
    }

    public void DestroyAndSpawn(string name)
    {
        if (spawnRoot == null)
        {
            Debug.LogWarning($"{nameof(SpawnByName)} on '{gameObject.name}' has no spawn root assigned.", this);
            return;
        }

        ClearSpawnRoot();

        if (!prefabByName.TryGetValue(name, out GameObject prefab))
        {
            Debug.LogWarning($"{nameof(SpawnByName)} on '{gameObject.name}' has no prefab registered with name '{name}'.", this);
            return;
        }

        if (prefab == null)
        {
            Debug.LogWarning($"{nameof(SpawnByName)} on '{gameObject.name}' has null prefab registered with name '{name}'.", this);
            return;
        }

        Debug.Log("Spawning {prefab}");
        Instantiate(prefab, spawnRoot);
    }

    private void ClearSpawnRoot()
    {
        for (int i = spawnRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(spawnRoot.GetChild(i).gameObject);
        }
    }
}
