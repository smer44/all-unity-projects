using UnityEngine;

public abstract class AbstractInventoryUIController : MonoBehaviour
{
    [SerializeField] private InventoryOfUnit inventoryOfUnit;
    private bool hasWarnedMissingMemoryBehaviour;
    
    protected abstract string MemoryPrefix { get; }

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
    /// Displays the assigned unit's memory list. Subclasses choose the list prefix
    /// and which entry types they can display.
    /// </summary>
    public void RefreshInventoryDisplay()
    {
        ClearInventoryPanel();

        if (inventoryOfUnit == null || string.IsNullOrWhiteSpace(inventoryOfUnit.inventoryKey))
        {
            return;
        }
        var memoryBehaviour = MemoryBehaviour.Instance;
        if (memoryBehaviour == null || memoryBehaviour.StoryMemory == null)
            return;

        string listKey = MemoryPrefix + inventoryOfUnit.inventoryKey;
        var lists = memoryBehaviour.StoryMemory.memoryForUnitDict;
        if (lists == null || !lists.TryGetValue(listKey, out EntryListForUnit list)
            || list == null || list.entries == null)
            return;

        foreach (AbstractMemoryEntry entry in list.entries)
        {
            if (entry != null)
                AddInventoryEntry(entry);
        }
    }

    public abstract void ClearInventoryPanel();

    public abstract bool AddInventoryEntry(AbstractMemoryEntry entry);

    private void HandleMemoryChanged(string key, int _)
    {
        RefreshInventoryDisplay();
        MemoryBehaviour.LogMemoryContents();
    }
}
