using UnityEngine;

public class AutoBattlingUnitUI : ChildConstructorUISupplier
{
    [SerializeField] private AutoBattlingUnit unit;

    public override NamedValues GetNamedValues()
    {
        return unit != null ? unit.Stats : null;
    }
}

public static class AutoBattlingUnitValueFormatting
{
    public static string ToFormattedValue(this int value)
    {
        return ((float)value).ToFormattedValue();
    }

    public static string ToFormattedValue(this float value)
    {
        if (Mathf.Abs(value) >= 10000f)
            return value.ToString("0.0E+0");

        return value.ToString("0.0");
    }
}
