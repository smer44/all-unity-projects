using System.Collections;
using UnityEngine;

public class DestroyTimer : MonoBehaviour
{
    [SerializeField] private float destroyTime = 1f;

    private void Start()
    {
        StartCoroutine(DestroyAfterTime());
    }

    private IEnumerator DestroyAfterTime()
    {
        yield return new WaitForSeconds(destroyTime);

        Destroy(gameObject);
    }
}
