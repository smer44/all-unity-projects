using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class QuestEntryUI : MonoBehaviour
{
    [SerializeField] private TMP_Text questNameText;
    [SerializeField] private TMP_Text questDescriptionText;

    public QuestEntry Entry { get; private set; }

    public void SetQuestEntry(QuestEntry entry)
    {
        Entry = entry;
        if (questNameText != null)
            questNameText.text = entry != null ? entry.questName : string.Empty;
        if (questDescriptionText != null)
            questDescriptionText.text = entry != null ? entry.questDescription : string.Empty;
    }
}
