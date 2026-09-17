using UnityEngine;
using UnityEngine.InputSystem;

public class TargetSelector : MonoBehaviour
{
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private PlayerVisualsRotationController rotationController;
    [SerializeField] private string targetTag = "Untagged";
    [SerializeField] private float perpendicularDistanceWeight = 1f;
    [SerializeField] private float forwardDistanceWeight = 0.1f;
    [SerializeField] private GameObject selected;
    [SerializeField] private bool useMouseInput = true;
    [SerializeField] private bool highlightSelection = true;

    private TargetSelectable selectedTargetSelectable;

    public GameObject Selected => selected;
    public Transform CameraTransform => cameraTransform;
    public bool UseMouseInput { get => useMouseInput; set => useMouseInput = value; }

    private void Awake()
    {
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        if (selected != null)
        {
            selected.TryGetComponent(out selectedTargetSelectable);
        }

        UpdateRotationControllerTarget();
    }

    private void Update()
    {
        if (!useMouseInput)
            return;

        Mouse mouse = Mouse.current;
        if (mouse == null || !mouse.middleButton.wasPressedThisFrame)
        {
            return;
        }

        if (selected == null)
        {
            SelectTarget();
            return;
        }

        ClearSelection();
    }

    private void OnDisable()
    {
        ClearSelection();
    }

    private void SelectTarget()
    {
        GameObject target = TargetAssistUtil.GetClosestToCameraForwardLine<TargetSelectable>(
            cameraTransform,
            targetTag,
            perpendicularDistanceWeight,
            forwardDistanceWeight);

        Debug.Log($"TargetSelector: selected target {target}");

        if (target == null || !target.TryGetComponent<TargetSelectable>(out _))
        {
            return;
        }

        SetSelectedTarget(target);
    }

    public void SetSelectedTarget(GameObject target)
    {
        if (selected != target)
        {
            ClearSelection();
            selected = target;
            if (selected != null && highlightSelection
                && selected.TryGetComponent(out selectedTargetSelectable))
            {
                selectedTargetSelectable.OnTargetSelect();
            }
        }

        UpdateRotationControllerTarget();
    }

    private void ClearSelection()
    {
        if (highlightSelection && selectedTargetSelectable != null)
        {
            selectedTargetSelectable.OnTargetDeSelect();
        }

        selected = null;
        selectedTargetSelectable = null;
        UpdateRotationControllerTarget();
    }

    private void UpdateRotationControllerTarget()
    {
        if (rotationController == null)
            rotationController = GetComponentInParent<PlayerVisualsRotationController>();

        if (rotationController == null)
        {
            return;
        }

        rotationController.target = selected;
    }
}
