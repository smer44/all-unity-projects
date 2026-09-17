using System;
using System.Collections.Generic;
using UnityEngine;

public class ProductionGraphController : MonoBehaviour
{
    [SerializeField] public List<AmountOf<ItemType>> Goals = new();
    [SerializeField] public ProductionNode OutputStorage;
    [SerializeField] public ProductionOverview ProductionOverview;

    public ProductionGraph Graph { get; } = new();

    /// <summary>
    /// Plans an existing Goals entry as a requested flow into OutputStorage.
    /// Amount is interpreted per the supplied interval; existing stock is not subtracted.
    /// </summary>
    public List<ProductionGoal> BackwardsProductionStepNaive(
        ProductionGraph graphs, AmountOf<ItemType> goal, float timeIntervalSeconds = 1f)
    {
        if (goal == null)
            throw new ArgumentNullException(nameof(goal));
        if (OutputStorage == null)
            throw new InvalidOperationException("OutputStorage must reference a production node before planning final goals.");

        return BackwardsProductionStepNaive(graphs,
            new ProductionGoal(OutputStorage, goal.Item, goal.Amount, timeIntervalSeconds));
    }

    /// <summary>
    /// Adds one supplier-to-consumer edge for the requested flow and returns the
    /// supplier recipe's input goals for the next layer. Does not recurse or execute work.
    /// Lowest metric wins; ties keep the first node/recipe in candidate order.
    /// Throws without changing the graph if no supplier exists or its inputs are invalid.
    /// Calls are additive: repeating a goal adds another demand, not an update.
    /// </summary>
    public List<ProductionGoal> BackwardsProductionStepNaive(ProductionGraph graphs, ProductionGoal goal)
    {
        if (graphs == null)
            throw new ArgumentNullException(nameof(graphs));
        if (goal == null)
            throw new ArgumentNullException(nameof(goal));
        if (goal.IncomingNode == null)
            throw new ArgumentException("The goal's consumer must still exist.", nameof(goal));

        if (!TrySelectSupplier(goal, out ProductionEdge edge, out float outputPerExecution))
            throw new InvalidOperationException($"No supplier found for '{goal.ItemType}' at '{goal.IncomingNode.name}'.");

        List<ProductionGoal> previousGoals = CreateInputGoals(edge, outputPerExecution);
        graphs.AddEdge(edge);
        return previousGoals;
    }

    /// <summary>
    /// Lower is better. Override this metric to include other supplier costs later.
    /// </summary>
    public virtual float ProductionEdgeMetric(ProductionEdge edge)
    {
        if (edge == null)
            throw new ArgumentNullException(nameof(edge));

        return Vector3.Distance(edge.OutgoingNode.transform.position, edge.IncomingNode.transform.position);
    }

    private bool TrySelectSupplier(ProductionGoal goal, out ProductionEdge bestEdge, out float outputPerExecution)
    {
        bestEdge = null;
        outputPerExecution = 0f;
        if (ProductionOverview == null ||
            !ProductionOverview.ProducersByOutput.TryGetValue(goal.ItemType, out List<ProductionNode> producers) || producers == null)
            return false;

        float bestMetric = float.PositiveInfinity;
        foreach (ProductionNode supplier in producers)
        {
            if (supplier == null || supplier.AvailableRecipes == null)
                continue;

            foreach (Recipe recipe in supplier.AvailableRecipes)
            {
                if (!TryGetRecipeOutput(recipe, goal, out ItemDefinition item, out float quantity))
                    continue;

                ProductionEdge candidate = new ProductionEdge(supplier, recipe, item, goal);
                float metric = ProductionEdgeMetric(candidate);
                if (float.IsNaN(metric) || float.IsInfinity(metric) || metric >= bestMetric)
                    continue;

                bestMetric = metric;
                bestEdge = candidate;
                outputPerExecution = quantity;
            }
        }

        return bestEdge != null;
    }

    private static bool TryGetRecipeOutput(
        Recipe recipe, ProductionGoal goal, out ItemDefinition item, out float quantity)
    {
        item = null;
        quantity = 0f;
        if (recipe == null || recipe.Outputs == null)
            return false;

        foreach (ItemAmount output in recipe.Outputs)
        {
            if (!ProductionOverview.IsUsableOutput(output) || output.Item.ItemType != goal.ItemType)
                continue;
            if (goal.RequiredItem != null && output.Item != goal.RequiredItem)
                continue;

            // Pick one exact definition even for type goals, matching storage/transport semantics.
            if (item == null)
                item = output.Item;
            if (output.Item == item)
                quantity += output.Amount;
        }

        return item != null && quantity > 0f && !float.IsInfinity(quantity);
    }

    private static List<ProductionGoal> CreateInputGoals(ProductionEdge edge, float outputPerExecution)
    {
        if (edge.Recipe.Inputs == null)
            throw new InvalidOperationException($"Recipe '{edge.Recipe.DisplayName}' has no input list.");

        Dictionary<ItemDefinition, float> inputs = new();
        foreach (ItemAmount input in edge.Recipe.Inputs)
        {
            if (input == null || input.Item == null || input.Item.ItemType == ItemType.None ||
                input.Amount < 0f || float.IsNaN(input.Amount) || float.IsInfinity(input.Amount))
                throw new InvalidOperationException($"Recipe '{edge.Recipe.DisplayName}' has an invalid input.");
            if (input.Amount == 0f)
                continue;

            inputs.TryGetValue(input.Item, out float quantity);
            inputs[input.Item] = quantity + input.Amount;
        }

        List<ProductionGoal> goals = new();
        foreach (var input in inputs)
        {
            // Continuous flow ratios, not rounded batches. Labor, timing and capacity
            // feasibility belong to a later scheduling step.
            float requiredQuantity = (float)((double)input.Value * edge.SupplyQuantity / outputPerExecution);
            goals.Add(new ProductionGoal(edge.OutgoingNode, input.Key, requiredQuantity, edge.TimeIntervalSeconds));
        }

        return goals;
    }

    public void AddGoal(ItemType itemType, int amount)
    {
        if (amount <= 0)
            //
            return;


        Goals.Add(new AmountOf<ItemType>
        {
            Item = itemType,
            Amount = amount
        });
    }
}
