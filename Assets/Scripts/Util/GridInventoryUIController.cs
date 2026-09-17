using UnityEngine;

public sealed class GridInventoryUIController : AbstractInventoryUIController
{
    [SerializeField] private GridUI gridInventoryPanel;

    public override bool AddInventoryEntry(InventoryEntry entry)
    {
        return gridInventoryPanel != null && gridInventoryPanel.AddInventoryEntry(entry);
    }

    public override void ClearInventoryPanel()
    {
        if (gridInventoryPanel != null)
            gridInventoryPanel.ClearInventoryEntries();
    }
}
