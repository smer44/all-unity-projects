using UnityEngine;

[CreateAssetMenu(fileName = "NamedValue", menuName = "SO Auto Battler/Named Value")]
public class NamedValue : ScriptableObject
{
    public string valueName;
    public float value;

    public string Name => string.IsNullOrWhiteSpace(valueName) ? name : valueName;
    public float Value
    {
        get => value;
        set => this.value = value;
    }

    public void Set(string newName, float newValue)
    {
        valueName = newName;
        value = newValue;
    }
}
