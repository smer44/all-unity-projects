using System.Collections.Generic;
using UnityEngine;

public class BattleActionLoggerUI : MonoBehaviour
{
    [SerializeField] private BattleActionLogger logger;
    [SerializeField] private RectTransform root;
    [SerializeField] private GameObject entryPrefab;
    [SerializeField] private bool isMinimized;

    private readonly List<GameObject> spawnedEntries = new List<GameObject>();
    private int visibleEntryCount = -1;
    private bool wasMinimized;

    private void Start()
    {
        Refresh();
    }

    private void Update()
    {
        int entryCount = logger != null && logger.Entries != null ? logger.Entries.Length : 0;

        if (entryCount != visibleEntryCount || isMinimized != wasMinimized)
            Refresh();
    }

    public void ToggleMinimize()
    {
        isMinimized = !isMinimized;
        Refresh();
    }

    private void Refresh()
    {
        if (root == null)
            root = transform as RectTransform;

        if (root == null || entryPrefab == null)
            return;

        ClearRoot();

        BattleActionLogEntry[] entries = logger != null ? logger.Entries : null;
        visibleEntryCount = entries != null ? entries.Length : 0;
        wasMinimized = isMinimized;

        if (entries == null)
            return;

        if (isMinimized)
        {
            if (entries.Length > 0)
                SpawnEntry(entries[entries.Length - 1]);

            return;
        }

        for (int i = 0; i < entries.Length; i++)
            SpawnEntry(entries[i]);
    }

    private void SpawnEntry(BattleActionLogEntry entry)
    {
        GameObject child = Instantiate(entryPrefab, root);
        spawnedEntries.Add(child);

        NamedValueUI namedValueUI = child.GetComponent<NamedValueUI>();
        if (namedValueUI == null)
            return;

        namedValueUI.namedValue = null;

        if (namedValueUI.nameText != null)
            namedValueUI.nameText.text = entry.Author;

        if (namedValueUI.valueText != null)
            namedValueUI.valueText.text = entry.Entry;
    }

    private void ClearRoot()
    {
        spawnedEntries.Clear();

        for (int i = root.childCount - 1; i >= 0; i--)
            Destroy(root.GetChild(i).gameObject);
    }
}
