using System;

/// <summary>
/// A requested goods flow into a production node over a given interval.
/// Type goals accept any matching definition; recipe inputs require an exact definition.
/// </summary>
public sealed class ProductionGoal
{
    public ProductionNode IncomingNode { get; }
    public ItemType ItemType { get; }
    public ItemDefinition RequiredItem { get; }
    public float SupplyQuantity { get; }
    public float TimeIntervalSeconds { get; }

    public ProductionGoal(
        ProductionNode incomingNode,
        ItemType itemType,
        float supplyQuantity,
        float timeIntervalSeconds = 1f)
    {
        if (incomingNode == null)
            throw new ArgumentNullException(nameof(incomingNode));
        if (itemType == ItemType.None)
            throw new ArgumentException("A production goal must request an item type.", nameof(itemType));
        ValidatePositiveFinite(supplyQuantity, nameof(supplyQuantity));
        ValidatePositiveFinite(timeIntervalSeconds, nameof(timeIntervalSeconds));

        IncomingNode = incomingNode;
        ItemType = itemType;
        SupplyQuantity = supplyQuantity;
        TimeIntervalSeconds = timeIntervalSeconds;
    }

    public ProductionGoal(
        ProductionNode incomingNode,
        ItemDefinition requiredItem,
        float supplyQuantity,
        float timeIntervalSeconds = 1f)
        : this(incomingNode, GetItemType(requiredItem), supplyQuantity, timeIntervalSeconds)
    {
        RequiredItem = requiredItem;
    }

    private static ItemType GetItemType(ItemDefinition item)
    {
        if (item == null)
            throw new ArgumentNullException(nameof(item));

        return item.ItemType;
    }

    private static void ValidatePositiveFinite(float value, string parameterName)
    {
        if (value <= 0f || float.IsNaN(value) || float.IsInfinity(value))
            throw new ArgumentOutOfRangeException(parameterName, "The value must be positive and finite.");
    }
}
