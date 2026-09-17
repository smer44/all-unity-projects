using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public class InventoryUITests
{
    private GameObject root;
    private GridUI grid;

    [SetUp]
    public void SetUp()
    {
        root = new GameObject("Inventory UI tests", typeof(RectTransform), typeof(Canvas));
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/InventoryPanel.prefab");
        Assert.That(prefab, Is.Not.Null);
        grid = Object.Instantiate(prefab, root.transform).GetComponent<GridUI>();
        Assert.That(grid, Is.Not.Null);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(root);
    }

    [Test]
    public void GridInitializesBeforeStartOnlyOnceAndLaysOutRows()
    {
        var entry = new InventoryEntry { key = "First" };
        Assert.That(grid.AddInventoryEntry(entry), Is.True);
        InventoryEntryUI firstCell = grid.GetComponentInChildren<InventoryEntryUI>();
        typeof(GridUI).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(grid, null);
        Assert.That(grid.Initialize(), Is.True);
        Assert.That(grid.GetInventoryEntry(0, 0), Is.SameAs(entry));
        Assert.That(grid.GetComponentInChildren<InventoryEntryUI>(), Is.SameAs(firstCell));
        Assert.That(grid.transform.childCount, Is.EqualTo(grid.XCells * grid.YCells));

        var rect = (RectTransform)grid.transform;
        LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
        var first = (RectTransform)grid.transform.GetChild(0);
        var second = (RectTransform)grid.transform.GetChild(1);
        var nextRow = (RectTransform)grid.transform.GetChild(grid.XCells);
        Assert.That(second.anchoredPosition.x, Is.GreaterThan(first.anchoredPosition.x));
        Assert.That(second.anchoredPosition.y, Is.EqualTo(first.anchoredPosition.y));
        Assert.That(nextRow.anchoredPosition.x, Is.EqualTo(first.anchoredPosition.x));
        Assert.That(nextRow.anchoredPosition.y, Is.LessThan(first.anchoredPosition.y));
        Assert.That(rect.rect.width, Is.EqualTo(492f).Within(0.01f));
        Assert.That(rect.rect.height, Is.EqualTo(300f).Within(0.01f));
    }

    [Test]
    public void AddingFillsRowsRejectsOverflowAndReusesFirstEmptyCell()
    {
        for (int j = 0; j < grid.YCells; j++)
        {
            for (int i = 0; i < grid.XCells; i++)
            {
                var entry = new InventoryEntry { key = $"{i},{j}" };
                Assert.That(grid.AddInventoryEntry(entry), Is.True);
                Assert.That(grid.GetInventoryEntry(i, j), Is.SameAs(entry));
            }
        }

        Assert.That(grid.AddInventoryEntry(new InventoryEntry()), Is.False);
        Assert.That(grid.AddInventoryEntry(null), Is.False);
        Assert.That(grid.SetInventoryEntry(1, 0, null), Is.True);
        var replacement = new InventoryEntry { key = "Replacement" };
        Assert.That(grid.AddInventoryEntry(replacement), Is.True);
        Assert.That(grid.GetInventoryEntry(1, 0), Is.SameAs(replacement));
        Assert.That(grid.GetInventoryEntry(0, 1).key, Is.EqualTo("0,1"));
    }

    [Test]
    public void CoordinatesRejectOutOfBoundsWithoutChangingEntries()
    {
        var entry = new InventoryEntry { key = "Last" };
        Assert.That(grid.SetInventoryEntry(grid.XCells - 1, grid.YCells - 1, entry), Is.True);
        foreach (Vector2Int coordinate in new[]
        {
            new Vector2Int(-1, 0), new Vector2Int(0, -1),
            new Vector2Int(grid.XCells, 0), new Vector2Int(0, grid.YCells)
        })
        {
            Assert.That(grid.SetInventoryEntry(coordinate.x, coordinate.y, entry), Is.False);
            Assert.That(grid.GetInventoryEntry(coordinate.x, coordinate.y), Is.Null);
        }
        Assert.That(grid.GetInventoryEntry(grid.XCells - 1, grid.YCells - 1), Is.SameAs(entry));
        Assert.That(grid.GetInventoryEntry(0, 0), Is.Null);
    }

    [Test]
    public void ClearingSpritesPreservesCellsAndTheirConfiguredColor()
    {
        grid.Initialize();
        var cell = grid.transform.GetChild(0).GetComponent<InventoryEntryUI>();
        Image image = cell.GetComponent<Image>();
        var defaultColor = new Color(0.4f, 0.5f, 0.6f, 0.7f);
        image.color = defaultColor;
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/tablet.png");
        Assert.That(sprite, Is.Not.Null);

        grid.SetInventoryEntry(0, 0, new InventoryEntry { key = "Tablet", sprite = sprite });
        Assert.That(image.sprite, Is.SameAs(sprite));
        grid.SetInventoryEntry(0, 0, new InventoryEntry { key = "No icon" });
        Assert.That(image.sprite, Is.Null);
        Assert.That(image.color, Is.EqualTo(defaultColor));
        Assert.That(grid.GetInventoryEntry(0, 0), Is.Not.Null);

        grid.SetInventoryEntry(0, 0, new InventoryEntry { sprite = sprite });
        grid.ClearInventoryEntries();
        Assert.That(grid.transform.GetChild(0).GetComponent<InventoryEntryUI>(), Is.SameAs(cell));
        Assert.That(grid.GetInventoryEntry(0, 0), Is.Null);
        Assert.That(image.sprite, Is.Null);
        Assert.That(image.color, Is.EqualTo(defaultColor));
        Assert.That(image.enabled, Is.True);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void MissingOrDisabledLayoutWarnsWithoutGeneratingCells(bool disabled)
    {
        GridLayoutGroup layout = grid.GetComponent<GridLayoutGroup>();
        if (disabled)
            layout.enabled = false;
        else
            Object.DestroyImmediate(layout);

        LogAssert.Expect(LogType.Warning, "GridUI requires an enabled GridLayoutGroup on its panel.");
        Assert.That(grid.Initialize(), Is.False);
        Assert.That(grid.transform.childCount, Is.Zero);
    }

    [Test]
    public void MemoryChangesRefreshThroughBaseControllerAndReuseCells()
    {
        var instanceProperty = typeof(MemoryBehaviour).GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
        MemoryBehaviour previousMemory = MemoryBehaviour.Instance;
        var memoryObject = new GameObject("Inventory test memory");
        memoryObject.SetActive(false);
        MemoryBehaviour memory = memoryObject.AddComponent<MemoryBehaviour>();
        StoryMemory story = ScriptableObject.CreateInstance<StoryMemory>();
        var serializedMemory = new SerializedObject(memory);
        serializedMemory.FindProperty("storyMemory").objectReferenceValue = story;
        serializedMemory.ApplyModifiedPropertiesWithoutUndo();
        instanceProperty.SetValue(null, memory);
        AbstractInventoryUIController controller = grid.GetComponent<GridInventoryUIController>();

        try
        {
            InvokeControllerLifecycle(controller, "OnEnable");
            MemoryBehaviour.Set("Tablet", 2);
            Assert.That(grid.GetInventoryEntry(0, 0).key, Is.EqualTo("Tablet"));
            Assert.That(grid.GetInventoryEntry(1, 0), Is.Null);
            Transform firstCell = grid.transform.GetChild(0);
            MemoryBehaviour.Set("FoodRatio", 1);
            Assert.That(grid.GetInventoryEntry(1, 0).key, Is.EqualTo("FoodRatio"));
            MemoryBehaviour.Set("Tablet", 0);
            Assert.That(grid.GetInventoryEntry(0, 0).key, Is.EqualTo("FoodRatio"));
            Assert.That(grid.GetInventoryEntry(1, 0), Is.Null);
            Assert.That(grid.transform.GetChild(0), Is.SameAs(firstCell));

            InvokeControllerLifecycle(controller, "OnDisable");
            MemoryBehaviour.Set("FoodRatio", 0);
            Assert.That(grid.GetInventoryEntry(0, 0).key, Is.EqualTo("FoodRatio"));
            InvokeControllerLifecycle(controller, "OnEnable");
            controller.RefreshInventoryDisplay();
            Assert.That(grid.GetInventoryEntry(0, 0), Is.Null);
            MemoryBehaviour.Set("Tablet", -1);
            Assert.That(grid.GetInventoryEntry(0, 0).key, Is.EqualTo("Tablet"));
        }
        finally
        {
            InvokeControllerLifecycle(controller, "OnDisable");
            Object.DestroyImmediate(memoryObject);
            Object.DestroyImmediate(story);
            instanceProperty.SetValue(null, previousMemory);
        }
    }

    private static void InvokeControllerLifecycle(AbstractInventoryUIController controller, string method)
    {
        // Runtime MonoBehaviour callbacks do not run automatically in Edit Mode.
        typeof(AbstractInventoryUIController).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(controller, null);
    }

    [TestCase("Assets/Prefabs/UI/Canvas.prefab")]
    [TestCase("Assets/Prefabs/UI/TurnBasedUI.prefab")]
    public void ExistingNestedPrefabsRetainGridControllerAndItemConfiguration(string path)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        GridInventoryUIController controller = prefab.GetComponentInChildren<GridInventoryUIController>(true);
        Assert.That(controller, Is.Not.Null);
        var serializedController = new SerializedObject(controller);
        Assert.That(serializedController.FindProperty("gridInventoryPanel").objectReferenceValue,
            Is.SameAs(controller.GetComponent<GridUI>()));
        SerializedProperty entries = serializedController.FindProperty("inventoryEntries");
        Assert.That(entries.arraySize, Is.EqualTo(2));
        Assert.That(entries.GetArrayElementAtIndex(0).FindPropertyRelative("key").stringValue, Is.EqualTo("Tablet"));
        Assert.That(entries.GetArrayElementAtIndex(1).FindPropertyRelative("key").stringValue, Is.EqualTo("FoodRatio"));
        Assert.That(entries.GetArrayElementAtIndex(0).FindPropertyRelative("sprite").objectReferenceValue, Is.Not.Null);
        Assert.That(entries.GetArrayElementAtIndex(1).FindPropertyRelative("sprite").objectReferenceValue, Is.Not.Null);
    }
}
