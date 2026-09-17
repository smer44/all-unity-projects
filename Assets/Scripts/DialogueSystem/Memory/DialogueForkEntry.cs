using UnityEngine;
using UnityEngine.Timeline;

[CreateAssetMenu(fileName = "DialogueForkEntry", menuName = "Scriptable Objects/Fabler/DialogueForkEntry")]
public class DialogueForkEntry : AbstractDialogueEntry
{
    public ForkSelector selector;


    public AbstractDialogueEntry[] transitions;


    public override DialogueEntry GetDialogueEntryVariant()
    {
        int variant = selector.GetForkVariant();
        if ( 0 <= variant && variant  < transitions.Length)
        {
            return transitions[variant].GetDialogueEntryVariant();   
        }
        return null;
        
    }


    public override void Visit(Fabler fabler, FablerDialogueState master)
    {
        GetDialogueEntryVariant().Visit(fabler, master);
    }

    public override AbstractDialogueEntry Next()
    {
        return GetDialogueEntryVariant().Next();
    }    

    /*/
    public override SignalAsset SignalOnExit()
    {
        return GetDialogueEntryVariant().SignalOnExit();
    }   

    public override string SignalReceiver()
    {
        return GetDialogueEntryVariant().SignalReceiver();
    }
    /*/
    public override SignalAndTarget [] SignalsAndTargets()
    {
        return GetDialogueEntryVariant().SignalsAndTargets();
    }

    public override bool NextOnMouseClick()
    {
        return GetDialogueEntryVariant().NextOnMouseClick();
    }


    public override float TimeToLive()
    {
        return GetDialogueEntryVariant().TimeToLive();
    }    
    
}
