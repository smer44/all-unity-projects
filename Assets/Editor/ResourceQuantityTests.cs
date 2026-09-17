using System.Collections.Generic;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class ResourceQuantityTests
{
    private GameObject root;
    private Field2DPlaser field;
    private UnitOnField unit;
    private readonly List<Object> runtimeAssets = new();

    [SetUp]
    public void SetUp()
    {
        root = new GameObject("Resource quantity tests");
        field = root.AddComponent<Field2DPlaser>();
        unit = CreateObject("Worker").AddComponent<UnitOnField>();
        unit.SetField(field);
    }

    [TearDown]
    public void TearDown()
    {
        foreach (UnitOnField worker in root.GetComponentsInChildren<UnitOnField>())
            if (worker.AssignedWork != null)
                runtimeAssets.Add(worker.AssignedWork);
        Object.DestroyImmediate(root);
        foreach (Object asset in runtimeAssets)
            if (asset != null)
                Object.DestroyImmediate(asset);
        runtimeAssets.Clear();
    }

    [Test]
    public void WorkTransfersFractionalQuantityOnlyAfterCompletionAndOnlyOnce()
    {
        RessourceOnField resource = CreateResource(ItemType.WoodLog, 10f);
        UnitWork work = CreateWork(resource, 2.5f);

        Assert.That(work.Tick(field, unit, 0.5f), Is.False);
        work.Finish(field, unit);
        Assert.That(resource.ItemAmount.Amount, Is.EqualTo(10f));
        Assert.That(unit.CarriedItems, Is.Empty);

        Assert.That(work.Tick(field, unit, 0.5f), Is.True);
        work.Finish(field, unit);
        work.Finish(field, unit);
        Assert.That(resource.ItemAmount.Amount, Is.EqualTo(7.5f));
        Assert.That(unit.CarriedItems[ItemType.WoodLog].Amount, Is.EqualTo(2.5f));
    }

    [Test]
    public void RepeatedWorkStacksTheSameItemAndKeepsDifferentItemsSeparate()
    {
        RessourceOnField wood = CreateResource(ItemType.WoodLog, 10f);
        CompleteWork(CreateWork(wood, 2f), unit);
        CompleteWork(CreateWork(wood, 3f), unit);
        CompleteWork(CreateWork(CreateResource(ItemType.Crystal, 10f), 1.5f), unit);

        Assert.That(unit.CarriedItems.Count, Is.EqualTo(2));
        Assert.That(unit.CarriedItems[ItemType.WoodLog].Item, Is.EqualTo(ItemType.WoodLog));
        Assert.That(unit.CarriedItems[ItemType.WoodLog].Amount, Is.EqualTo(5f));
        Assert.That(unit.CarriedItems[ItemType.Crystal].Amount, Is.EqualTo(1.5f));
        Assert.That(wood.ItemAmount.Amount, Is.EqualTo(5f));
    }

    [Test]
    public void WorkersShareRemainingStockWithoutDuplicatingItems()
    {
        RessourceOnField resource = CreateResource(ItemType.Crystal, 3.5f);
        UnitOnField secondUnit = CreateObject("Second worker").AddComponent<UnitOnField>();
        UnitWork firstWork = CreateWork(resource, 2f);
        UnitWork secondWork = CreateWork(resource, 2f);
        CompleteWork(firstWork, unit);
        CompleteWork(secondWork, secondUnit);

        Assert.That(resource.ItemAmount.Amount, Is.Zero);
        Assert.That(resource.HasItems, Is.False);
        Assert.That(unit.CarriedItems[ItemType.Crystal].Amount, Is.EqualTo(2f));
        Assert.That(secondUnit.CarriedItems[ItemType.Crystal].Amount, Is.EqualTo(1.5f));
        Assert.That(unit.CollectFrom(resource, 2f), Is.Zero);
    }

    [Test]
    public void TravelAndCancellationDoNotAwardItems()
    {
        RessourceOnField resource = CreateResource(ItemType.WoodLog, 10f);
        resource.transform.localPosition = new Vector3(1000f, 0f, 0f);
        UnitWork work = CreateWork(resource, 2f);
        Assert.That(work.Tick(field, unit, 0.1f), Is.False);
        work.Finish(field, unit);
        work.Cancel(field, unit);
        Assert.That(unit.CarriedItems, Is.Empty);
        Assert.That(resource.ItemAmount.Amount, Is.EqualTo(10f));
    }

    [Test]
    public void DestroyedTargetDoesNotAwardItems()
    {
        RessourceOnField resource = CreateResource(ItemType.WoodLog, 10f);
        UnitWork work = CreateWork(resource, 2f);
        work.Tick(field, unit, 0.5f);
        Object.DestroyImmediate(resource.gameObject);
        Assert.That(work.Tick(field, unit, 1f), Is.True);
        work.Finish(field, unit);
        Assert.That(unit.CarriedItems, Is.Empty);
    }

    [Test]
    public void InvalidRequestsAndUnconfiguredResourcesDoNotChangeInventory()
    {
        RessourceOnField resource = CreateResource(ItemType.WoodLog, 10f);
        foreach (float quantity in new[] { 0f, -1f, float.NaN, float.PositiveInfinity })
            Assert.That(unit.CollectFrom(resource, quantity), Is.Zero);

        Assert.That(resource.ItemAmount.Amount, Is.EqualTo(10f));
        Assert.That(unit.CollectFrom(null, 1f), Is.Zero);
        Assert.That(unit.CollectFrom(CreateResource(ItemType.None, 10f), 1f), Is.Zero);
        Assert.That(unit.CarriedItems, Is.Empty);
    }

    [Test]
    public void LocationsCopyStartingStockWithoutChangingTheAssetOrEachOther()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ScriptsCityBuilder/ScriptsCity2DSCene/TreePrefab.prefab");
        ItemAmount initialItems = AssetDatabase.LoadAssetAtPath<ItemAmount>("Assets/ScriptsCity2DSCene/TreeItemAmount.asset");
        Assert.That(prefab, Is.Not.Null);
        Assert.That(initialItems, Is.Not.Null);
        RessourceOnField first = Object.Instantiate(prefab, root.transform).GetComponent<RessourceOnField>();
        RessourceOnField second = Object.Instantiate(prefab, root.transform).GetComponent<RessourceOnField>();

        Assert.That(first.ItemAmount.Item, Is.EqualTo(ItemType.WoodLog));
        Assert.That(first.ItemAmount.Amount, Is.EqualTo(100f));
        unit.CollectFrom(first, 2.5f);
        Assert.That(first.ItemAmount.Amount, Is.EqualTo(97.5f));
        Assert.That(second.ItemAmount.Amount, Is.EqualTo(100f));
        Assert.That(initialItems.Amount, Is.EqualTo(100f));
        Assert.That(first.ItemAmount, Is.Not.SameAs(second.ItemAmount));
        Assert.That(first.ItemAmount, Is.Not.SameAs(unit.CarriedItems[ItemType.WoodLog]));
    }

    [TestCase("TreeItemAmount", ItemType.WoodLog, 100f)]
    [TestCase("CrystalItemAmount", ItemType.Crystal, 100f)]
    [TestCase("ItemAmount 10 Wood", ItemType.WoodLog, 10f)]
    [TestCase("ItemAmount 10 IronOre", ItemType.IronOre, 10f)]
    [TestCase("Recipes/IronOre", ItemType.None, 0f)]
    public void AmountAssetsPreserveTheirItemsAndQuantities(string assetName, ItemType itemType, float amount)
    {
        ItemAmount asset = AssetDatabase.LoadAssetAtPath<ItemAmount>($"Assets/ScriptsCity2DSCene/{assetName}.asset");
        Assert.That(asset, Is.Not.Null);
        Assert.That(asset.Item == null ? ItemType.None : asset.Item.ItemType, Is.EqualTo(itemType));
        Assert.That(asset.Amount, Is.EqualTo(amount));
        AmountOf<ItemDefinition> first = asset.CreateRuntimeAmount();
        AmountOf<ItemDefinition> second = asset.CreateRuntimeAmount();
        Assert.That(first.Item, Is.SameAs(asset.Item));
        first.Amount = 123f;
        Assert.That(second.Amount, Is.EqualTo(amount));
        Assert.That(asset.Amount, Is.EqualTo(amount));
    }

    [Test]
    public void SavedRecipeReferencesTheRenamedAmountAsset()
    {
        Recipe recipe = AssetDatabase.LoadAssetAtPath<Recipe>("Assets/ScriptsCity2DSCene/Recipes/MineHandwork.asset");
        ItemAmount output = AssetDatabase.LoadAssetAtPath<ItemAmount>("Assets/ScriptsCity2DSCene/Recipes/IronOre.asset");
        Assert.That(recipe, Is.Not.Null);
        Assert.That(output, Is.Not.Null);
        Assert.That(recipe.Outputs, Is.EqualTo(new[] { output }));
    }

    [Test]
    public void ReturningHomeKeepsCollectedItems()
    {
        CompleteWork(CreateWork(CreateResource(ItemType.WoodLog, 10f), 2f), unit);
        UnitReturnHomeAction returnAction = UnitReturnHomeAction.Create();
        runtimeAssets.Add(returnAction);
        Assert.That(returnAction.Tick(field, unit, 1f), Is.True);
        returnAction.Finish(field, unit);
        Assert.That(unit.CarriedItems[ItemType.WoodLog].Amount, Is.EqualTo(2f));
    }

    [Test]
    public void ResourcePopupShowsRemainingQuantityAndDisablesDepletedJobs()
    {
        RessourceOnSpawnUI popup = RessourceOnSpawnUI.Create(root.transform);
        RessourceOnField resource = CreateResource(ItemType.Crystal, 2.5f);
        resource.PossibleWorks = new[] { CreateWork(resource, 1f) };
        resource.SetJobManagementSystem(root.AddComponent<JobManagementSystem>());
        typeof(RessourceOnSpawnUI).GetField("ressource", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(popup, resource);
        var refresh = typeof(RessourceOnSpawnUI).GetMethod("Refresh", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        var serializedPopup = new SerializedObject(popup);
        var quantityLabel = (TMP_Text)serializedPopup.FindProperty("quantityText").objectReferenceValue;
        var button = (Button)serializedPopup.FindProperty("addJobButton").objectReferenceValue;

        refresh.Invoke(popup, null);
        Assert.That(quantityLabel.text, Is.EqualTo($"Crystal: {2.5f:0.##}"));
        Assert.That(button.interactable, Is.True);
        unit.CollectFrom(resource, 10f);
        refresh.Invoke(popup, null);
        Assert.That(quantityLabel.text, Is.EqualTo("Crystal: 0"));
        Assert.That(button.interactable, Is.False);
    }

    [Test]
    public void ResourcePopupAssignsEveryClickToTheDisplayedResource()
    {
        UnitOnField secondUnit = CreateObject("Second worker").AddComponent<UnitOnField>();
        UnitOnField thirdUnit = CreateObject("Third worker").AddComponent<UnitOnField>();
        secondUnit.SetField(field);
        thirdUnit.SetField(field);
        thirdUnit.transform.localPosition = new Vector3(-100f, 0f, 0f);
        JobManagementSystem jobs = CreateJobSystem(unit, secondUnit, thirdUnit);
        RessourceOnField first = CreateResource(ItemType.WoodLog, 10f);
        RessourceOnField second = CreateResource(ItemType.WoodLog, 10f);
        second.transform.localPosition = new Vector3(100f, 0f, 0f);
        RessourceOnFieldData data = ScriptableObject.CreateInstance<RessourceOnFieldData>();
        runtimeAssets.Add(data);
        var serializedData = new SerializedObject(data);
        serializedData.FindProperty("maxPlaces").intValue = 2;
        serializedData.ApplyModifiedPropertiesWithoutUndo();
        first.Data = second.Data = data;
        UnitWork work = CreateWork(first, 1f);
        first.PossibleWorks = second.PossibleWorks = new[] { work };
        jobs.Register(field, first);
        jobs.Register(field, second);

        MouseController2D source = root.AddComponent<MouseController2D>();
        source.clickedObject = first.gameObject;
        RessourceOnSpawnUI popup = RessourceOnSpawnUI.Create(root.transform);
        popup.OnSpawn(source);
        Button button = popup.GetComponentInChildren<Button>();
        button.onClick.Invoke();
        Assert.That(unit.AssignedWork.RessourceToWorkOn, Is.SameAs(first));
        Assert.That(first.HasFreeSlots(), Is.True, "The first resource must still be a valid candidate.");

        source.clickedObject = second.gameObject;
        popup.OnSpawn(source);
        button.onClick.Invoke();
        Assert.That(secondUnit.AssignedWork.RessourceToWorkOn, Is.SameAs(second));
        Assert.That(first.OccupiedPlaces, Is.EqualTo(1));
        Assert.That(second.OccupiedPlaces, Is.EqualTo(1));

        button.onClick.Invoke();
        Assert.That(thirdUnit.AssignedWork.RessourceToWorkOn, Is.SameAs(second));
        Assert.That(first.OccupiedPlaces, Is.EqualTo(1));
        Assert.That(second.OccupiedPlaces, Is.EqualTo(2));
        Assert.That(button.interactable, Is.False);
    }

    [TestCase("full")]
    [TestCase("depleted")]
    [TestCase("unregistered")]
    [TestCase("missing")]
    public void TargetedJobDoesNotFallBackToAnotherResource(string unavailableReason)
    {
        JobManagementSystem jobs = CreateJobSystem(unit);
        RessourceOnField first = CreateResource(ItemType.WoodLog, 10f);
        RessourceOnField second = CreateResource(ItemType.WoodLog, 10f);
        UnitWork work = CreateWork(first, 1f);
        first.PossibleWorks = second.PossibleWorks = new[] { work };
        jobs.Register(field, first);
        jobs.Register(field, second);

        switch (unavailableReason)
        {
            case "full": second.TryOccupyPlace(); break;
            case "depleted": second.ItemAmount.Amount = 0f; break;
            case "unregistered": jobs.UnRegister(field, second); break;
        }

        Assert.That(jobs.AssignJobToResource(work.WorkName, unavailableReason == "missing" ? null : second), Is.False);
        Assert.That(unit.AssignedWork, Is.Null);
        Assert.That(first.OccupiedPlaces, Is.Zero);
        Assert.That(jobs.AssignJobNaive(work.WorkName), Is.True, "General job assignment should still select available resources.");
        Assert.That(unit.AssignedWork.RessourceToWorkOn, Is.SameAs(first));
    }

    private JobManagementSystem CreateJobSystem(params UnitOnField[] workers)
    {
        UnitController controller = root.AddComponent<UnitController>();
        UnitDwelling dwelling = root.AddComponent<UnitDwelling>();
        var serializedDwelling = new SerializedObject(dwelling);
        SerializedProperty units = serializedDwelling.FindProperty("units");
        units.arraySize = workers.Length;
        for (int i = 0; i < workers.Length; i++)
            units.GetArrayElementAtIndex(i).objectReferenceValue = workers[i];
        serializedDwelling.ApplyModifiedPropertiesWithoutUndo();
        controller.AddUnitsFromDwelling(dwelling);

        JobManagementSystem jobs = root.AddComponent<JobManagementSystem>();
        var serializedJobs = new SerializedObject(jobs);
        serializedJobs.FindProperty("fieldPlacer").objectReferenceValue = field;
        serializedJobs.FindProperty("unitController").objectReferenceValue = controller;
        serializedJobs.ApplyModifiedPropertiesWithoutUndo();
        return jobs;
    }

    private GameObject CreateObject(string name)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(root.transform, false);
        return obj;
    }

    private RessourceOnField CreateResource(ItemType item, float amount)
    {
        RessourceOnField resource = CreateObject("Resource").AddComponent<RessourceOnField>();
        resource.ItemAmount.Item = item;
        resource.ItemAmount.Amount = amount;
        return resource;
    }

    private UnitWork CreateWork(RessourceOnField resource, float quantity)
    {
        UnitWork template = ScriptableObject.CreateInstance<UnitWork>();
        template.name = "Gather";
        runtimeAssets.Add(template);
        var serializedWork = new SerializedObject(template);
        serializedWork.FindProperty("quantity").floatValue = quantity;
        serializedWork.FindProperty("workTime").floatValue = 1f;
        serializedWork.ApplyModifiedPropertiesWithoutUndo();
        UnitWork work = template.CreateAssignedWork(resource.gameObject);
        runtimeAssets.Add(work);
        work.Begin(field, unit);
        return work;
    }

    private void CompleteWork(UnitWork work, UnitOnField worker)
    {
        Assert.That(work.Tick(field, worker, 1f), Is.True);
        work.Finish(field, worker);
    }
}
