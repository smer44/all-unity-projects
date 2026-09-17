using System;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public class UIGameObjectFactoryTests
{
    private GameObject root;

    [SetUp]
    public void SetUp()
    {
        root = new GameObject("Panel factory tests", typeof(RectTransform), typeof(Canvas));
        root.layer = LayerMask.NameToLayer("UI");
    }

    [TearDown]
    public void TearDown() => Object.DestroyImmediate(root);

    [Test]
    public void WeightsAndPercentagesKeepTheirProportionsAfterResizing()
    {
        var panel = UIGameObjectFactory.CreatePanel(root.transform, "Weighted panel",
            new UIGameObjectFactory.PanelFormatting
            {
                Width = 400f, Height = 200f, RowHeightPercentages = new[] { 25f, 75f }
            },
            new UIGameObjectFactory.RowFormatting { ItemWidthWeights = new[] { 2f, 1f, 1f } },
            new[] { "Name", "Amount", "Action" }, new[] { "Wood", "10", "Gather" });

        Assert.That(panel.Root.GetComponent<Image>(), Is.Not.Null);
        Assert.That(panel.Rows[0].Root.rect.height, Is.EqualTo(50f).Within(0.001f));
        Assert.That(panel.Rows[1].Root.rect.height, Is.EqualTo(150f).Within(0.001f));
        Assert.That(panel.Rows[1].Labels[0].rectTransform.rect.width, Is.EqualTo(200f).Within(0.001f));
        Assert.That(panel.Rows[1].Labels[1].rectTransform.rect.width, Is.EqualTo(100f).Within(0.001f));
        Assert.That(panel.Rows[0].Root.anchorMin.y, Is.EqualTo(panel.Rows[1].Root.anchorMax.y));

        panel.Root.sizeDelta = new Vector2(800f, 400f);
        Assert.That(panel.Rows[0].Root.rect.height, Is.EqualTo(100f).Within(0.001f));
        Assert.That(panel.Rows[1].Labels[0].rectTransform.rect.width, Is.EqualTo(400f).Within(0.001f));
        Assert.That(panel.Rows[1].Labels[2].rectTransform.rect.width, Is.EqualTo(200f).Within(0.001f));
    }

    [Test]
    public void IndividualRowsCanUseDifferentFontsSizesAndWeights()
    {
        var formatting = new UIGameObjectFactory.PanelFormatting
        {
            Width = 300f, Height = 100f, RowHeightPercentages = new[] { 40f, 60f }
        };
        var panel = UIGameObjectFactory.CreatePanel(root.transform, "Individual rows", formatting,
            new UIGameObjectFactory.RowFormatting());
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        Assert.That(font, Is.Not.Null);
        var header = UIGameObjectFactory.CreateRow(panel.Root, 0, formatting,
            new UIGameObjectFactory.RowFormatting { Font = font, FontSize = 18f, FontStyle = FontStyles.Bold }, "Title");
        var row = UIGameObjectFactory.CreateRow(panel.Root, 1, formatting,
            new UIGameObjectFactory.RowFormatting { ItemWidthWeights = new[] { 1f, 2f } }, "Wood", "12");

        Assert.That(header.Labels[0].font, Is.SameAs(font));
        Assert.That(header.Labels[0].fontSize, Is.EqualTo(18f));
        Assert.That(header.Labels[0].fontStyle, Is.EqualTo(FontStyles.Bold));
        Assert.That(row.Labels[0].fontSize, Is.EqualTo(14f));
        Assert.That(row.Labels[0].rectTransform.rect.width, Is.EqualTo(100f).Within(0.001f));
        Assert.That(row.Labels[1].rectTransform.rect.width, Is.EqualTo(200f).Within(0.001f));
    }

    [Test]
    public void ReplacingMiddleLabelPreservesItsSlotAndReferenceAndInvokesCallback()
    {
        var panel = UIGameObjectFactory.CreatePanel(root.transform, "Button panel",
            new UIGameObjectFactory.PanelFormatting { Width = 400f },
            new UIGameObjectFactory.RowFormatting { ItemWidthWeights = new[] { 1f, 2f, 1f } },
            new[] { "Before", "Gather", "After" });
        TextMeshProUGUI label = panel.Rows[0].Labels[1];
        TMP_FontAsset font = label.font;
        int clicks = 0;
        Button button = UIGameObjectFactory.ReplaceLabelWithButton(label, () => clicks++);
        RectTransform rect = (RectTransform)button.transform;

        Assert.That(button.transform.GetSiblingIndex(), Is.EqualTo(1));
        Assert.That(panel.Rows[0].Root.childCount, Is.EqualTo(3));
        Assert.That(rect.rect.width, Is.EqualTo(200f).Within(0.001f));
        Assert.That(button.GetComponentInChildren<TextMeshProUGUI>(), Is.SameAs(label));
        Assert.That(label.text, Is.EqualTo("Gather"));
        Assert.That(label.font, Is.SameAs(font));
        Assert.That(button.targetGraphic.raycastTarget, Is.True);
        Assert.That(label.raycastTarget, Is.False);
        button.onClick.Invoke();
        Assert.That(clicks, Is.EqualTo(1));

        panel.Root.sizeDelta = new Vector2(800f, 200f);
        Assert.That(rect.rect.width, Is.EqualTo(400f).Within(0.001f));
        Assert.That(label.rectTransform.rect.width, Is.EqualTo(rect.rect.width));
    }

    [Test]
    public void InvalidFormattingDoesNotLeavePartiallyGeneratedPanels()
    {
        var panelFormatting = new UIGameObjectFactory.PanelFormatting();
        var rowFormatting = new UIGameObjectFactory.RowFormatting { ItemWidthWeights = new[] { 1f, 2f } };
        Assert.Throws<ArgumentException>(() => UIGameObjectFactory.CreatePanel(root.transform, "Invalid",
            panelFormatting, rowFormatting, new[] { "Only one cell" }));
        rowFormatting.ItemWidthWeights = new[] { float.NaN };
        Assert.Throws<ArgumentOutOfRangeException>(() => UIGameObjectFactory.CreatePanel(root.transform, "Invalid",
            panelFormatting, rowFormatting, new[] { "Cell" }));
        rowFormatting.ItemWidthWeights = Array.Empty<float>();
        panelFormatting.RowHeightPercentages = new[] { 60f, 50f };
        Assert.Throws<ArgumentException>(() => UIGameObjectFactory.CreatePanel(root.transform, "Invalid",
            panelFormatting, rowFormatting, new[] { "First" }, new[] { "Second" }));
        Assert.That(root.transform.childCount, Is.Zero);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ResourcePopupGeneratesOnceWithNoSourceAndSupportsLegacyPrefab(bool usePrefab)
    {
        RessourceOnSpawnUI popup = usePrefab
            ? Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/ScriptsCity2DSCene/RessourceOnSpawnUIPrefab.prefab"), root.transform).GetComponent<RessourceOnSpawnUI>()
            : RessourceOnSpawnUI.Create(root.transform);
        popup.GenerateUI();
        TextMeshProUGUI firstLabel = popup.GetComponentInChildren<TextMeshProUGUI>();
        popup.OnSpawn((AbstractMouseController)null);
        popup.OnSpawn((SpawnInfoOnClick2D)null);
        popup.GenerateUI();

        Assert.That(popup.GetComponentsInChildren<TextMeshProUGUI>().Length, Is.EqualTo(5));
        Assert.That(popup.GetComponentInChildren<TextMeshProUGUI>(), Is.SameAs(firstLabel));
        Assert.That(firstLabel.text, Is.EqualTo("Unknown resource"));
        Assert.That(popup.GetComponentsInChildren<Button>().Length, Is.EqualTo(1));
        Assert.That(popup.GetComponentInChildren<Button>().interactable, Is.False);
        Assert.That(((RectTransform)popup.transform).rect.size, Is.EqualTo(new Vector2(220f, 156f)));
    }
}
