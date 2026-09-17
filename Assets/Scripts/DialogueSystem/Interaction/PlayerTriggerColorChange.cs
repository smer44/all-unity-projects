using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class PlayerTriggerColorChange : MonoBehaviour
{
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private Color playerInsideColor = Color.green;
    [SerializeField] private bool restoreOnExit = true;

    private Material runtimeMaterial;
    private Color originalColor;
    private string colorPropertyName;

    private void Reset()
    {
        if (targetRenderer == null)
        {
            targetRenderer = GetComponent<Renderer>();
            if (targetRenderer == null)
            {
                targetRenderer = GetComponentInChildren<Renderer>();
            }
        }

        var triggerCollider = GetComponent<Collider>();
        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
        }
    }

    private void Awake()
    {
        if (targetRenderer == null)
        {
            targetRenderer = GetComponent<Renderer>();
            if (targetRenderer == null)
            {
                targetRenderer = GetComponentInChildren<Renderer>();
            }
        }

        if (targetRenderer == null)
        {
            Debug.LogWarning($"{name} has no Renderer for {nameof(PlayerTriggerColorChange)}.", this);
            return;
        }

        runtimeMaterial = targetRenderer.material;
        colorPropertyName = ResolveColorProperty(runtimeMaterial);
        if (string.IsNullOrEmpty(colorPropertyName))
        {
            Debug.LogWarning($"{name} material has no supported color property.", this);
            runtimeMaterial = null;
            return;
        }

        originalColor = runtimeMaterial.GetColor(colorPropertyName);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other))
        {
            return;
        }

        ApplyColor(playerInsideColor);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!restoreOnExit || !IsPlayer(other))
        {
            return;
        }

        ApplyColor(originalColor);
    }

    private bool IsPlayer(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            return true;
        }

        return other.attachedRigidbody != null && other.attachedRigidbody.CompareTag("Player");
    }

    private void ApplyColor(Color color)
    {
        if (runtimeMaterial == null || string.IsNullOrEmpty(colorPropertyName))
        {
            return;
        }

        runtimeMaterial.SetColor(colorPropertyName, color);
    }

    private string ResolveColorProperty(Material material)
    {
        if (material == null)
        {
            return null;
        }

        if (material.HasProperty("_BaseColor"))
        {
            return "_BaseColor";
        }

        if (material.HasProperty("_Color"))
        {
            return "_Color";
        }

        return null;
    }
}
