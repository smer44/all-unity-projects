using UnityEngine;
using UnityEngine.Serialization;

public class PrefabSpawner : MonoBehaviour
{
    [SerializeField] private GameObject prefabToSpawn;
    [SerializeField] private Transform spawnPoint;
    [FormerlySerializedAs("fablerMemoryKey")]
    [SerializeField] private string memoryKey;
    [FormerlySerializedAs("requiredFablerMemoryValue")]
    [SerializeField] private int requiredMemoryValue;

    public void SpawnPrefab()
    {
        if (prefabToSpawn == null)
        {
            Debug.LogWarning("Prefab is not assigned.");
            return;
        }

        Transform parent = spawnPoint != null ? spawnPoint : transform;
        Instantiate(prefabToSpawn, parent);
    }

    public void SpawnPrefabIfMemory()
    {
        if (string.IsNullOrWhiteSpace(memoryKey))
        {
            Debug.LogWarning($"{nameof(PrefabSpawner)} on '{gameObject.name}' has no memory key assigned.", this);
            return;
        }

        if (!MemoryBehaviour.TryGet(memoryKey, out int memorizedValue))
        {
            return;
        }

        if (memorizedValue == requiredMemoryValue)
        {
            SpawnPrefab();
        }
    }
}
