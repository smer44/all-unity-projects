using UnityEngine;

//[CreateAssetMenu(fileName = "AbstractDate", menuName = "Scriptable Objects/AbstractDate")]
public abstract class AbstractDate : ScriptableObject
{
    public abstract int Days();

    public abstract int DayOfWeek();

    public abstract AbstractDate NewNextDay();

    public abstract string PP();
}
