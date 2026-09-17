using System.Collections.Generic;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public class ProductionNodePanelUITests
{
    private GameObject root;
    private ProductionNode node;
    private Recipe first;
    private Recipe second;
    private readonly List<Object> assets = new();

    [SetUp]
    public void SetUp()
    {
        root = new GameObject("Workshop", typeof(RectTransform), typeof(Canvas));
        node = root.AddComponent<ProductionNode>();
        node.TotalLabor = 1f;
        first = CreateRecipe("Iron plate");
        second = CreateRecipe("Wood plank");
        node.AvailableRecipes.AddRange(new[] { first, second });
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
    public void RequestsWaitForLaborAndDecreaseOnlyWhenCompleted()
    {
        var startedCounts = new List<int>();
        var completedCounts = new List<int>();
        node.OnProductionSignal += (owner, recipe, signal) =>
        {
            if (signal == ProductionSignal.Started)
                startedCounts.Add(owner.GetPendingRecipeCount(recipe));
            if (signal == ProductionSignal.Completed)
                completedCounts.Add(owner.GetPendingRecipeCount(recipe));
        };
        Assert.That(node.QueueRecipe(first), Is.True);
        Assert.That(node.QueueRecipe(first), Is.True);
        Assert.That(node.GetPendingRecipeCount(first), Is.EqualTo(2));
        Assert.That(node.Work.Count, Is.EqualTo(1));

        node.UpdateWork(1f);
        Assert.That(node.GetPendingRecipeCount(first), Is.EqualTo(2));
        node.UpdateWork(1f);
        Assert.That(node.GetPendingRecipeCount(first), Is.EqualTo(1));
        Assert.That(node.Work.Count, Is.EqualTo(1));
        Assert.That(node.Work[0].Timer, Is.EqualTo(first.Time));
        node.UpdateWork(2f);

        Assert.That(node.GetPendingRecipeCount(first), Is.Zero);
        Assert.That(node.Work, Is.Empty);
        Assert.That(node.FreeLabor, Is.EqualTo(1f));
        Assert.That(startedCounts, Is.EqualTo(new[] { 1, 1 }));
        Assert.That(completedCounts, Is.EqualTo(new[] { 1, 0 }));
    }

    [Test]
    public void RequestsWaitForInputsAndOutputCapacityWithoutBeingLost()
    {
        ItemDefinition ore = Asset<ItemDefinition>();
        ore.ItemType = ItemType.IronOre;
        ItemAmount input = Asset<ItemAmount>();
        var serializedInput = new SerializedObject(input);
        serializedInput.FindProperty("itemAmount.Item").objectReferenceValue = ore;
        serializedInput.FindProperty("itemAmount.Amount").floatValue = 2f;
        serializedInput.ApplyModifiedPropertiesWithoutUndo();
        first.Inputs.Add(input);
        Assert.That(node.QueueRecipe(first), Is.True);
        node.UpdateWork(3f);
        Assert.That(node.Work, Is.Empty);
        Assert.That(node.GetPendingRecipeCount(first), Is.EqualTo(1));

        node.StoredItems.Add(new AmountOf<ItemDefinition> { Item = ore, Amount = 2f });
        node.UpdateWork(0f);
        Assert.That(node.Work.Count, Is.EqualTo(1));
        Assert.That(node.StoredItems, Is.Empty);
        node.UpdateWork(first.Time);
        Assert.That(node.GetPendingRecipeCount(first), Is.Zero);

        second.Outputs.Add(input);
        node.QueueRecipe(second);
        Assert.That(node.Work, Is.Empty);
        Assert.That(node.GetPendingRecipeCount(second), Is.EqualTo(1));
        node.StorageSize = Asset<StorageSize>();
        node.UpdateWork(0f);
        Assert.That(node.Work, Is.Empty);
        node.StorageSize.Capacities.Add(new AmountOf<ItemSuperType> { Item = ore.ItemSuperType, Amount = 10f });
        node.UpdateWork(0f);
        node.UpdateWork(second.Time);
        Assert.That(node.GetPendingRecipeCount(second), Is.Zero);
        Assert.That(node.StoredItems[0].Amount, Is.EqualTo(2f));
        Assert.That(input.Amount, Is.EqualTo(2f), "Production must not consume the shared asset's amount.");
    }

    [Test]
    public void PausedRequestsStayWithTheirNodeAndRejectUnavailableRecipes()
    {
        GameObject otherObject = new GameObject("Other workshop");
        otherObject.transform.SetParent(root.transform);
        ProductionNode other = otherObject.AddComponent<ProductionNode>();
        other.AvailableRecipes.Add(first);
        other.TotalLabor = 1f;
        node.isProducing = false;
        node.QueueRecipe(first);
        node.QueueRecipe(second);
        TickNode();
        Assert.That(node.Work, Is.Empty);
        Assert.That(node.GetPendingRecipeCount(first), Is.EqualTo(1));
        Assert.That(other.GetPendingRecipeCount(first), Is.Zero);
        Assert.That(other.QueueRecipe(second), Is.False);
        Assert.That(other.QueueRecipe(null), Is.False);

        node.isProducing = true;
        TickNode();
        Assert.That(node.Work.Count, Is.EqualTo(1));
        node.UpdateWork(first.Time);
        Assert.That(node.GetPendingRecipeCount(first), Is.Zero);
        Assert.That(node.GetPendingRecipeCount(second), Is.EqualTo(1));
        Assert.That(node.Work[0].Recipe, Is.SameAs(second));
    }

    [Test]
    public void RecipeButtonsUpdateTheirOwnCountsAndRefreshReusesRows()
    {
        ProductionNodePanelUI panel = ProductionNodePanelUI.Create(root.transform);
        panel.ShowNode(node);
        Button[] buttons = panel.GetComponentsInChildren<Button>();
        Assert.That(buttons.Length, Is.EqualTo(2));
        Assert.That(panel.transform.GetChild(0).GetComponentInChildren<TMP_Text>().text, Is.EqualTo("Workshop"));
        Assert.That(panel.transform.GetChild(2).GetChild(0).GetComponent<TMP_Text>().text, Is.EqualTo("Iron plate"));
        TMP_Text firstCount = panel.transform.GetChild(2).GetChild(1).GetComponent<TMP_Text>();
        TMP_Text secondCount = panel.transform.GetChild(3).GetChild(1).GetComponent<TMP_Text>();

        buttons[0].onClick.Invoke();
        buttons[0].onClick.Invoke();
        buttons[1].onClick.Invoke();
        Assert.That(firstCount.text, Is.EqualTo("2"));
        Assert.That(secondCount.text, Is.EqualTo("1"));
        node.UpdateWork(first.Time);
        panel.Refresh();
        Assert.That(firstCount.text, Is.EqualTo("1"));
        Assert.That(secondCount.text, Is.EqualTo("1"));
        Assert.That(panel.GetComponentsInChildren<Button>(), Is.EqualTo(buttons));

        panel.Hide();
        node.UpdateWork(first.Time);
        panel.ShowNode(node);
        Assert.That(firstCount.text, Is.EqualTo("0"));
        Assert.That(secondCount.text, Is.EqualTo("1"));
    }

    [Test]
    public void RebindingTargetsTheNewNodeAndHandlesEmptyOrMissingRecipes()
    {
        ProductionNodePanelUI panel = ProductionNodePanelUI.Create(root.transform);
        panel.ShowNode(node);
        Button button = panel.GetComponentInChildren<Button>();
        GameObject otherObject = new GameObject("Other workshop");
        otherObject.transform.SetParent(root.transform);
        ProductionNode other = otherObject.AddComponent<ProductionNode>();
        other.AvailableRecipes.AddRange(node.AvailableRecipes);
        panel.ShowNode(other);
        button.onClick.Invoke();
        Assert.That(node.GetPendingRecipeCount(first), Is.Zero);
        Assert.That(other.GetPendingRecipeCount(first), Is.EqualTo(1));

        other.AvailableRecipes = new List<Recipe> { null, second, second };
        panel.Refresh();
        Assert.That(panel.GetComponentsInChildren<Button>().Length, Is.EqualTo(1));
        other.AvailableRecipes = null;
        panel.Refresh();
        Assert.That(panel.GetComponentsInChildren<Button>(), Is.Empty);
        Assert.That(panel.transform.GetChild(2).GetComponentInChildren<TMP_Text>().text, Is.EqualTo("No recipes available."));
        panel.ShowNode(null);
        Assert.That(panel.transform.GetChild(0).GetComponentInChildren<TMP_Text>().text, Is.EqualTo("Unknown production node"));
    }

    private Recipe CreateRecipe(string name)
    {
        Recipe recipe = Asset<Recipe>();
        recipe.DisplayName = name;
        recipe.Time = 2f;
        recipe.Labor = 1f;
        return recipe;
    }

    private void TickNode()
    {
        typeof(ProductionNode).GetMethod("FixedUpdate",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(node, null);
    }

    private T Asset<T>() where T : ScriptableObject
    {
        T asset = ScriptableObject.CreateInstance<T>();
        assets.Add(asset);
        return asset;
    }
}
