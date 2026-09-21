using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "StatTempSet", menuName = "Stats/Stat Temp Set")]
public class StatTempSet : ScriptableObject
{
    [Tooltip("Each pair's value is the stat's maximum. Current values start at maximum. For duplicate names, the last entry wins.")]
    [SerializeField] private List<StatPair> maximumStats = new List<StatPair>();

    private readonly Dictionary<string, StatTempPair> stats = new Dictionary<string, StatTempPair>();

    private void OnEnable()
    {
        RebuildDictionary();
    }

    private void OnValidate()
    {
        RebuildDictionary();
    }

    /// <summary>Returns the current value, or zero if the name is missing or blank.</summary>
    public int GetStat(string statName)
    {
        if (string.IsNullOrWhiteSpace(statName))
            return 0;

        return stats.TryGetValue(statName, out StatTempPair stat) ? stat.value : 0;
    }

    /// <summary>
    /// Updates a configured stat's current value, clamped between zero and its maximum.
    /// Missing or blank names are ignored. The serialized maximums are unchanged.
    /// </summary>
    public void SetStat(string statName, int value)
    {
        if (string.IsNullOrWhiteSpace(statName))
            return;

        if (stats.TryGetValue(statName, out StatTempPair stat))
            stat.value = Mathf.Clamp(value, 0, stat.maxValue);
    }

    /// <summary>Restores every current value to its configured maximum.</summary>
    public void FullHeal()
    {
        foreach (StatTempPair stat in stats.Values)
            stat.value = stat.maxValue;
    }

    public bool IsGreaterThenZero(string statName)
    {
        return GetStat(statName) > 0;
    }

    private void RebuildDictionary()
    {
        stats.Clear();

        if (maximumStats == null)
            return;

        foreach (StatPair pair in maximumStats)
        {
            if (pair == null)
                continue;

            pair.value = Mathf.Max(0, pair.value);

            if (string.IsNullOrWhiteSpace(pair.statName))
                continue;

            stats[pair.statName] = new StatTempPair
            {
                statName = pair.statName,
                value = pair.value,
                maxValue = pair.value
            };
        }
    }
}
