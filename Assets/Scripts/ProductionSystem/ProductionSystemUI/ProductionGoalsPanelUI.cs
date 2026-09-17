using System.Collections.Generic;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class ProductionGoalsPanelUI : MonoBehaviour
{
    [SerializeField] private ProductionGraphController productionGraphController;
    [SerializeField] private TMP_FontAsset font;
    [SerializeField] private Vector2 panelSize = new(420f, 480f);
    [SerializeField, HideInInspector] private ProductionInfoTableUI table;

    private IReadOnlyList<AmountOf<ItemType>> goals;
    private float nextRefreshTime;

    public void ShowController(ProductionGraphController controller)
    {
        productionGraphController = controller;
        goals = null;
        gameObject.SetActive(true);
        Refresh();
    }

    public void ShowGoals(IReadOnlyList<AmountOf<ItemType>> value)
    {
        productionGraphController = null;
        goals = value;
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
            table = ProductionInfoTableUI.Create((RectTransform)transform, panelSize, font, false);

        IReadOnlyList<AmountOf<ItemType>> source = productionGraphController != null
            ? productionGraphController.Goals : goals;
        int count = source?.Count ?? 0;
        table.Begin($"Goals ({count})", 1);
        if (count == 0)
            table.AddCell("No production goals.");
        else
        {
            for (int i = 0; i < count; i++)
            {
                AmountOf<ItemType> goal = source[i];
                table.AddCell(goal == null ? "Missing goal" : $"{goal.Item}: {goal.Amount:0.##}");
            }
        }
        table.End();
        nextRefreshTime = Time.unscaledTime + 0.25f;
    }
}
