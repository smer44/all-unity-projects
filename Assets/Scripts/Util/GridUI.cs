using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class GridUI : MonoBehaviour
{
    [SerializeField] private InventoryEntryUI gridItemPrefab;
    [SerializeField] private RectTransform panel;
    [SerializeField, Min(1)] private int xcells = 5;
    [SerializeField, Min(1)] private int ycells = 3;

    private InventoryEntryUI[,] cells;

    public int XCells => xcells;
    public int YCells => ycells;

    private void Start()
    {
        Initialize();
    }

    // The controller may refresh in OnEnable, before this component's Start.
    public bool Initialize()
    {
        if (cells != null)
            return true;

        if (panel == null || gridItemPrefab == null || xcells < 1 || ycells < 1)
        {
            Debug.LogWarning($"{nameof(GridUI)} requires a panel, a cell prefab and positive grid dimensions.", this);
            return false;
        }

        if (!panel.TryGetComponent(out GridLayoutGroup layout) || !layout.enabled)
        {
            Debug.LogWarning($"{nameof(GridUI)} requires an enabled {nameof(GridLayoutGroup)} on its panel.", this);
            return false;
        }

        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = xcells;
        layout.startCorner = GridLayoutGroup.Corner.UpperLeft;
        layout.startAxis = GridLayoutGroup.Axis.Horizontal;

        cells = new InventoryEntryUI[xcells, ycells];
        for (int j = 0; j < ycells; j++)
        {
            for (int i = 0; i < xcells; i++)
            {
                InventoryEntryUI cell = Instantiate(gridItemPrefab, panel, false);
                cell.name = $"Cell ({i}, {j})";
                cell.Entry = null;
                cells[i, j] = cell;
            }
        }

        return true;
    }

    /// <summary>Coordinates start at (0, 0) in the top-left corner.</summary>
    public InventoryEntry GetInventoryEntry(int i, int j)
    {
        return Initialize() && IsInBounds(i, j) ? cells[i, j].Entry : null;
    }

    /// <summary>Replaces the entry at these coordinates. Null empties the cell.</summary>
    public bool SetInventoryEntry(int i, int j, InventoryEntry entry)
    {
        if (!Initialize() || !IsInBounds(i, j))
            return false;

        cells[i, j].Entry = entry;
        return true;
    }

    /// <summary>Fills the first empty cell, left to right, then top to bottom.</summary>
    public bool AddInventoryEntry(InventoryEntry entry)
    {
        if (entry == null || !Initialize())
            return false;

        for (int j = 0; j < cells.GetLength(1); j++)
        {
            for (int i = 0; i < cells.GetLength(0); i++)
            {
                if (cells[i, j].Entry != null)
                    continue;

                cells[i, j].Entry = entry;
                return true;
            }
        }

        return false;
    }

    public void ClearInventoryEntries()
    {
        if (!Initialize())
            return;

        foreach (InventoryEntryUI cell in cells)
            cell.Entry = null;
    }

    private bool IsInBounds(int i, int j)
    {
        return i >= 0 && j >= 0 && i < cells.GetLength(0) && j < cells.GetLength(1);
    }
}
