using UnityEngine;

public class RubicLikeVisuals : MonoBehaviour
{
    [SerializeField]
    private RubicLike rubicLike;

    [SerializeField]
    private MultiMeshLikeRenderer meshRenderer;


    private void Start()
    {
        RebuildVisuals();
    }

    [ContextMenu("Rebuild Visuals")]
    public void RebuildVisuals()
    {
        if (rubicLike == null)
        {
            Debug.LogWarning($"{nameof(RubicLikeVisuals)} has no {nameof(RubicLike)} assigned.", this);
            return;
        }

        if (GetComponent<Renderer>() == null)
        {
            Debug.LogWarning($"{nameof(RubicLikeVisuals)} has no {nameof(MultiMeshLikeRenderer)} assigned.", this);
            return;
        }

        Vector3Int[] coordinates = rubicLike.GetCoordinatesCopy();
        meshRenderer.Rebuild(coordinates);
    }
}