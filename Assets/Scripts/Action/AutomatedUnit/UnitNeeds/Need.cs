using UnityEngine;

public abstract class Need : ScriptableObject
{
    [SerializeField] private string needName;

    public string Name => string.IsNullOrWhiteSpace(needName) ? name : needName;

    public abstract float GetNecessity();

    public abstract void Tick(float delta);
    public abstract void Change(float amount);
}


public class LinearNeed : Need
{
    public float NeedPerSecond;

    public float Nessesity;


    public override float GetNecessity()
    {
        return Nessesity;
    }

    public override void Tick(float delta)
    {
        Nessesity += NeedPerSecond * delta;
    }

    public override void Change(float amount)
    {
        Nessesity += amount;
    }
}
