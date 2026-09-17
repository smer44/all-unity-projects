using UnityEngine;

public abstract class AbstractInventoryUIController : MonoBehaviour
{
    [SerializeField] private InventoryEntry[] inventoryEntries;
    private bool hasWarnedMissingMemoryBehaviour;

    protected virtual void Start()
    {
        RefreshInventoryDisplay();
    }

    protected virtual void OnEnable()
    {
        MemoryBehaviour.MemoryChanged += HandleMemoryChanged;

        if (!Application.isPlaying)
        {
            return;
        }

        if (MemoryBehaviour.Instance != null)
        {
            RefreshInventoryDisplay();
            return;
        }

        if (!hasWarnedMissingMemoryBehaviour)
        {
            Debug.LogWarning(
                $"{GetType().Name} cannot refresh from memory because {nameof(MemoryBehaviour)}.Instance is not initialized.",
                this);
            hasWarnedMissingMemoryBehaviour = true;
        }
    }

    protected virtual void OnDisable()
    {
        MemoryBehaviour.MemoryChanged -= HandleMemoryChanged;
    }

    /// <summary>
    /// Rebuilds the inventory UI from the current MemoryBehaviour values.
    /// Any entry whose configured key exists and has a non-zero value is displayed.
    /// </summary>
    public void RefreshInventoryDisplay()
    {
        ClearInventoryPanel();

        if (inventoryEntries == null)
        {
            return;
        }

        for (int i = 0; i < inventoryEntries.Length; i++)
        {
            InventoryEntry entry = inventoryEntries[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.key))
            {
                continue;
            }

            if (!MemoryBehaviour.TryGet(entry.key, out int value) || value == 0)
            {
                continue;
            }

            if (!AddInventoryEntry(entry))
            {
                Debug.LogWarning($"{GetType().Name} could not display inventory entry '{entry.key}'.", this);
                break;
            }
        }
    }

    public abstract void ClearInventoryPanel();

    public abstract bool AddInventoryEntry(InventoryEntry entry);

    private void HandleMemoryChanged(string key, int _)
    {
        RefreshInventoryDisplay();
        MemoryBehaviour.LogMemoryContents();

    }
}
