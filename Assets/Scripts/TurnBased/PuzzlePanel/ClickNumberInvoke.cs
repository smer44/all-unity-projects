using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class ClickNumberInvoke : MonoBehaviour
{
    /// <summary>
    /// Fired when a GameObject with ClickableNumber is clicked.
    /// Payload: the ClickableNumber.Number value.
    /// </summary>
    public static event Action<int> ClickedNumber;

    [SerializeField] private LayerMask hitLayers = ~0; // default: everything
    [SerializeField] private float maxDistance = 1000f;


    private void Update()
    {
        if (Mouse.current  == null)
        {
            Debug.LogWarning($"{nameof(ClickNumberInvoke)}: Mouse.current  is null.");
            return;
        }

        if (!Mouse.current.leftButton.wasPressedThisFrame)
            return;

        var raycastCamera = Camera.main;

        if (raycastCamera == null)
        {
            Debug.LogWarning($"{nameof(ClickNumberInvoke)}: Camera.main is null.");
            return;
        }

        Vector2 screenPos = Mouse.current.position.ReadValue();

        Ray ray = raycastCamera.ScreenPointToRay(screenPos);

        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, hitLayers, QueryTriggerInteraction.Ignore))
        {
            // You can choose hit.collider.gameObject OR walk up to parent:
            // var clickable = hit.collider.GetComponentInParent<ClickableNumber>();
            var clickable = hit.collider.GetComponent<ClickableNumber>();

            if (clickable != null)
            {
                //Debug.Log($"ClickNumberInvoke : invoked with {ClickedNumber != null}, {clickable.Number}");
                ClickedNumber?.Invoke(clickable.Number);
            }
        }
    }



}
