using UnityEngine;

public class TransportLineVisualizer : MonoBehaviour
{
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private GameObject movingItemPrefab;

    public TransportExecution Execution;

    private GameObject movingItemInstance;

    private void Awake()
    {
        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();
    }

    private void Start()
    {
        if (movingItemPrefab != null)
        {
            movingItemInstance = Instantiate(movingItemPrefab, transform);
        }
    }

    private void Update()
    {
        if (Execution == null)
            return;

        Vector3 fromPosition = Execution.Fro.transform.position;
        Vector3 toPosition = Execution.To.transform.position;

        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, fromPosition);
        lineRenderer.SetPosition(1, toPosition);

        if (movingItemInstance != null)
        {
            float t = 0f;

            if (Execution.AllDistance > 0f)
                t = Execution.CurrentDistance / Execution.AllDistance;

            t = Mathf.Clamp01(t);

            movingItemInstance.transform.position = Vector3.Lerp(fromPosition, toPosition, t);
        }
    }
}