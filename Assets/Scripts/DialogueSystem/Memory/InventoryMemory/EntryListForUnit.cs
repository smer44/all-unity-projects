using UnityEngine;
using System.Collections.Generic;

public abstract class EntryListForUnit : ScriptableObject
{
    [SerializeField]
    public string memoryKey;

    [SerializeField]
    public AbstractMemoryEntry[] entries;

    public List<AbstractMemoryEntry> ToList()
    {
        return new List<AbstractMemoryEntry>(entries);
    }

    public abstract string MemoryType();

    public string EntryListKey()
    {
        return MemoryType() + memoryKey;
    }
}
