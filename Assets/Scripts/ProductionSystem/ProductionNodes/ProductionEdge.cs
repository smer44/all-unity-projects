using System;

/// <summary>
/// A planned flow from OutgoingNode (supplier) to IncomingNode (consumer).
/// SupplyQuantity is the requested amount per TimeIntervalSeconds, not available capacity.
/// </summary>
public sealed class ProductionEdge
{
    public ProductionNode OutgoingNode { get; }
    public ProductionNode IncomingNode { get; }
    public Recipe Recipe { get; }
    public ItemDefinition Item { get; }
    public float SupplyQuantity { get; }
    public float TimeIntervalSeconds { get; }
    public float SupplyPerSecond => SupplyQuantity / TimeIntervalSeconds;

    public ProductionEdge(ProductionNode outgoingNode, Recipe recipe, ItemDefinition item, ProductionGoal goal)
    {
        if (outgoingNode == null)
            throw new ArgumentNullException(nameof(outgoingNode));
        if (recipe == null)
            throw new ArgumentNullException(nameof(recipe));
        if (item == null)
            throw new ArgumentNullException(nameof(item));
        if (goal == null)
            throw new ArgumentNullException(nameof(goal));
        if (item.ItemType != goal.ItemType || (goal.RequiredItem != null && item != goal.RequiredItem))
            throw new ArgumentException("The supplied item must match the goal.", nameof(item));

        OutgoingNode = outgoingNode;
        IncomingNode = goal.IncomingNode;
        Recipe = recipe;
        Item = item;
        SupplyQuantity = goal.SupplyQuantity;
        TimeIntervalSeconds = goal.TimeIntervalSeconds;
    }
}
