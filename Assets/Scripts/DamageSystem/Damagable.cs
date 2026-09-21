using UnityEngine;

public class Damagable : MonoBehaviour
{
    [SerializeField] private CharacterData data;

    [SerializeField] private string hpTempStatName = "HP";

    public void GetDamage(int damage)
    {
        int oldHp = data.statsTemp.GetStat(hpTempStatName);
        int newHp = oldHp - damage;
        data.statsTemp.SetStat(hpTempStatName, newHp);
    }



}
