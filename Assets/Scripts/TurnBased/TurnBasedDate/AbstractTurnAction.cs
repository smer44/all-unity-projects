using UnityEngine;

//[CreateAssetMenu(fileName = "AbstractTurnAction", menuName = "Scriptable Objects/AbstractTurnAction")]
public abstract class AbstractTurnAction : ScriptableObject
{
    public int enetgyCost;
    public string  keyName;

    public abstract void Execute();

    public string KeyName()
    {
        return keyName;
    }
    public int EnergyCost()
    {
        return enetgyCost;
    }

}
