using UnityEngine;

public class TagLayerAssigner : MonoBehaviour
{
    [SerializeField] private string targetTag = "Untagged";
    [SerializeField, Range(0, 31)] private int targetLayer = 0;

    private void Awake()
    {
        if (!string.IsNullOrEmpty(targetTag))
            gameObject.tag = targetTag;

        gameObject.layer = targetLayer;
    }
}
