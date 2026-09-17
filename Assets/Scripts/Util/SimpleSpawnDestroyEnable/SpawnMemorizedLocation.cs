using UnityEngine;

public class SpawnMemorizedLocation : SpawnByName
{
    private void Start()
    {
        if (MemoryBehaviour.Instance == null || MemoryBehaviour.Instance.StoryMemory == null)
        {
            Debug.LogWarning($"{nameof(SpawnMemorizedLocation)} on '{gameObject.name}' cannot spawn because story memory is not initialized.", this);
            return;
        }

        string currentLocation = MemoryBehaviour.Instance.StoryMemory.currentLocation;

        if (string.IsNullOrWhiteSpace(currentLocation))
        {
            Debug.LogWarning($"{nameof(SpawnMemorizedLocation)} on '{gameObject.name}' cannot spawn because current location is empty.", this);
            return;
        }

        DestroyAndSpawn(currentLocation);
    }
}
