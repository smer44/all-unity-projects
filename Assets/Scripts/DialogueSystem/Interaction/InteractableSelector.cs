using System.Collections.Generic;
using UnityEngine;

public class InteractableSelector : MonoBehaviour
{
    public Transform PlayerTransform;
    public Transform InteractionPivotTransform;

    [SerializeField] private GameObject selected;

    private readonly List<AbstractInteractable> potentialInteractables = new List<AbstractInteractable>();

    public GameObject Selected => selected;

    private void OnTriggerEnter(Collider other)
    {
        if (PlayerTransform == null || !other.TryGetComponent<AbstractInteractable>(out var interactable))
        {
            return;
        }

        // Admission uses the player's local +Z direction expressed in world space.
        var toInteractable = interactable.transform.position - PlayerTransform.position;
        if (Vector3.Dot(PlayerTransform.forward, toInteractable) <= 0f)
        {
            return;
        }

        if (!potentialInteractables.Contains(interactable))
        {
            potentialInteractables.Add(interactable);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent<AbstractInteractable>(out var interactable))
        {
            potentialInteractables.Remove(interactable);
        }
    }

    private void Update()
    {
        selected = GetBestInteractable();
    }

    public GameObject GetBestInteractable()
    {
        if (InteractionPivotTransform == null)
        {
            return null;
        }

        GameObject closest = null;
        float closestSqrDistance = float.PositiveInfinity;
        var pivotPosition = InteractionPivotTransform.position;

        for (int i = 0; i < potentialInteractables.Count; i++)
        {
            var interactable = potentialInteractables[i];
            if (interactable == null || !interactable.gameObject.activeInHierarchy)
            {
                potentialInteractables.RemoveAt(i);
                i--;
                continue;
            }

            float sqrDistance = (interactable.transform.position - pivotPosition).sqrMagnitude;
            if (sqrDistance < closestSqrDistance)
            {
                closestSqrDistance = sqrDistance;
                closest = interactable.gameObject;
            }
        }

        return closest;
    }

    private void OnDisable()
    {
        potentialInteractables.Clear();
        selected = null;
    }
}
