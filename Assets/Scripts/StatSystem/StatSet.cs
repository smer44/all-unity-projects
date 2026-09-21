using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "StatSet", menuName = "Stats/Stat Set")]
public class StatSet : ScriptableObject
{
    [Tooltip("Initial stat values. For duplicate names, the last entry wins.")]
    [SerializeField] private List<StatPair> initialStats = new List<StatPair>();

    private readonly Dictionary<string, int> stats = new Dictionary<string, int>();

    private void OnEnable()
    {
        RebuildDictionary();
    }

    private void OnValidate()
    {
        RebuildDictionary();
    }

    /// <summary>Returns the stat value, or zero if the name is missing or blank.</summary>
    public int GetStat(string statName)
    {
        if (string.IsNullOrWhiteSpace(statName))
            return 0;

        return stats.TryGetValue(statName, out int value) ? value : 0;
    }

    /// <summary>
    /// Adds or updates a runtime stat, clamping its value to zero or greater.
    /// Blank names are ignored. The serialized initial values are unchanged.
    /// </summary>
    public void SetStat(string statName, int value)
    {
        if (string.IsNullOrWhiteSpace(statName))
            return;

        stats[statName] = Mathf.Max(0, value);
    }

    private void RebuildDictionary()
    {
        stats.Clear();

        if (initialStats == null)
            return;

        for (int i = 0; i < initialStats.Count; i++)
        {
            StatPair pair = initialStats[i];
            if (pair == null)
                continue;

            pair.value = Mathf.Max(0, pair.value);

            if (string.IsNullOrWhiteSpace(pair.statName))
                continue;

            stats[pair.statName] = pair.value;
        }
    }
}
