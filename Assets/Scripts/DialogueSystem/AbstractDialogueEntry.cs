using UnityEngine;
using UnityEngine.Timeline;

public abstract class AbstractDialogueEntry : ScriptableObject
{
    

    public abstract void Visit (Fabler fabler, FablerDialogueState master);

    public abstract AbstractDialogueEntry Next();

    //public abstract SignalAsset SignalOnExit();

    public abstract SignalAndTarget [] SignalsAndTargets();

    public abstract DialogueEntry GetDialogueEntryVariant();

    public abstract bool NextOnMouseClick();

    public abstract float TimeToLive();

    //public abstract  string SignalReceiver();
}