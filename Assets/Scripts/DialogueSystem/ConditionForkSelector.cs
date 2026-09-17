using UnityEngine;

[CreateAssetMenu(fileName = "ConditionForkSelector", menuName = "Scriptable Objects/Fabler/ConditionForkSelector")]
public class ConditionForkSelector : ForkSelector
{
    public string fieldName;
    public ConditionType condition;
    public int value;

    public enum ConditionType
    {
        Less,
        More,
        Equal,
        NonEqual
    }

    public override int GetForkVariant()
    {
        if (!MemoryBehaviour.TryGet(fieldName, out int memoryValue))
        {
            return 0;
        }

        return Matches(memoryValue) ? 1 : 0;
    }

    private bool Matches(int memoryValue)
    {
        return condition switch
        {
            ConditionType.Less => memoryValue < value,
            ConditionType.More => memoryValue > value,
            ConditionType.Equal => memoryValue == value,
            ConditionType.NonEqual => memoryValue != value,
            _ => false
        };
    }
}
