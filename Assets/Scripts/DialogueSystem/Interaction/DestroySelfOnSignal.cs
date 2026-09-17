using UnityEngine;

public class DestroySelfOnSignal : MonoBehaviour
{
    public void DestroySelf()
    {
        Destroy(gameObject);
    }
}