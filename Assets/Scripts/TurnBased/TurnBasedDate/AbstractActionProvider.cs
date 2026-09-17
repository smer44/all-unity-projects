using UnityEngine;

//[CreateAssetMenu(fileName = "AbstractActionFilter", menuName = "Scriptable Objects/AbstractActionFilter")]
public abstract class AbstractActionProvider : ScriptableObject
{  

    public abstract AbstractTurnAction[] GetActionsForDate(StoryMemory memory);


}
