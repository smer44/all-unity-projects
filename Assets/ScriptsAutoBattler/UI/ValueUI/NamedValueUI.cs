using TMPro;
using UnityEngine;

public class NamedValueUI : ValueUI
{
    public TMP_Text nameText;
    public NamedValue namedValue;

    private void Update()
    {
        if (namedValue == null)
            return;

        if (nameText != null)
            nameText.text = namedValue.Name;

        if (valueText != null)
            valueText.text = namedValue.Value.ToFormattedValue();
    }
}
