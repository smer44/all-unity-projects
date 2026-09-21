using UnityEngine;

[CreateAssetMenu(fileName = "CharacterData", menuName = "Character/CharacterData")]
public class CharacterData : ScriptableObject
{
    [SerializeField] public string characterID;
    [SerializeField] public string displayName;

    [SerializeField] public string fraction;

    [SerializeField] public StatSet stats;

    [SerializeField] public StatTempSet statsTemp;




}
