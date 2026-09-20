using UnityEngine;

[DisallowMultipleComponent]
public class TogglerOfGameObject : MonoBehaviour
{
    [SerializeField] private GameObject targetObject;
    [SerializeField] private bool disableOnStart;

    private void Start()
    {
        if (disableOnStart)
        {
            SetTargetActive(false);
        }
    }

    public virtual void EnableTargetObject()
    {
        SetTargetActive(true);
    }

    public virtual void DisableTargetObject()
    {
        SetTargetActive(false);
    }
    public void ToggleTargetObject()
    {
        GameObject target = GetTargetObject();
        if (target == null)
        {
            return;
        }

        target.SetActive(!target.activeSelf);
    }

    public void SetTargetActive(bool isActive)
    {
        GameObject target = GetTargetObject();

        Debug.Log($"TogglerOfGameObject {this.name} SetTargetActive {isActive} for GameObject {target}");

        if (target == null)
        {
            return;
        }

        target.SetActive(isActive);
    }

    private GameObject GetTargetObject()
    {
        if (targetObject != null)
        {
            return targetObject;
        }

        Debug.LogWarning($"{nameof(TogglerOfGameObject)} on '{gameObject.name}' has no targetObject assigned.", this);
        return null;
    }
}
