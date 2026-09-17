[System.Serializable]
public class TransportExecution
{
    public Storage Fro;
    public Storage To;
    public AmountOf<ItemDefinition> Package;
    public float AllDistance;
    public float CurrentDistance;

    public TransportExecution(Storage fro, Storage to, AmountOf<ItemDefinition> package, float allDistance)
    {
        Fro = fro;
        To = to;
        Package = package;
        AllDistance = allDistance;
        CurrentDistance = 0f;
    }
}