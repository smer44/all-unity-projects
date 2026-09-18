using UnityEngine;

public sealed class GridInventoryUIController : AbstractInventoryUIController
{
    [SerializeField] private GridUI gridInventoryPanel;

    protected override string MemoryPrefix => MemoryForUnitOfInventory.inventoryPrefix;

    public override bool AddInventoryEntry(AbstractMemoryEntry entry)
    {
        return entry is InventoryEntry inventoryEntry && inventoryEntry != null
            && gridInventoryPanel != null && gridInventoryPanel.AddInventoryEntry(inventoryEntry);
    }

    public override void ClearInventoryPanel()
    {
        if (gridInventoryPanel != null)
            gridInventoryPanel.ClearInventoryEntries();
    }
}
