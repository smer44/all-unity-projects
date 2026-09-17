using UnityEngine;

[CreateAssetMenu(fileName = "QuantityForkSelector", menuName = "Scriptable Objects/Fabler/QuantityForkSelector")]
public class QuantityForkSelector : ForkSelector
{
    public string fieldName;
    
    public override int GetForkVariant()
    {
        return MemoryBehaviour.Get(fieldName);

    }
}
