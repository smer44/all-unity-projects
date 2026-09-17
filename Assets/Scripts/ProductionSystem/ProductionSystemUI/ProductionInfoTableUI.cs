using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shared resource-panel styling, scrolling and reusable text cells.
/// Serialized references also allow the generated layout to be saved in a prefab.
/// </summary>
[Serializable]
public sealed class ProductionInfoTableUI
{
    private const float RowHeight = 30f;
    private const float ColumnSpacing = 12f;
    private static readonly Color Background = new(0.08f, 0.09f, 0.1f, 0.94f);
    private static readonly Color BodyText = new(0.88f, 0.9f, 0.92f, 1f);

    [SerializeField] private TextMeshProUGUI title;
    [SerializeField] private ScrollRect scroll;
    [SerializeField] private List<TextMeshProUGUI> cells = new();

    private int columns;
    private int usedCells;
    private float cellWidth;

    public bool IsValid => title != null && scroll != null && scroll.content != null;

    public static ProductionInfoTableUI Create(RectTransform root, Vector2 size, TMP_FontAsset font, bool horizontal)
    {
        root.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
        root.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);
        Image background = root.GetComponent<Image>();
        if (background == null)
            background = root.gameObject.AddComponent<Image>();
        background.color = Background;

        ProductionInfoTableUI table = new();
        table.title = CreateLabel(root, "Title", font);
        table.title.fontSize = 18f;
        table.title.fontStyle = FontStyles.Bold;
        table.title.color = Color.white;
        SetAnchors(table.title.rectTransform, new Vector2(0f, 1f), Vector2.one,
            new Vector2(12f, -40f), new Vector2(-12f, -8f));
        table.scroll = CreateScrollView(root, horizontal);
        return table;
    }

    public void Begin(string heading, int columnCount)
    {
        title.text = heading;
        columns = Mathf.Max(1, columnCount);
        usedCells = 0;
        float availableWidth = Mathf.Max(1f, scroll.viewport.rect.width);
        cellWidth = (availableWidth - ColumnSpacing * (columns - 1)) / columns;
        if (scroll.horizontal)
            cellWidth = Mathf.Max(180f, cellWidth);
    }

    public void AddCell(string text, bool heading = false)
    {
        if (usedCells == cells.Count)
            cells.Add(CreateLabel(scroll.content, $"Cell {usedCells}", title.font));

        TextMeshProUGUI cell = cells[usedCells];
        cell.gameObject.SetActive(true);
        cell.text = text;
        cell.fontStyle = heading ? FontStyles.Bold : FontStyles.Normal;
        cell.color = heading ? Color.white : BodyText;
        cell.rectTransform.anchoredPosition = new Vector2(
            usedCells % columns * (cellWidth + ColumnSpacing), -(usedCells / columns) * RowHeight);
        cell.rectTransform.sizeDelta = new Vector2(cellWidth, RowHeight);
        usedCells++;
    }

    public void End()
    {
        for (int i = usedCells; i < cells.Count; i++)
            cells[i].gameObject.SetActive(false);

        int rows = (usedCells + columns - 1) / columns;
        scroll.content.sizeDelta = new Vector2(
            columns * cellWidth + (columns - 1) * ColumnSpacing, rows * RowHeight);

        // Keep the current scroll position, clamping it when rows or columns disappear.
        Vector2 position = scroll.content.anchoredPosition;
        position.x = Mathf.Clamp(position.x, -Mathf.Max(0f, scroll.content.rect.width - scroll.viewport.rect.width), 0f);
        position.y = Mathf.Clamp(position.y, 0f, Mathf.Max(0f, scroll.content.rect.height - scroll.viewport.rect.height));
        scroll.content.anchoredPosition = position;
    }

    private static ScrollRect CreateScrollView(RectTransform root, bool horizontal)
    {
        RectTransform area = CreateRect(root, "Scroll View");
        SetAnchors(area, Vector2.zero, Vector2.one, new Vector2(12f, 12f), new Vector2(-12f, -48f));
        ScrollRect scroll = area.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = horizontal;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = RowHeight;

        RectTransform viewport = CreateRect(area, "Viewport");
        SetAnchors(viewport, Vector2.zero, Vector2.one,
            new Vector2(0f, horizontal ? 14f : 0f), new Vector2(-14f, 0f));
        viewport.gameObject.AddComponent<RectMask2D>();
        // A transparent graphic receives wheel/drag events in the gaps between labels.
        viewport.gameObject.AddComponent<Image>().color = Color.clear;
        scroll.viewport = viewport;
        scroll.content = CreateRect(viewport, "Content");
        scroll.verticalScrollbar = CreateScrollbar(area, false);
        if (horizontal)
            scroll.horizontalScrollbar = CreateScrollbar(area, true);
        return scroll;
    }

    private static Scrollbar CreateScrollbar(RectTransform parent, bool horizontal)
    {
        RectTransform track = CreateRect(parent, horizontal ? "Horizontal Scrollbar" : "Vertical Scrollbar");
        if (horizontal)
            SetAnchors(track, Vector2.zero, Vector2.right, Vector2.zero, new Vector2(-14f, 8f));
        else
            SetAnchors(track, Vector2.right, Vector2.one, new Vector2(-8f, 14f), Vector2.zero);
        track.gameObject.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.05f);
        RectTransform handle = CreateRect(track, "Handle");
        SetAnchors(handle, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Image image = handle.gameObject.AddComponent<Image>();
        image.color = new Color(0.4f, 0.44f, 0.48f, 1f);

        Scrollbar scrollbar = track.gameObject.AddComponent<Scrollbar>();
        scrollbar.handleRect = handle;
        scrollbar.targetGraphic = image;
        scrollbar.direction = horizontal ? Scrollbar.Direction.LeftToRight : Scrollbar.Direction.BottomToTop;
        scrollbar.navigation = new Navigation { mode = Navigation.Mode.None };
        return scrollbar;
    }

    private static TextMeshProUGUI CreateLabel(RectTransform parent, string name, TMP_FontAsset font)
    {
        RectTransform rect = CreateRect(parent, name);
        TextMeshProUGUI label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.font = font != null ? font : TMP_Settings.defaultFontAsset;
        label.fontSize = 14f;
        label.color = BodyText;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Ellipsis;
        label.richText = false;
        label.raycastTarget = false;
        return label;
    }

    private static RectTransform CreateRect(RectTransform parent, string name)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.layer = parent.gameObject.layer;
        RectTransform rect = (RectTransform)obj.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = Vector2.zero;
        return rect;
    }

    private static void SetAnchors(RectTransform rect, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }
}
