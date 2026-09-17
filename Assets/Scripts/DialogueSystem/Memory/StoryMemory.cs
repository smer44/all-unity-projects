using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "StoryMemory", menuName = "Fabler/StoryMemory")]
public sealed class StoryMemory : ScriptableObject
{
    [Serializable]
    private struct MemoryPair
    {
        public string key;
        public int value;
    }

    [SerializeField] private List<MemoryPair> initialMemory = new();
    [SerializeField] private AbstractDate currentDate;

    [SerializeField] public string currentLocation;

    [NonSerialized] public Dictionary<string, int> memory = new();
    [NonSerialized] private bool hasWarnedMissingCurrentDate;

    public AbstractDate CurrentDate
    {
        get
        {
            if (currentDate == null && !hasWarnedMissingCurrentDate)
            {
                Debug.LogWarning($"{nameof(StoryMemory)} '{name}' has no current date assigned.", this);
                hasWarnedMissingCurrentDate = true;
            }

            return currentDate;
        }
    }

    public void SetCurrentDate(AbstractDate date)
    {
        currentDate = date;
        hasWarnedMissingCurrentDate = false;
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
        RebuildMemory();
    }

    private void OnValidate()
    {
        RebuildMemory();
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
}
