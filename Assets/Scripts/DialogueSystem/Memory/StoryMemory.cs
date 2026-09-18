using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Story memory what holds memory variables what may represent scenario branches what you are in 
/// Also inventory for different units stored by the string inventory keys.
/// </summary>
[CreateAssetMenu(fileName = "StoryMemory", menuName = "Memory/StoryMemory")]
public sealed class StoryMemory : ScriptableObject
{
    [Serializable]
    private struct MemoryPair
    {
        public string key;
        public int value;
    }

    [Tooltip("List of memory entries for units")]
    [SerializeField] public EntryListForUnit[] memoryForUnitList;

    [NonSerialized] public Dictionary<string, EntryListForUnit> memoryForUnitDict = new();

    [SerializeField] private List<MemoryPair> initialMemory = new();
    [SerializeField] private AbstractDate currentDate;

    [SerializeField] public string currentLocation;

    [Tooltip("List of memory variables")]
    [NonSerialized] public Dictionary<string, int> memory = new();

    public AbstractDate CurrentDate
    {
        get
        {
            if (currentDate == null)
            {
                Debug.LogWarning($"{nameof(StoryMemory)} '{name}' has no current date assigned.", this);
            }

            return currentDate;
        }
    }

    

    public void SetCurrentDate(AbstractDate date)
    {
        currentDate = date;
    }

    public void AdvanceToNextDay()
    {
        AbstractDate date = CurrentDate;
        if (date == null)
            return;

        SetCurrentDate(date.NewNextDay());
    }

    public Dictionary<string, int> GetMemory()
    {
        if (memory == null)
        {
            memory = new Dictionary<string, int>();
        }
        return memory;
    }


    private void OnEnable()
    {
        RebuildAllMemory();
    }

    private void OnValidate()
    {
        RebuildAllMemory();
    }

    private void RebuildAllMemory()
    {
        RebuildMemory();
        RebuildInventoryMemory();
    }

    private void RebuildMemory()
    {
        memory ??= new Dictionary<string, int>();
        memory.Clear();

        if (initialMemory == null)
            return;

        for (int i = 0; i < initialMemory.Count; i++)
        {
            var pair = initialMemory[i];
            if (string.IsNullOrEmpty(pair.key))
            {
                Debug.LogWarning(
                    $"{nameof(StoryMemory)} '{name}' has an empty key at index {i}. Entry skipped.",
                    this);
                continue;
            }

            if (memory.ContainsKey(pair.key))
            {
                Debug.LogWarning(
                    $"{nameof(StoryMemory)} '{name}' has a duplicate key '{pair.key}'. The last value wins.",
                    this);
            }

            memory[pair.key] = pair.value;
        }
    }

    private void RebuildInventoryMemory()
    {
        memoryForUnitDict.Clear();
        if(memoryForUnitList == null)
            return;
        foreach(EntryListForUnit inv in memoryForUnitList)
        {
            if (inv == null)
            {
                continue;
            }
            string listKey = inv.EntryListKey();

            if (string.IsNullOrEmpty(listKey))
            {
                Debug.LogWarning(
                    $"{nameof(StoryMemory)} '{name}' has an empty key at  {inv}. Entry skipped.",
                    this); 
                continue;               
            }

            if (memoryForUnitDict.ContainsKey(listKey))
            {
                Debug.LogWarning(
                    $"{nameof(StoryMemory)} '{name}' has a duplicate key at '{inv}'. The first value wins.",
                    this);                
                continue;
            }

            Debug.Log($"StoryMemory.RebuildInventoryMemory set key {listKey}");
            memoryForUnitDict[listKey] = inv;
        }
    }
}
