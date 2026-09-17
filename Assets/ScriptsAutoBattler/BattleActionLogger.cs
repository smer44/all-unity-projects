using UnityEngine;

public class BattleActionLogger : MonoBehaviour
{
    [SerializeField] private BattleActionLogEntry[] entries;

    public BattleActionLogEntry[] Entries => entries;

    public void Log(string author, string entry)
    {
        if (string.IsNullOrWhiteSpace(entry))
            return;

        int oldLength = entries != null ? entries.Length : 0;
        BattleActionLogEntry[] newEntries = new BattleActionLogEntry[oldLength + 1];

        for (int i = 0; i < oldLength; i++)
            newEntries[i] = entries[i];

        newEntries[oldLength] = new BattleActionLogEntry(author, entry);
        entries = newEntries;
    }
}

[System.Serializable]
public struct BattleActionLogEntry
{
    [SerializeField] private string author;
    [SerializeField] private string entry;

    public BattleActionLogEntry(string author, string entry)
    {
        this.author = author;
        this.entry = entry;
    }

    public string Author => author;
    public string Entry => entry;
}
