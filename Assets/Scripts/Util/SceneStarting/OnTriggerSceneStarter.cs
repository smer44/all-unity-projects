using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class OnTriggerSceneStarter : SceneStarter
{
    [SerializeField] private string sceneName;
    [SerializeField] private string triggeringTag = "Player";

    private void Reset()
    {
        var triggerCollider = GetComponent<Collider>();
        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsMatchingTrigger(other))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning($"{nameof(OnTriggerSceneStarter)} on '{name}' has no scene name assigned.", this);
            return;
        }

        StartScene(sceneName);
    }

    private bool IsMatchingTrigger(Collider other)
    {
        if (other == null || string.IsNullOrWhiteSpace(triggeringTag))
        {
            return false;
        }

        if (other.CompareTag(triggeringTag))
        {
            return true;
        }

        return other.attachedRigidbody != null && other.attachedRigidbody.CompareTag(triggeringTag);
    }
}
