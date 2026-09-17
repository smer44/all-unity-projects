using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(100)]
public class LookOnTargetOrMiddle : MonoBehaviour
{
    [SerializeField] private TargetSelector targetSelector;
    [SerializeField, Min(1f)] private float aimDistance = 1000f;
    [SerializeField] private LayerMask aimLayers = Physics.DefaultRaycastLayers;

    private SpawnTimerLoop spawner;
    private Transform playerRoot;

    private void Awake()
    {
        PlayerController player = GetComponentInParent<PlayerController>();
        if (player != null)
        {
            playerRoot = player.transform;
            if (targetSelector == null)
            {
                targetSelector = player.GetComponentInChildren<TargetSelector>(true);
            }
        }
    }

    private void OnEnable()
    {
        spawner = GetComponent<SpawnTimerLoop>();
        if (spawner != null)
        {
            spawner.BeforeSpawn += UpdateAim;
        }
    }

    private void OnDisable()
    {
        if (spawner != null)
        {
            spawner.BeforeSpawn -= UpdateAim;
        }
    }

    private void LateUpdate()
    {
        UpdateAim();
    }

    public void UpdateAim()
    {
        Camera aimCamera = GetAimCamera();
        GameObject selected = targetSelector != null ? targetSelector.Selected : null;
        Vector3 aimPoint;
        if (selected != null)
        {
            aimPoint = selected.transform.position;
        }
        else if (aimCamera != null)
        {
            Ray ray = aimCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            float closestDistance = Mathf.Max(1f, aimDistance);
            aimPoint = ray.GetPoint(closestDistance);
            foreach (RaycastHit hit in Physics.RaycastAll(ray, closestDistance, aimLayers, QueryTriggerInteraction.Ignore))
            {
                // The camera can sit behind the player and the weapon's colliders.
                if (playerRoot != null && hit.transform.IsChildOf(playerRoot))
                {
                    continue;
                }

                if (hit.distance < closestDistance)
                {
                    closestDistance = hit.distance;
                    aimPoint = hit.point;
                }
            }
        }
        else
        {
            return;
        }

        Vector3 direction = aimPoint - transform.position;
        if (direction.sqrMagnitude > Mathf.Epsilon)
        {
            Vector3 up = aimCamera != null ? aimCamera.transform.up : Vector3.up;
            transform.rotation = Quaternion.LookRotation(direction, up);
        }
    }

    private Camera GetAimCamera()
    {
        if (targetSelector != null && targetSelector.CameraTransform != null
            && targetSelector.CameraTransform.TryGetComponent(out Camera selectedCamera)
            && selectedCamera.isActiveAndEnabled)
        {
            return selectedCamera;
        }

        return Camera.main;
    }
}
