using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class QuestListUIController : AbstractInventoryUIController
{
    [SerializeField] private PackedBox questListPanel;
    [SerializeField] private QuestEntryUI questEntryPrefab;

    private readonly List<QuestEntryUI> entries = new();

    protected override string MemoryPrefix => MemoryForUnitOfQuests.questPrefix;

    public override bool AddInventoryEntry(AbstractMemoryEntry entry)
    {
        if (entry is not QuestEntry questEntry || questEntry == null
            || questEntryPrefab == null || questListPanel == null
            || !questListPanel.enabled || questListPanel.PackingMode != PackedBoxMode.VBox)
            return false;

        QuestEntryUI row = Instantiate(questEntryPrefab, questListPanel.transform, false);
        row.SetQuestEntry(questEntry);
        entries.Add(row);
        return true;
    }

    public override void ClearInventoryPanel()
    {
        foreach (QuestEntryUI row in entries)
        {
            if (row == null)
                continue;

            // Destroy is deferred in play mode; hide old rows before rebuilding.
            row.gameObject.SetActive(false);
            if (Application.isPlaying)
                Destroy(row.gameObject);
            else
                DestroyImmediate(row.gameObject);
        }

        entries.Clear();
    }
}
