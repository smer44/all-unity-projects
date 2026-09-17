using System.Collections;
using UnityEngine;

public class SpawnTimerLoop : MonoBehaviour
{
    public event System.Action BeforeSpawn;

    [SerializeField] private GameObject prefab;
    [SerializeField] private float spawnInterval = 1f;

    [SerializeField] private bool spawnBeforeWait;

    private Coroutine spawnCoroutine;
    public bool active = false;

    public void StartOrContinueSpawn()
    {
        if (spawnCoroutine != null)
            return;

        active = true;
        spawnCoroutine = StartCoroutine(SpawnLoop());
    }

    public void StopSpawn()
    {
        active = false;
        if (spawnCoroutine == null)
            return;

        StopCoroutine(spawnCoroutine);
        spawnCoroutine = null;
    }

    protected virtual void OnDisable()
    {
        StopSpawn();
    }

    private IEnumerator SpawnLoop()
    {
        while (active)
        {   
            if (spawnBeforeWait)
            {   
                Spawn();
                yield return new WaitForSeconds(spawnInterval);
                
            }
            else
            {
                yield return new WaitForSeconds(spawnInterval);
                Spawn();               

            }

        }
    }

    private void Spawn()
    {
        if (prefab == null)
            return;

        BeforeSpawn?.Invoke();
        Instantiate(prefab, transform.position, transform.rotation);
    }
}
