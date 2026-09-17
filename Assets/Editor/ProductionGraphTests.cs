using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public class ProductionGraphTests
{
    private GameObject root;
    private ProductionGraphController controller;
    private ProductionOverview overview;
    private readonly List<Object> assets = new();

    [SetUp]
    public void SetUp()
    {
        root = new GameObject("Production graph tests");
        controller = root.AddComponent<ProductionGraphController>();
        overview = root.AddComponent<ProductionOverview>();
        controller.ProductionOverview = overview;
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(root);
        foreach (Object asset in assets)
            Object.DestroyImmediate(asset);
        assets.Clear();
    }

    [Test]
    public void IndexContainsEachNodeOncePerOutputAcrossRecipesAndRepeatedNodes()
    {
        ItemDefinition ore = Item(ItemType.IronOre);
        ItemDefinition slag = Item(ItemType.Slag);
        ProductionNode supplier = Node("Supplier", Vector3.zero,
            Recipe(new[] { Amount(ore, 2f), Amount(ore, 1f), Amount(slag, 1f) }),
            Recipe(new[] { Amount(ore, 4f) }));

        RegisterProducers(new[] { supplier, supplier, null });

        Assert.That(overview.ProducersByOutput[ItemType.IronOre], Is.EqualTo(new[] { supplier }));
        Assert.That(overview.ProducersByOutput[ItemType.Slag], Is.EqualTo(new[] { supplier }));
        Assert.That(overview.UnRegister(supplier), Is.True);
        Assert.That(overview.ProducersByOutput, Is.Empty);
    }

    [Test]
    public void StepChoosesClosestSupplierAndStoresTheSameEdgeInBothIndexes()
    {
        ItemDefinition ore = Item(ItemType.IronOre);
        Recipe recipe = Recipe(new[] { Amount(ore, 1f) });
        ProductionNode consumer = Node("Consumer", new Vector3(10f, 0f, 0f));
        ProductionNode far = Node("Far", new Vector3(10f, 0f, 9f), recipe);
        ProductionNode near = Node("Near", new Vector3(13f, 4f, 0f), recipe);
        RegisterProducers(new[] { far, near });

        List<ProductionGoal> next = controller.BackwardsProductionStepNaive(controller.Graph,
            new ProductionGoal(consumer, ItemType.IronOre, 20f, 10f));

        ProductionEdge edge = controller.Graph.IncomingEdges[consumer][0];
        Assert.That(edge.OutgoingNode, Is.SameAs(near));
        Assert.That(controller.Graph.OutgoingEdges[near][0], Is.SameAs(edge));
        Assert.That(edge.IncomingNode, Is.SameAs(consumer));
        Assert.That(edge.Item, Is.SameAs(ore));
        Assert.That(edge.Recipe, Is.SameAs(recipe));
        Assert.That(edge.SupplyQuantity, Is.EqualTo(20f));
        Assert.That(edge.TimeIntervalSeconds, Is.EqualTo(10f));
        Assert.That(edge.SupplyPerSecond, Is.EqualTo(2f));
        Assert.That(controller.ProductionEdgeMetric(edge), Is.EqualTo(5f));
        Assert.That(next, Is.Empty);
        Assert.That(near.Work, Is.Empty);
        Assert.That(near.StoredItems, Is.Empty);
    }

    [Test]
    public void StepsPropagateFractionalRatesOneLayerAtATimeAndKeepExactIngredients()
    {
        ItemDefinition ore = Item(ItemType.IronOre);
        ItemDefinition ingot = Item(ItemType.IronIngot);
        ItemDefinition plate = Item(ItemType.IronPlate);
        ProductionNode consumer = Node("Consumer", Vector3.zero);
        ProductionNode press = Node("Press", Vector3.right,
            Recipe(new[] { Amount(plate, 1f), Amount(plate, 1f) }, Amount(ingot, 1f), Amount(ingot, 2f)));
        ProductionNode smelter = Node("Smelter", Vector3.right * 2f,
            Recipe(new[] { Amount(ingot, 1f) }, Amount(ore, 2f)));
        ProductionNode mine = Node("Mine", Vector3.right * 3f, Recipe(new[] { Amount(ore, 3f) }));
        RegisterProducers(new[] { press, smelter, mine });

        List<ProductionGoal> next = controller.BackwardsProductionStepNaive(controller.Graph,
            new ProductionGoal(consumer, ItemType.IronPlate, 5f, 10f));

        Assert.That(controller.Graph.OutgoingEdges.Count, Is.EqualTo(1), "A call expands only one layer.");
        Assert.That(next.Count, Is.EqualTo(1));
        Assert.That(next[0].IncomingNode, Is.SameAs(press));
        Assert.That(next[0].RequiredItem, Is.SameAs(ingot));
        Assert.That(next[0].SupplyQuantity, Is.EqualTo(7.5f));
        Assert.That(next[0].TimeIntervalSeconds, Is.EqualTo(10f));

        next = controller.BackwardsProductionStepNaive(controller.Graph, next[0]);
        Assert.That(next[0].IncomingNode, Is.SameAs(smelter));
        Assert.That(next[0].RequiredItem, Is.SameAs(ore));
        Assert.That(next[0].SupplyQuantity, Is.EqualTo(15f));
        next = controller.BackwardsProductionStepNaive(controller.Graph, next[0]);
        Assert.That(next, Is.Empty);
        Assert.That(controller.Graph.OutgoingEdges.Count, Is.EqualTo(3));
    }

    [Test]
    public void ExactInputSkipsCloserSupplierOfDifferentDefinitionWithSameType()
    {
        ItemDefinition requiredOre = Item(ItemType.IronOre);
        ItemDefinition differentOre = Item(ItemType.IronOre);
        ProductionNode consumer = Node("Consumer", Vector3.zero);
        ProductionNode wrong = Node("Wrong definition", Vector3.right, Recipe(new[] { Amount(differentOre, 1f) }));
        ProductionNode correct = Node("Correct definition", Vector3.right * 10f, Recipe(new[] { Amount(requiredOre, 1f) }));
        RegisterProducers(new[] { wrong, correct });

        controller.BackwardsProductionStepNaive(controller.Graph, new ProductionGoal(consumer, requiredOre, 1f));

        Assert.That(controller.Graph.IncomingEdges[consumer][0].OutgoingNode, Is.SameAs(correct));
    }

    [Test]
    public void EqualMetricsKeepFirstSupplierAndFirstMatchingRecipe()
    {
        ItemDefinition ore = Item(ItemType.IronOre);
        Recipe firstRecipe = Recipe(new[] { Amount(ore, 1f) });
        ProductionNode consumer = Node("Consumer", Vector3.zero);
        ProductionNode first = Node("First", Vector3.right, firstRecipe, Recipe(new[] { Amount(ore, 2f) }));
        ProductionNode second = Node("Second", Vector3.left, firstRecipe);
        RegisterProducers(new[] { first, second });

        controller.BackwardsProductionStepNaive(controller.Graph, new ProductionGoal(consumer, ore, 1f));

        ProductionEdge edge = controller.Graph.IncomingEdges[consumer][0];
        Assert.That(edge.OutgoingNode, Is.SameAs(first));
        Assert.That(edge.Recipe, Is.SameAs(firstRecipe));
    }

    [Test]
    public void GraphRetainsMultipleFlowsAndSupportsProductionWithinTheSameNode()
    {
        ItemDefinition ore = Item(ItemType.IronOre);
        ProductionNode supplier = Node("Supplier", Vector3.zero, Recipe(new[] { Amount(ore, 1f) }));
        ProductionNode firstConsumer = Node("First consumer", Vector3.right);
        ProductionNode secondConsumer = Node("Second consumer", Vector3.left);
        RegisterProducers(new[] { supplier });

        foreach (ProductionNode consumer in new[] { firstConsumer, secondConsumer, supplier })
            controller.BackwardsProductionStepNaive(controller.Graph, new ProductionGoal(consumer, ore, 1f));
        controller.BackwardsProductionStepNaive(controller.Graph, new ProductionGoal(firstConsumer, ore, 2f));

        Assert.That(controller.Graph.OutgoingEdges[supplier].Count, Is.EqualTo(4));
        Assert.That(controller.Graph.IncomingEdges[firstConsumer].Count, Is.EqualTo(2));
        ProductionEdge internalEdge = controller.Graph.IncomingEdges[supplier][0];
        Assert.That(controller.Graph.OutgoingEdges[supplier], Does.Contain(internalEdge));
        controller.Graph.AddEdge(internalEdge);
        Assert.That(controller.Graph.OutgoingEdges[supplier].Count, Is.EqualTo(4));
        Assert.That(controller.Graph.IncomingEdges[supplier].Count, Is.EqualTo(1));
    }

    [Test]
    public void MissingSupplierAndInvalidRecipeInputsLeaveGraphUnchanged()
    {
        ItemDefinition ore = Item(ItemType.IronOre);
        ProductionNode consumer = Node("Consumer", Vector3.zero);
        ProductionGoal goal = new ProductionGoal(consumer, ore, 1f);

        Assert.Throws<InvalidOperationException>(() => controller.BackwardsProductionStepNaive(controller.Graph, goal));
        ProductionNode invalid = Node("Invalid", Vector3.right,
            Recipe(new[] { Amount(ore, 1f) }, Amount(ore, -1f)));
        RegisterProducers(new[] { invalid });
        Assert.Throws<InvalidOperationException>(() => controller.BackwardsProductionStepNaive(controller.Graph, goal));

        Assert.That(controller.Graph.OutgoingEdges, Is.Empty);
        Assert.That(controller.Graph.IncomingEdges, Is.Empty);
    }

    [Test]
    public void ExistingControllerGoalCreatesAnEdgeIntoOutputStorage()
    {
        ItemDefinition ore = Item(ItemType.IronOre);
        ProductionNode supplier = Node("Supplier", Vector3.right, Recipe(new[] { Amount(ore, 1f) }));
        controller.OutputStorage = Node("Output storage", Vector3.zero);
        RegisterProducers(new[] { supplier });
        controller.AddGoal(ItemType.IronOre, 20);

        List<ProductionGoal> next = controller.BackwardsProductionStepNaive(controller.Graph, controller.Goals[0], 10f);

        ProductionEdge edge = controller.Graph.IncomingEdges[controller.OutputStorage][0];
        Assert.That(edge.OutgoingNode, Is.SameAs(supplier));
        Assert.That(edge.IncomingNode, Is.SameAs(controller.OutputStorage));
        Assert.That(edge.SupplyPerSecond, Is.EqualTo(2f));
        Assert.That(next, Is.Empty);
    }

    [Test]
    public void SharedRecipeAssetsKeepTheirQuantitiesWhenNodesExecuteIndependently()
    {
        ItemDefinition ore = Item(ItemType.IronOre);
        ItemDefinition ingot = Item(ItemType.IronIngot);
        ItemAmount input = Amount(ore, 2f);
        ItemAmount output = Amount(ingot, 3f);
        Recipe recipe = Recipe(new[] { output }, input);
        StorageSize size = ScriptableObject.CreateInstance<StorageSize>();
        assets.Add(size);
        size.Capacities.Add(new AmountOf<ItemSuperType> { Item = ore.ItemSuperType, Amount = 20f });
        ProductionNode first = Node("First", Vector3.zero, recipe);
        ProductionNode second = Node("Second", Vector3.right, recipe);
        foreach (ProductionNode node in new[] { first, second })
        {
            node.StorageSize = size;
            node.StoredItems.Add(new AmountOf<ItemDefinition> { Item = ore, Amount = 4f });
            Assert.That(node.TryStartRecipe(recipe), Is.True);
            node.UpdateWork(0f);
        }
        Assert.That(first.TryStartRecipe(recipe), Is.True);
        first.UpdateWork(0f);

        Assert.That(StorageFunctions.GetStoredAmountByType(first.StoredItems, ItemType.IronOre), Is.Zero);
        Assert.That(StorageFunctions.GetStoredAmountByType(first.StoredItems, ItemType.IronIngot), Is.EqualTo(6f));
        Assert.That(StorageFunctions.GetStoredAmountByType(second.StoredItems, ItemType.IronOre), Is.EqualTo(2f));
        Assert.That(StorageFunctions.GetStoredAmountByType(second.StoredItems, ItemType.IronIngot), Is.EqualTo(3f));
        Assert.That(input.Amount, Is.EqualTo(2f));
        Assert.That(output.Amount, Is.EqualTo(3f));
    }

    [Test]
    public void GoalsRejectInvalidQuantitiesAndIntervals()
    {
        ProductionNode consumer = Node("Consumer", Vector3.zero);
        foreach (float invalid in new[] { 0f, -1f, float.NaN, float.PositiveInfinity })
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ProductionGoal(consumer, ItemType.IronOre, invalid));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ProductionGoal(consumer, ItemType.IronOre, 1f, invalid));
        }
    }

    private void RegisterProducers(IEnumerable<ProductionNode> nodes)
    {
        foreach (ProductionNode node in nodes)
            overview.Register(node);
    }

    private ProductionNode Node(string name, Vector3 position, params Recipe[] recipes)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(root.transform, false);
        obj.transform.position = position;
        ProductionNode node = obj.AddComponent<ProductionNode>();
        node.AvailableRecipes.AddRange(recipes);
        return node;
    }

    private ItemDefinition Item(ItemType type)
    {
        ItemDefinition item = ScriptableObject.CreateInstance<ItemDefinition>();
        item.ItemType = type;
        assets.Add(item);
        return item;
    }

    private Recipe Recipe(ItemAmount[] outputs, params ItemAmount[] inputs)
    {
        Recipe recipe = ScriptableObject.CreateInstance<Recipe>();
        recipe.Outputs.AddRange(outputs);
        recipe.Inputs.AddRange(inputs);
        assets.Add(recipe);
        return recipe;
    }

    private ItemAmount Amount(ItemDefinition item, float amount)
    {
        ItemAmount result = ScriptableObject.CreateInstance<ItemAmount>();
        assets.Add(result);
        var serialized = new SerializedObject(result);
        SerializedProperty value = serialized.FindProperty("itemAmount");
        value.FindPropertyRelative("Item").objectReferenceValue = item;
        value.FindPropertyRelative("Amount").floatValue = amount;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return result;
    }
}
