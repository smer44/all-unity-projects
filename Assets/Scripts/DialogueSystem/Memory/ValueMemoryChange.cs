using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "ValueMemoryChange", menuName = "Scriptable Objects/Fabler/ValueMemoryChange")]

//adds change value to the memorized value

public class ValueMemoryChange : AbstractMemoryChange
{
    public int change;
    [FormerlySerializedAs("name")]
    public string key;

    public override void Memorize()
    {
      
        MemoryBehaviour.Add(key,change);
    }
}
