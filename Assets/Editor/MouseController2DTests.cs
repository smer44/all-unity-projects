using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

public class MouseController2DTests
{
    private GameObject root;
    private GameObject prefab;
    private MouseController2DForTests controller;
    private Mouse mouse;
    private Keyboard keyboard;
    private Mouse previousMouse;
    private Keyboard previousKeyboard;

    [SetUp]
    public void SetUp()
    {
        previousMouse = Mouse.current;
        previousKeyboard = Keyboard.current;
        mouse = InputSystem.AddDevice<Mouse>();
        keyboard = InputSystem.AddDevice<Keyboard>();

        root = new GameObject("Mouse state tests", typeof(RectTransform));
        Field2DPlaser field = root.AddComponent<Field2DPlaser>();
        SetReference(field, "parentRectTransform", root.transform);
        controller = root.AddComponent<MouseController2DForTests>();
        SetReference(controller, "fieldPlacer", field);
        controller.HitObject = root;
        typeof(MouseController2D).GetMethod("Awake",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(controller, null);
        prefab = new GameObject("Selected prefab", typeof(RectTransform));
    }

    [TearDown]
    public void TearDown()
    {
        // Remove popups first because their runtime cleanup uses deferred destruction.
        foreach (RessourceOnSpawnUI popup in root.GetComponentsInChildren<RessourceOnSpawnUI>(true))
            Object.DestroyImmediate(popup.gameObject);
        foreach (ProductionNodePanelUI popup in root.GetComponentsInChildren<ProductionNodePanelUI>(true))
            Object.DestroyImmediate(popup.gameObject);
        Object.DestroyImmediate(root);
        Object.DestroyImmediate(prefab);
        InputSystem.RemoveDevice(mouse);
        InputSystem.RemoveDevice(keyboard);
        previousMouse?.MakeCurrent();
        previousKeyboard?.MakeCurrent();
    }

    [Test]
    public void StartsInDefaultStateAndFieldClicksDoNotPlaceAnything()
    {
        Click();

        Assert.That(controller.CurrentState, Is.SameAs(controller.DefaultMouseState));
        Assert.That(root.transform.childCount, Is.Zero);
    }

    [Test]
    public void SelectionPlacesOneCopyPerClickAndCanSwitchPrefabs()
    {
        controller.SelectPrefabToPlace(prefab);
        Click();
        Click();

        Assert.That(root.transform.childCount, Is.EqualTo(2));
        Assert.That(root.transform.GetChild(0).name, Is.EqualTo("Selected prefab(Clone)"));
        Assert.That(controller.CurrentState, Is.SameAs(controller.PlacementSelectedMouseState));

        GameObject nextPrefab = root.transform.GetChild(0).gameObject;
        nextPrefab.name = "Next prefab";
        controller.SelectPrefabToPlace(nextPrefab);
        Click();

        Assert.That(root.transform.childCount, Is.EqualTo(3));
        Assert.That(root.transform.GetChild(2).name, Is.EqualTo("Next prefab(Clone)"));
    }

    [Test]
    public void EscapeCancelsBeforeASimultaneousClickAndAllowsANewSelection()
    {
        controller.SelectPrefabToPlace(prefab);
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape));
        InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Left));
        InputSystem.Update();
        controller.Tick();

        Assert.That(controller.CurrentState, Is.SameAs(controller.DefaultMouseState));
        Assert.That(controller.PlacementSelectedMouseState.SelectedPrefab, Is.Null);
        Assert.That(root.transform.childCount, Is.Zero);

        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        Click();
        Assert.That(root.transform.childCount, Is.Zero);

        controller.SelectPrefabToPlace(prefab);
        Click();
        Assert.That(root.transform.childCount, Is.EqualTo(1));
    }

    [TestCase(MouseButton.Right)]
    [TestCase(MouseButton.Middle)]
    public void OtherMouseButtonsDoNotPlaceTheSelectedPrefab(MouseButton button)
    {
        controller.SelectPrefabToPlace(prefab);
        Click(button);

        Assert.That(root.transform.childCount, Is.Zero);
        Assert.That(controller.CurrentState, Is.SameAs(controller.PlacementSelectedMouseState));
    }

    [Test]
    public void PlacementIgnoresToolbarButtonsAndPositionsOutsideTheField()
    {
        controller.SelectPrefabToPlace(prefab);
        controller.HitObject = prefab; // A toolbar or panel outside the field hierarchy.
        Click();
        Assert.That(root.transform.childCount, Is.Zero);

        GameObject button = new GameObject("Overlay button", typeof(RectTransform), typeof(Button));
        button.transform.SetParent(root.transform, false);
        controller.HitObject = button;
        Click();
        Assert.That(root.transform.childCount, Is.EqualTo(1));

        controller.HitObject = root;
        InputSystem.QueueStateEvent(mouse, new MouseState());
        InputSystem.Update();
        InputSystem.QueueStateEvent(mouse,
            new MouseState { position = new Vector2(10000f, 10000f) }.WithButton(MouseButton.Left));
        InputSystem.Update();
        controller.Tick();
        Assert.That(root.transform.childCount, Is.EqualTo(1));
    }

    [Test]
    public void DefaultStateStillOpensResourceInfo()
    {
        GameObject resource = new GameObject("Resource", typeof(RectTransform), typeof(RessourceOnField));
        resource.transform.SetParent(root.transform, false);
        controller.HitObject = resource;
        Click();

        Assert.That(controller.clickedObject, Is.SameAs(resource));
        Assert.That(resource.GetComponentInChildren<RessourceOnSpawnUI>(), Is.Not.Null);
        Assert.That(controller.CurrentState, Is.SameAs(controller.DefaultMouseState));
    }

    [Test]
    public void ProductionChildClickOpensPanelAndButtonClickKeepsItOpen()
    {
        GameObject producer = new GameObject("Workshop", typeof(RectTransform), typeof(ProductionNode));
        producer.transform.SetParent(root.transform, false);
        GameObject child = new GameObject("Workshop icon", typeof(RectTransform));
        child.transform.SetParent(producer.transform, false);
        Recipe recipe = ScriptableObject.CreateInstance<Recipe>();
        try
        {
            recipe.DisplayName = "Make plate";
            producer.GetComponent<ProductionNode>().AvailableRecipes.Add(recipe);
            controller.HitObject = child;
            Click();
            ProductionNodePanelUI panel = producer.GetComponentInChildren<ProductionNodePanelUI>();
            Assert.That(panel, Is.Not.Null);
            Assert.That(controller.clickedObject, Is.SameAs(producer));
            Assert.That(AbstractMouseController.HasOpenInfo, Is.True);

            Button button = panel.GetComponentInChildren<Button>();
            controller.HitObject = button.gameObject;
            Click();
            button.onClick.Invoke();
            Assert.That(panel.gameObject.activeInHierarchy, Is.True);
            Assert.That(controller.clickedObject, Is.SameAs(producer));
            Assert.That(producer.GetComponent<ProductionNode>().GetPendingRecipeCount(recipe), Is.EqualTo(1));
            controller.HitObject = child;
            Click();
            Assert.That(producer.GetComponentsInChildren<ProductionNodePanelUI>().Length, Is.EqualTo(1));

            controller.HitObject = root;
            Click();
            Assert.That(AbstractMouseController.HasOpenInfo, Is.False);
        }
        finally
        {
            Object.DestroyImmediate(recipe);
        }
    }

    [Test]
    public void SwitchingResourceAndProductionSelectionsKeepsOnlyOnePopup()
    {
        GameObject resource = new GameObject("Resource", typeof(RectTransform), typeof(RessourceOnField));
        resource.transform.SetParent(root.transform, false);
        GameObject producer = new GameObject("Workshop", typeof(RectTransform), typeof(ProductionNode));
        producer.transform.SetParent(root.transform, false);
        controller.HitObject = resource;
        Click();
        controller.HitObject = producer;
        Click();
        Assert.That(resource.GetComponentInChildren<RessourceOnSpawnUI>(), Is.Null);
        Assert.That(producer.GetComponentInChildren<ProductionNodePanelUI>(), Is.Not.Null);
        controller.HitObject = resource;
        Click();
        Assert.That(resource.GetComponentInChildren<RessourceOnSpawnUI>(), Is.Not.Null);
        Assert.That(producer.GetComponentInChildren<ProductionNodePanelUI>(), Is.Null);
    }

    [Test]
    public void PlacementIgnoresProductionPanelBackground()
    {
        ProductionNodePanelUI panel = ProductionNodePanelUI.Create(root.transform);
        controller.HitObject = panel.gameObject;
        Assert.That(controller.CanPlaceAtScreenPosition(Vector2.zero), Is.False);
        controller.SelectPrefabToPlace(prefab);
        Click();
        Assert.That(root.transform.childCount, Is.EqualTo(1));
    }

    [Test]
    public void DisablingControllerClearsPlacementSelection()
    {
        controller.SelectPrefabToPlace(prefab);
        controller.DisableForTests();

        Assert.That(controller.PlacementSelectedMouseState.SelectedPrefab, Is.Null);
        Click();
        Assert.That(root.transform.childCount, Is.Zero);
    }

    private void Click(MouseButton button = MouseButton.Left)
    {
        InputSystem.QueueStateEvent(mouse, new MouseState());
        InputSystem.Update();
        InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(button));
        InputSystem.Update();
        controller.Tick();
    }

    private static void SetReference(Object target, string name, Object value)
    {
        var serialized = new SerializedObject(target);
        serialized.FindProperty(name).objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}

public class MouseController2DForTests : MouseController2D
{
    public GameObject HitObject;

    // Calling Unity's Update message through SendMessage asserts in EditMode.
    public void Tick() => base.Update();
    public void DisableForTests() => base.OnDisable();

    protected override GameObject FindClickedObject(Vector2 screenPosition) => HitObject;
}
