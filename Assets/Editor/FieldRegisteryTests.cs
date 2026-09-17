using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

public class FieldRegisteryTests
{
    private GameObject root;
    private Field2DPlaser field;
    private ProductionOverview overview;
    private JobManagementSystem jobs;
    private FieldRegisterListenerForTests listener;
    private readonly List<Object> assets = new();

    [SetUp]
    public void SetUp()
    {
        root = new GameObject("Field registry tests", typeof(RectTransform));
        field = root.AddComponent<Field2DPlaser>();
        overview = root.AddComponent<ProductionOverview>();
        jobs = root.AddComponent<JobManagementSystem>();
        listener = root.AddComponent<FieldRegisterListenerForTests>();
        SetReference(field, "parentRectTransform", root.transform);
        SetReference(field, "productionOverview", overview);
        SetReference(field, "jobManagementSystem", jobs);
        SetReference(jobs, "fieldPlacer", field);

        var serializedField = new SerializedObject(field);
        SerializedProperty listeners = serializedField.FindProperty("registerListeners");
        MonoBehaviour[] values = { null, overview, jobs, listener, listener, field };
        listeners.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
            listeners.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        serializedField.ApplyModifiedPropertiesWithoutUndo();
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
    public void StartingFieldRegistersInactiveNestedComponentsWithEveryMatchingListenerOnce()
    {
        GameObject parent = CreateObject("Starting building");
        GameObject child = CreateObject("Nested components");
        child.transform.SetParent(parent.transform, false);
        ProductionNode node = AddProducer(child);
        RessourceOnField resource = AddResource(child);
        parent.SetActive(false);

        InvokeField("InitializeExistingObjects");

        Assert.That(overview.ProducersByOutput[ItemType.IronOre], Is.EqualTo(new[] { node }));
        Assert.That(jobs.RessourcesByWorkName[resource.PossibleWorks[0].WorkName], Is.EqualTo(new[] { child }));
        Assert.That(node.Field, Is.SameAs(field));
        Assert.That(resource.Field, Is.SameAs(field));
        Assert.That(resource.JobManagementSystem, Is.SameAs(jobs));
        Assert.That(listener.ProductionRegistrations, Is.EqualTo(1));
        Assert.That(listener.ResourceRegistrations, Is.EqualTo(1));
        Assert.That(listener.LastField, Is.SameAs(field));
    }

    [Test]
    public void PlacementUpdatesAllRegistriesWithoutSpawnReceiverRegistration()
    {
        GameObject prefab = CreateObject("Building prefab");
        AddProducer(prefab);
        AddResource(prefab);
        field.TryPlaceAtScreenPosition(prefab, Vector2.zero);

        ProductionNode placedNode = overview.ProducersByOutput[ItemType.IronOre][0];
        Assert.That(placedNode.gameObject, Is.Not.SameAs(prefab));
        Assert.That(listener.ProductionRegistrations, Is.EqualTo(1));
        Assert.That(listener.ResourceRegistrations, Is.EqualTo(1));
        Assert.That(jobs.RessourcesByWorkName["Gather"], Is.EqualTo(new[] { placedNode.gameObject }));

    }

    [Test]
    public void InspectorArrayWorksWithoutTheDedicatedSystemReferences()
    {
        SetReference(field, "productionOverview", null);
        SetReference(field, "jobManagementSystem", null);
        ProductionNode node = AddProducer(CreateObject("Producer"));
        RessourceOnField resource = AddResource(CreateObject("Resource"));

        Assert.That(field.RegisterProductionNode(node), Is.True);
        Assert.That(field.RegisterRessource(resource), Is.True);
        Assert.That(resource.JobManagementSystem, Is.SameAs(jobs));
        Assert.That(field.UnRegisterProductionNode(node), Is.True);
        Assert.That(field.UnRegisterRessource(resource), Is.True);
        Assert.That(overview.ProducersByOutput, Is.Empty);
        Assert.That(jobs.RessourcesByWorkName, Is.Empty);
        Assert.That(listener.ProductionRemovals, Is.EqualTo(1));
        Assert.That(listener.ResourceRemovals, Is.EqualTo(1));
    }

    [Test]
    public void OverviewKeepsOtherProducersAndRemovesChangedRecipesFromEveryOutput()
    {
        ProductionNode first = AddProducer(CreateObject("First"));
        ProductionNode second = AddProducer(CreateObject("Second"));
        first.AvailableRecipes.Add(first.AvailableRecipes[0]);
        first.AvailableRecipes[0].Outputs.Add(Amount(ItemType.Slag, 1f));
        first.AvailableRecipes[0].Outputs.Add(Amount(ItemType.IronOre, 2f));

        Assert.That(overview.Register(first), Is.True);
        Assert.That(overview.Register(first), Is.False);
        Assert.That(overview.Register(second), Is.True);
        Assert.That(overview.ProducersByOutput[ItemType.IronOre], Is.EqualTo(new[] { first, second }));
        first.AvailableRecipes.Clear();

        Assert.That(overview.UnRegister(first), Is.True);
        Assert.That(overview.UnRegister(first), Is.False);
        Assert.That(overview.ProducersByOutput.ContainsKey(ItemType.Slag), Is.False);
        Assert.That(overview.ProducersByOutput[ItemType.IronOre], Is.EqualTo(new[] { second }));
    }

    [TestCase(true)]
    [TestCase(false)]
    public void FieldRegistersStorageAndOutputNodesUnderNone(bool nullRecipes)
    {
        ProductionNode storage = CreateObject("Storage").AddComponent<ProductionNode>();
        ProductionNode output = CreateObject("Output").AddComponent<ProductionNode>();
        ProductionNode producer = AddProducer(CreateObject("Producer"));
        if (nullRecipes)
            storage.AvailableRecipes = null;

        InvokeField("InitializeExistingObjects");

        Assert.That(overview.ProducersByOutput[ItemType.None], Is.EqualTo(new[] { storage, output }));
        Assert.That(overview.ProducersByOutput[ItemType.IronOre], Is.EqualTo(new[] { producer }));
        Assert.That(field.RegisterProductionNode(storage), Is.False);
        Assert.That(overview.ProducersByOutput[ItemType.None].Count, Is.EqualTo(2));
        Assert.That(field.UnRegisterProductionNode(storage), Is.True);
        Assert.That(overview.ProducersByOutput[ItemType.None], Is.EqualTo(new[] { output }));
        Assert.That(field.UnRegisterProductionNode(output), Is.True);
        Assert.That(overview.ProducersByOutput.ContainsKey(ItemType.None), Is.False);
        Assert.That(overview.ProducersByOutput[ItemType.IronOre], Is.EqualTo(new[] { producer }));
    }

    [Test]
    public void OverviewSkipsInvalidOutputsAndInitializesOnlyWhenMissingAtStart()
    {
        overview.ProducersByOutput = null;
        typeof(ProductionOverview).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(overview, null);
        Assert.That(overview.ProducersByOutput, Is.Not.Null);
        ProductionNode node = AddProducer(CreateObject("Producer"));
        Recipe recipe = node.AvailableRecipes[0];
        recipe.Outputs.Clear();
        recipe.Outputs.Add(null);
        ItemAmount missingItem = ScriptableObject.CreateInstance<ItemAmount>();
        assets.Add(missingItem);
        recipe.Outputs.Add(missingItem);
        recipe.Outputs.Add(Amount(ItemType.None, 1f));
        foreach (float amount in new[] { 0f, -1f, float.NaN, float.PositiveInfinity })
            recipe.Outputs.Add(Amount(ItemType.IronOre, amount));
        Assert.That(overview.Register(node), Is.False);
        Assert.That(overview.Register(null), Is.False);
        Assert.That(overview.UnRegister(null), Is.False);
        Assert.That(overview.ProducersByOutput, Is.Empty);

        recipe.Outputs.Add(Amount(ItemType.IronOre, 1f));
        Assert.That(overview.Register(node), Is.True);
        var index = overview.ProducersByOutput;
        typeof(ProductionOverview).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(overview, null);
        Assert.That(overview.ProducersByOutput, Is.SameAs(index));
        Assert.That(index[ItemType.IronOre], Is.EqualTo(new[] { node }));
    }

    [Test]
    public void ResourceRegistryRemovesAllOriginalWorksAndPreservesOtherResources()
    {
        RessourceOnField first = AddResource(CreateObject("First"));
        RessourceOnField second = AddResource(CreateObject("Second"));
        UnitWork extraWork = ScriptableObject.CreateInstance<UnitWork>();
        extraWork.name = "Mine";
        assets.Add(extraWork);
        first.PossibleWorks = new[] { first.PossibleWorks[0], extraWork, first.PossibleWorks[0], null };
        Registery<AbstractFieldPlacer, RessourceOnField> registery = jobs;

        Assert.That(registery.Register(field, first), Is.True);
        Assert.That(registery.Register(field, first), Is.False);
        Assert.That(registery.Register(field, second), Is.True);
        first.PossibleWorks = null;
        Assert.That(registery.UnRegister(field, first), Is.True);
        Assert.That(registery.UnRegister(field, first), Is.False);
        Assert.That(jobs.RessourcesByWorkName.ContainsKey("Mine"), Is.False);
        Assert.That(jobs.RessourcesByWorkName["Gather"], Is.EqualTo(new[] { second.gameObject }));
    }

    [Test]
    public void ResourceRegistryRejectsAnotherFieldWithoutChangingRegistration()
    {
        RessourceOnField resource = AddResource(CreateObject("Resource"));
        Field2DPlaser otherField = CreateObject("Other field").AddComponent<Field2DPlaser>();
        Assert.That(jobs.Register(field, resource), Is.True);

        LogAssert.Expect(LogType.Warning,
            "JobManagementSystem: ignored resource 'Resource' because it belongs to another field.");
        Assert.That(jobs.Register(otherField, resource), Is.False);
        Assert.That(jobs.UnRegister(otherField, resource), Is.False);
        Assert.That(resource.Field, Is.SameAs(field));
        Assert.That(jobs.RessourcesByWorkName["Gather"], Is.EqualTo(new[] { resource.gameObject }));
    }

    private GameObject CreateObject(string name)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(root.transform, false);
        return obj;
    }

