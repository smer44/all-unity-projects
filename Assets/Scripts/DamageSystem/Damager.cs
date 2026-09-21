using UnityEngine;

public class Damager : MonoBehaviour
{
    [SerializeField] private int damage = 10;

    [SerializeField] private bool destroyOnCollide;

    /*
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.TryGetComponent<Damagable>(out var damagable))
        {
            damagable.GetDamage(damage);
        }
    }
    */


    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<Damagable>(out var damagable))
            damagable.GetDamage(damage);

        if(destroyOnCollide)
            Destroy(this.gameObject);
    }
}
