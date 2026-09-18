using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public class MemoryListUITests
{
    private GameObject root;
    private InventoryOfUnit owner;
    private QuestListUIController quests;
    private GridInventoryUIController inventory;
    private StoryMemory memory;
    private MemoryBehaviour previousMemory;
    private readonly List<Object> assets = new();

    private static readonly FieldInfo InstanceField = typeof(MemoryBehaviour).GetField(
        "<Instance>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic);

    [SetUp]
    public void SetUp()
    {
        previousMemory = MemoryBehaviour.Instance;
        root = new GameObject("Memory list UI tests", typeof(RectTransform), typeof(Canvas));
        owner = root.AddComponent<InventoryOfUnit>();
        owner.inventoryKey = "Player";
        memory = Asset<StoryMemory>();

        // Use isolated edit-mode memory without running the persistent singleton's Awake.
        var memoryObject = new GameObject("Test memory");
        memoryObject.SetActive(false);
        memoryObject.transform.SetParent(root.transform);
        var behaviour = memoryObject.AddComponent<MemoryBehaviour>();
        SetReference(behaviour, "storyMemory", memory);
        InstanceField.SetValue(null, behaviour);

        quests = Panel<QuestListUIController>("QuestListPanel");
        inventory = Panel<GridInventoryUIController>("InventoryPanel");
        // Ordinary MonoBehaviour lifecycle callbacks do not run in edit-mode tests.
        Lifecycle(quests, "OnEnable");
        Lifecycle(inventory, "OnEnable");
    }

    [TearDown]
    public void TearDown()
    {
        if (quests != null)
            Lifecycle(quests, "OnDisable");
        if (inventory != null)
            Lifecycle(inventory, "OnDisable");
        Object.DestroyImmediate(root);
        InstanceField.SetValue(null, previousMemory);
        foreach (Object asset in assets)
            Object.DestroyImmediate(asset);
        assets.Clear();
    }

    [Test]
    public void ControllersAcceptOnlyTheirOwnEntryTypes()
    {
        var quest = Quest("Find the guide", "Speak to the guide at the gate.");
        var item = Asset<InventoryEntry>();
        Assert.That(quests.AddInventoryEntry(item), Is.False);
        Assert.That(quests.AddInventoryEntry(null), Is.False);
        Assert.That(inventory.AddInventoryEntry(quest), Is.False);
        Assert.That(inventory.AddInventoryEntry(null), Is.False);
        Assert.That(quests.AddInventoryEntry(quest), Is.True);
        Assert.That(inventory.AddInventoryEntry(item), Is.True);
        Assert.That(Rows().Length, Is.EqualTo(1));
        Assert.That(inventory.GetComponent<GridUI>().GetInventoryEntry(0, 0), Is.SameAs(item));
    }

    [Test]
    public void RefreshUsesSeparatePrefixesAndSkipsNullOrWrongEntries()
    {
        var first = Quest("First", "First description");
        var second = Quest("Second", "Second description");
        var item = Asset<InventoryEntry>();
        List<MemoryForUnitOfQuests>(first, null, item, second);
        List<MemoryForUnitOfInventory>(first, item, null);

        quests.RefreshInventoryDisplay();
        inventory.RefreshInventoryDisplay();

        QuestEntryUI[] rows = Rows();
        Assert.That(rows.Length, Is.EqualTo(2));
        Assert.That(rows[0].Entry, Is.SameAs(first));
        Assert.That(rows[1].Entry, Is.SameAs(second));
        Assert.That(rows[0].GetComponentsInChildren<TMP_Text>()[0].text, Is.EqualTo(first.questName));
        Assert.That(rows[0].GetComponentsInChildren<TMP_Text>()[1].text, Is.EqualTo(first.questDescription));
        Assert.That(inventory.GetComponent<GridUI>().GetInventoryEntry(0, 0), Is.SameAs(item));
        Assert.That(inventory.GetComponent<GridUI>().GetInventoryEntry(1, 0), Is.Null);

        quests.RefreshInventoryDisplay();
        Assert.That(Rows().Length, Is.EqualTo(2), "Refreshing must replace the previous rows.");
    }

    [Test]
    public void MemoryChangesRefreshBothListsAndDisabledControllersUnsubscribe()
    {
        var first = Quest("First", "Before the change");
        var second = Quest("Second", "After the change");
        var questList = List<MemoryForUnitOfQuests>(first);
        var item = Asset<InventoryEntry>();
        var inventoryList = List<MemoryForUnitOfInventory>(item);
        quests.RefreshInventoryDisplay();
        inventory.RefreshInventoryDisplay();

        questList.entries = new AbstractMemoryEntry[] { second };
        inventoryList.entries = System.Array.Empty<AbstractMemoryEntry>();
        MemoryBehaviour.Set("Progress", 1);
        Assert.That(Rows().Length, Is.EqualTo(1));
        Assert.That(Rows()[0].Entry, Is.SameAs(second));
        Assert.That(inventory.GetComponent<GridUI>().GetInventoryEntry(0, 0), Is.Null);

        quests.enabled = false;
        Lifecycle(quests, "OnDisable");
        questList.entries = System.Array.Empty<AbstractMemoryEntry>();
        MemoryBehaviour.Set("Progress", 2);
        Assert.That(Rows().Length, Is.EqualTo(1));
        quests.enabled = true;
        Lifecycle(quests, "OnEnable");
        MemoryBehaviour.Set("Progress", 3);
        Assert.That(Rows(), Is.Empty);
    }

    [Test]
    public void MissingListsAndMemoryClearStaleRowsWithoutThrowing()
    {
        var quest = Quest("Quest", "Description");
        var list = List<MemoryForUnitOfQuests>(quest);
        quests.RefreshInventoryDisplay();
        list.entries = null;
        Assert.DoesNotThrow(quests.RefreshInventoryDisplay);
        Assert.That(Rows(), Is.Empty);

        quests.AddInventoryEntry(quest);
        memory.memoryForUnitDict.Clear();
        Assert.DoesNotThrow(quests.RefreshInventoryDisplay);
        Assert.That(Rows(), Is.Empty);

        quests.AddInventoryEntry(quest);
        InstanceField.SetValue(null, null);
        Assert.DoesNotThrow(quests.RefreshInventoryDisplay);
        Assert.That(Rows(), Is.Empty);
    }

    [Test]
    public void QuestPrefabUsesVBoxAndDisplaysNameAboveDescription()
    {
        quests.AddInventoryEntry(Quest("A quest", "A description"));
        quests.AddInventoryEntry(Quest("Another quest", "Another description"));
        var panel = (RectTransform)quests.transform;
        LayoutRebuilder.ForceRebuildLayoutImmediate(panel);
        Assert.That(quests.GetComponent<PackedBox>().PackingMode, Is.EqualTo(PackedBoxMode.VBox));
        QuestEntryUI[] rows = Rows();
        var first = (RectTransform)rows[0].transform;
        var second = (RectTransform)rows[1].transform;
        Assert.That(first.rect.height, Is.GreaterThan(0));
        Assert.That(second.anchoredPosition.y, Is.LessThan(first.anchoredPosition.y - first.rect.height));
        TMP_Text[] labels = rows[0].GetComponentsInChildren<TMP_Text>();
        Assert.That(labels.Length, Is.EqualTo(2));
        Assert.That(labels[0].rectTransform.rect.height, Is.GreaterThan(0));
        Assert.That(labels[1].rectTransform.anchoredPosition.y, Is.LessThan(labels[0].rectTransform.anchoredPosition.y));
        rows[0].SetQuestEntry(null);
        Assert.That(labels[0].text, Is.Empty);
        Assert.That(labels[1].text, Is.Empty);
    }

    private QuestEntryUI[] Rows() => quests.GetComponentsInChildren<QuestEntryUI>();

    private static void Lifecycle(AbstractInventoryUIController controller, string callback)
    {
        typeof(AbstractInventoryUIController).GetMethod(callback,
            BindingFlags.Instance | BindingFlags.NonPublic).Invoke(controller, null);
    }

    private T Panel<T>(string name) where T : AbstractInventoryUIController
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefabs/UI/{name}.prefab");
        Assert.That(prefab, Is.Not.Null);
        T controller = Object.Instantiate(prefab, root.transform).GetComponent<T>();
        SetReference(controller, "inventoryOfUnit", owner);
        return controller;
    }

    private T Asset<T>() where T : ScriptableObject
    {
        T asset = ScriptableObject.CreateInstance<T>();
        assets.Add(asset);
        return asset;
    }

    private QuestEntry Quest(string name, string description)
    {
        var entry = Asset<QuestEntry>();
        entry.questName = name;
        entry.questDescription = description;
        return entry;
    }

    private T List<T>(params AbstractMemoryEntry[] entries) where T : EntryListForUnit
    {
        T list = Asset<T>();
        list.memoryKey = owner.inventoryKey;
        list.entries = entries;
        memory.memoryForUnitDict.Add(list.EntryListKey(), list);
        return list;
    }

    private static void SetReference(Object target, string property, Object value)
    {
        var serialized = new SerializedObject(target);
        serialized.FindProperty(property).objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
