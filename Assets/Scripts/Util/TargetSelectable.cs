using UnityEngine;

public class TargetSelectable : MonoBehaviour
{
    [SerializeField] private Renderer givenRenderer;
    [SerializeField] private Color selectedColor = Color.yellow;

    private Color previousColor;
    private bool hasPreviousColor;

    public void OnTargetSelect()
    {
        if (givenRenderer == null)
        {
            Debug.LogWarning($"{nameof(TargetSelectable)} on {name} has no renderer assigned.");
            return;
        }

        if (!hasPreviousColor)
        {
            previousColor = givenRenderer.material.color;
            hasPreviousColor = true;
        }

        givenRenderer.material.color = selectedColor;
    }

    public void OnTargetDeSelect()
    {
        if (givenRenderer != null && hasPreviousColor)
        {
            givenRenderer.material.color = previousColor;
        }

        hasPreviousColor = false;
    }
}
