using System;
using System.Collections.Generic;

/// <summary>
/// Runtime adjacency indexes. Both indexes reference the same edge objects.
/// Use AddEdge to keep the outgoing and incoming indexes in sync.
/// </summary>
public sealed class ProductionGraph
{
    public Dictionary<ProductionNode, List<ProductionEdge>> OutgoingEdges { get; } = new();
    public Dictionary<ProductionNode, List<ProductionEdge>> IncomingEdges { get; } = new();

    public void AddEdge(ProductionEdge edge)
    {
        if (edge == null)
            throw new ArgumentNullException(nameof(edge));
        if (edge.OutgoingNode == null || edge.IncomingNode == null)
            throw new ArgumentException("Both edge endpoints must exist.", nameof(edge));

        AddToIndex(OutgoingEdges, edge.OutgoingNode, edge);
        AddToIndex(IncomingEdges, edge.IncomingNode, edge);
    }

    public void Clear()
    {
        OutgoingEdges.Clear();
        IncomingEdges.Clear();
    }

    private static void AddToIndex(
        Dictionary<ProductionNode, List<ProductionEdge>> index,
        ProductionNode node,
        ProductionEdge edge)
    {
        if (!index.TryGetValue(node, out List<ProductionEdge> edges))
        {
            edges = new List<ProductionEdge>();
            index.Add(node, edges);
        }

        if (!edges.Contains(edge))
            edges.Add(edge);
    }
}