    private ProductionNode AddProducer(GameObject obj)
    {
        ProductionNode node = obj.AddComponent<ProductionNode>();
        Recipe recipe = ScriptableObject.CreateInstance<Recipe>();
        assets.Add(recipe);
        recipe.Outputs.Add(Amount(ItemType.IronOre, 1f));
        node.AvailableRecipes.Add(recipe);
        return node;
    }

    private ItemAmount Amount(ItemType type, float amount)
    {
        ItemDefinition item = ScriptableObject.CreateInstance<ItemDefinition>();
        item.ItemType = type;
        assets.Add(item);
        ItemAmount result = ScriptableObject.CreateInstance<ItemAmount>();
        assets.Add(result);
        var serialized = new SerializedObject(result);
        SerializedProperty value = serialized.FindProperty("itemAmount");
        value.FindPropertyRelative("Item").objectReferenceValue = item;
        value.FindPropertyRelative("Amount").floatValue = amount;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return result;
    }

    private RessourceOnField AddResource(GameObject obj)
    {
        RessourceOnField resource = obj.AddComponent<RessourceOnField>();
        UnitWork work = ScriptableObject.CreateInstance<UnitWork>();
        work.name = "Gather";
        assets.Add(work);
        resource.PossibleWorks = new[] { work };
        return resource;
    }

