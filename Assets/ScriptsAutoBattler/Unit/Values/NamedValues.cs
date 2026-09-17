using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NamedValues", menuName = "SO Auto Battler/Named Values")]
public class NamedValues : ScriptableObject
{
    [SerializeField] private NamedValue[] values;

    private readonly Dictionary<string, NamedValue> valuesByName = new Dictionary<string, NamedValue>();

    public int Count => values != null ? values.Length : 0;

    private void OnEnable()
    {
        RebuildDictionary();
    }

    private void OnValidate()
    {
        RebuildDictionary();
    }

    public float GetValue(string valueName)
    {
        NamedValue namedValue = GetNamedValue(valueName);
        return namedValue != null ? namedValue.Value : 0f;
    }

    public NamedValue GetNamedValue(string valueName)
    {
        if (string.IsNullOrWhiteSpace(valueName))
            return null;

        if (valuesByName.Count == 0)
            RebuildDictionary();

        valuesByName.TryGetValue(valueName, out NamedValue namedValue);
        return namedValue;
    }

    public NamedValue GetNamedValueAt(int index)
    {
        if (values == null || index < 0 || index >= values.Length)
            return null;

        return values[index];
    }

    public NamedValues CreateRuntimeInstance()
    {
        NamedValues runtimeValues = Instantiate(this);

        if (runtimeValues.values == null)
            return runtimeValues;

        for (int i = 0; i < runtimeValues.values.Length; i++)
        {
            if (runtimeValues.values[i] != null)
                runtimeValues.values[i] = Instantiate(runtimeValues.values[i]);
        }

        runtimeValues.RebuildDictionary();
        return runtimeValues;
    }

    private void RebuildDictionary()
    {
        valuesByName.Clear();

        if (values == null)
            return;

        foreach (NamedValue namedValue in values)
        {
            if (namedValue == null || string.IsNullOrWhiteSpace(namedValue.Name))
                continue;

            valuesByName[namedValue.Name] = namedValue;
            namedValue.Value = Mathf.Max(0f, namedValue.Value);
        }
    }
}
