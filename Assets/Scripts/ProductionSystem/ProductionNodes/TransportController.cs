using System.Collections.Generic;
using UnityEngine;

public class TransportController : MonoBehaviour
{
    [SerializeField] public float TransportSpeed = 1f;


    [Header("Visualization")]
    [SerializeField] private TransportLineVisualizer transportLineVisualizerPrefab;

    [Header("Runtime")]
    [SerializeField] public List<TransportExecution> Work = new();


    private readonly Dictionary<TransportExecution, TransportLineVisualizer> visualizers = new();



    private void Update()
    {
        TickTransports(Time.deltaTime);
    }




    public void TickTransports(float deltaTime)
    {
        float deltaDistance = TransportSpeed * deltaTime;

        for (int i = Work.Count - 1; i >= 0; i--)
        {

            TickTransport(deltaDistance, i);
        }
    }


    public void TickTransport(float deltaDistance, int i)
    {

        TransportExecution execution = Work[i];
        execution.CurrentDistance += deltaDistance;

        if (execution.CurrentDistance < execution.AllDistance)
            return;

        // Put() may return false on overflow, but the package is still stored.
        StorageFunctions.Put(
            execution.To.StorageSize.Capacities,
            execution.To.StoredItems,
            execution.Package);

        RemoveVisualizer(execution);
        Work.RemoveAt(i);


    }

    public bool Send(Storage fro, Storage to, AmountOf<ItemDefinition> package)
    {
        if (fro == null || to == null || package == null || package.Item == null || package.Amount <= 0f)
            return false;

        AmountOf<ItemDefinition> packageSnapshot = new AmountOf<ItemDefinition>
        {
            Item = package.Item,
            Amount = package.Amount
        };

        if (!StorageFunctions.CanGet(fro.StoredItems, packageSnapshot))
            return false;

        if (!StorageFunctions.CanPut(to.StorageSize.Capacities, to.StoredItems, packageSnapshot))
            return false;

        StorageFunctions.Get(fro.StoredItems, packageSnapshot);

        float distance = Vector3.Distance(fro.transform.position, to.transform.position);

        TransportExecution execution = new TransportExecution(fro, to, package, distance);

        Work.Add(execution);
        CreateVisualizer(execution);
        return true;
    }

    private void CreateVisualizer(TransportExecution execution)
    {
        TransportLineVisualizer visualizer = Instantiate(transportLineVisualizerPrefab, transform);
        visualizer.Execution = execution;
        visualizers.Add(execution, visualizer);
    }

    private void RemoveVisualizer(TransportExecution execution)
    {
        if (!visualizers.TryGetValue(execution, out TransportLineVisualizer visualizer))
            return;

        if (visualizer != null)
            Destroy(visualizer.gameObject);

        visualizers.Remove(execution);
    }


}