    private static void SetReference(Object target, string propertyName, Object value)
    {
        var serialized = new SerializedObject(target);
        serialized.FindProperty(propertyName).objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private void InvokeField(string method, params object[] arguments)
    {
        typeof(AbstractFieldPlacer).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(field, arguments);
    }
}

public class FieldRegisteryLifecycleTests
{
    [UnityTest]
    public IEnumerator StartupPlacementAndDestructionKeepTheOverviewAndJobsCurrent()
    {
        yield return new EnterPlayMode();

        GameObject root = new GameObject("Runtime field", typeof(RectTransform));
        root.SetActive(false);
        ItemDefinition item = ScriptableObject.CreateInstance<ItemDefinition>();
        ItemAmount output = ScriptableObject.CreateInstance<ItemAmount>();
        Recipe recipe = ScriptableObject.CreateInstance<Recipe>();
        UnitWork work = ScriptableObject.CreateInstance<UnitWork>();
        try
        {
            Field2DPlaser field = root.AddComponent<Field2DPlaser>();
            ProductionOverview overview = root.AddComponent<ProductionOverview>();
            JobManagementSystem jobs = root.AddComponent<JobManagementSystem>();
            var serializedField = new SerializedObject(field);
            serializedField.FindProperty("parentRectTransform").objectReferenceValue = root.transform;
            serializedField.FindProperty("productionOverview").objectReferenceValue = overview;
            serializedField.FindProperty("jobManagementSystem").objectReferenceValue = jobs;
            serializedField.ApplyModifiedPropertiesWithoutUndo();

            GameObject building = new GameObject("Starting building");
            building.transform.SetParent(root.transform, false);
            ProductionNode node = building.AddComponent<ProductionNode>();
            RessourceOnField resource = building.AddComponent<RessourceOnField>();
            item.ItemType = ItemType.IronOre;
            var serializedOutput = new SerializedObject(output);
            SerializedProperty outputValue = serializedOutput.FindProperty("itemAmount");
            outputValue.FindPropertyRelative("Item").objectReferenceValue = item;
            outputValue.FindPropertyRelative("Amount").floatValue = 1f;
            serializedOutput.ApplyModifiedPropertiesWithoutUndo();
            recipe.Outputs.Add(output);
            node.AvailableRecipes.Add(recipe);
            work.name = "Gather";
            resource.PossibleWorks = new[] { work };
            overview.ProducersByOutput = null;

            // Startup must initialize the dictionary before the field registers existing objects.
            root.SetActive(true);
            yield return null;
            Assert.That(overview.ProducersByOutput[ItemType.IronOre], Is.EqualTo(new[] { node }));
            Assert.That(jobs.RessourcesByWorkName["Gather"], Is.EqualTo(new[] { building }));

            field.TryPlaceAtScreenPosition(building, Vector2.zero);
            ProductionNode placedNode = overview.ProducersByOutput[ItemType.IronOre][1];
            Assert.That(jobs.RessourcesByWorkName["Gather"].Count, Is.EqualTo(2));

            Object.Destroy(building);
            Object.Destroy(placedNode.gameObject);
            yield return null;

            Assert.That(overview.ProducersByOutput, Is.Empty);
            Assert.That(jobs.RessourcesByWorkName, Is.Empty);
        }
        finally
        {
            Object.Destroy(root);
            Object.Destroy(item);
            Object.Destroy(output);
            Object.Destroy(recipe);
            Object.Destroy(work);
        }
    }

    [UnityTearDown]
    public IEnumerator LeavePlayMode()
    {
        if (Application.isPlaying)
            yield return new ExitPlayMode();
    }
}

public class FieldRegisterListenerForTests : MonoBehaviour,
    Registery<AbstractFieldPlacer, ProductionNode>, Registery<AbstractFieldPlacer, RessourceOnField>
{
    public int ProductionRegistrations;
    public int ResourceRegistrations;
    public int ProductionRemovals;
    public int ResourceRemovals;
    public AbstractFieldPlacer LastField;

    public bool Register(AbstractFieldPlacer field, ProductionNode node)
    {
        ProductionRegistrations++;
        LastField = field;
        return false;
    }

    public bool Register(AbstractFieldPlacer field, RessourceOnField resource)
    {
        ResourceRegistrations++;
        LastField = field;
        return false;
    }

    public bool UnRegister(AbstractFieldPlacer field, ProductionNode node)
    {
        ProductionRemovals++;
        return false;
    }

    public bool UnRegister(AbstractFieldPlacer field, RessourceOnField resource)
    {
        ResourceRemovals++;
        return false;
    }
}
