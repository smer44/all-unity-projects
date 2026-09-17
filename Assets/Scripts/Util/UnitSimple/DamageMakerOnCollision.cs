using UnityEngine;

public class DamageMakerOnCollision : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other == null)
            return;

        GetDamageColorChange damageReceiver = other.GetComponent<GetDamageColorChange>();

        if (damageReceiver != null)
            damageReceiver.GetDamage();
    }
}
