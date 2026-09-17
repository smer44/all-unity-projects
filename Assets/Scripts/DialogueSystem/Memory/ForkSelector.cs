using UnityEngine;

//Should not create an abstract class 
//[CreateAssetMenu(fileName = "MemoryOptions", menuName = "Scriptable Objects/MemoryOptions")]
public abstract class ForkSelector : ScriptableObject
{
    

    public abstract int GetForkVariant();

}
