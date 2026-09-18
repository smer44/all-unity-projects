using UnityEngine;

[CreateAssetMenu(fileName = "MemoryForUnitOfInventory", menuName = "Inventory/MemoryForUnitOfInventory")]
public class MemoryForUnitOfInventory : EntryListForUnit
{
    public const string inventoryPrefix = "Inventory_";

    public override string MemoryType()
    {
        return inventoryPrefix;
    }
}
