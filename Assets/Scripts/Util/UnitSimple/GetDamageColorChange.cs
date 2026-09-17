using System.Collections;
using UnityEngine;

public class GetDamageColorChange : MonoBehaviour
{
    [SerializeField] private Renderer givenRenderer;
    [SerializeField] private Color damageColor = Color.red;
    [SerializeField] private float damageColorTime = 0.2f;

    private Color previousColor;
    private bool hasPreviousColor;
    private Coroutine restoreColorCoroutine;

    public void GetDamage()
    {
        
        if (givenRenderer == null)
        {
            Debug.LogWarning($"{nameof(GetDamageColorChange)} on {name} has no renderer assigned.");
            return;
        }

        if (!hasPreviousColor)
        {
            previousColor = givenRenderer.material.color;
            
            hasPreviousColor = true;
        }

        givenRenderer.material.color = damageColor;
        Debug.Log($"{gameObject.name} changed color");

        if (restoreColorCoroutine != null)
            StopCoroutine(restoreColorCoroutine);

        restoreColorCoroutine = StartCoroutine(RestoreColorAfterDelay());
    }

    private IEnumerator RestoreColorAfterDelay()
    {   
        yield return new WaitForSeconds(damageColorTime);

        if (givenRenderer != null && hasPreviousColor)
            givenRenderer.material.color = previousColor;

        Debug.Log($"{gameObject.name} RestoreColorAfterDelay");
        hasPreviousColor = false;
        restoreColorCoroutine = null;
    }
}
