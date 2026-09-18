using UnityEngine;

[CreateAssetMenu(fileName = "QuestEntry", menuName = "Quests/QuestEntry")]
public class QuestEntry : AbstractMemoryEntry
{
    public Sprite sprite;
    public string questName;
    public string questDescription;
}
