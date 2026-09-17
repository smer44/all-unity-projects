using System.Collections.Generic;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class ProducersByOutputPanelUI : MonoBehaviour
{
    [SerializeField] private ProductionGraphController productionGraphController;
    [SerializeField] private ProductionOverview productionOverview;
    [SerializeField] private TMP_FontAsset font;
    [SerializeField] private Vector2 panelSize = new(960f, 560f);
    [SerializeField, HideInInspector] private ProductionInfoTableUI table;

    private IReadOnlyDictionary<ItemType, List<ProductionNode>> producers;
    private readonly List<ItemType> itemTypes = new();
    private float nextRefreshTime;

    public void ShowController(ProductionGraphController controller)
    {
        productionGraphController = controller;
        productionOverview = null;
        producers = null;
        gameObject.SetActive(true);
        Refresh();
    }

    public void ShowProducers(IReadOnlyDictionary<ItemType, List<ProductionNode>> value)
    {
        productionGraphController = null;
        productionOverview = null;
        producers = value;
        gameObject.SetActive(true);
        Refresh();
    }

    public void ShowOverview(ProductionOverview overview)
    {
        productionGraphController = null;
        productionOverview = overview;
        producers = null;
        gameObject.SetActive(true);
        Refresh();
    }

    public void Hide() => gameObject.SetActive(false);

    private void OnEnable() => Refresh();

    private void Update()
    {
        if (Time.unscaledTime >= nextRefreshTime)
            Refresh();
    }

    public void Refresh()
    {
        if (table == null || !table.IsValid)
            table = ProductionInfoTableUI.Create((RectTransform)transform, panelSize, font, true);

        ProductionOverview overview = productionOverview != null ? productionOverview
            : productionGraphController != null ? productionGraphController.ProductionOverview : null;
        IReadOnlyDictionary<ItemType, List<ProductionNode>> source = overview != null
            ? overview.ProducersByOutput : producers;
        int producerColumns = CollectItemTypes(source);
        table.Begin($"Producers by output ({itemTypes.Count})", producerColumns + 1);
        table.AddCell("ItemType", true);
        for (int column = 0; column < producerColumns; column++)
            table.AddCell($"Producer {column + 1}", true);

        if (itemTypes.Count == 0)
        {
            table.AddCell("No producers available.");
            for (int column = 0; column < producerColumns; column++)
                table.AddCell(string.Empty);
        }

        foreach (ItemType itemType in itemTypes)
        {
            List<ProductionNode> nodes = source[itemType];
            table.AddCell(itemType.ToString());
            for (int column = 0; column < producerColumns; column++)
            {
                bool hasEntry = nodes != null && column < nodes.Count;
                table.AddCell(hasEntry ? nodes[column] != null ? nodes[column].name : "Missing node" : "-");
            }
        }
        table.End();
        nextRefreshTime = Time.unscaledTime + 0.25f;
    }

    private int CollectItemTypes(IReadOnlyDictionary<ItemType, List<ProductionNode>> source)
    {
        itemTypes.Clear();
        int columns = 1;
        if (source != null)
        {
            foreach (var pair in source)
            {
                itemTypes.Add(pair.Key);
                columns = Mathf.Max(columns, pair.Value?.Count ?? 0);
            }
        }
        itemTypes.Sort();
        return columns;
    }
}
