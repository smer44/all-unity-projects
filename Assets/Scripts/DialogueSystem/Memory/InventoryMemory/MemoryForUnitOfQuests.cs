using UnityEngine;

[CreateAssetMenu(fileName = "MemoryForUnitOfQuests", menuName = "Quests/MemoryForUnitOfQuests")]
public class MemoryForUnitOfQuests : EntryListForUnit
{
    public const string questPrefix = "Quests_";

    public override string MemoryType()
    {
        return questPrefix;
    }
}
