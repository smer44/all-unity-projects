using UnityEngine;
using UnityEngine.Timeline;

[System.Serializable]
public struct SignalAndTarget
{
    public SignalAsset signal;
    public string receiver;
}


[CreateAssetMenu(fileName = "DialogueEntry", menuName = "Scriptable Objects/Fabler/DialogueEntry")]
public class DialogueEntry : AbstractDialogueEntry
{

    public string Who;
    public string What;
    public Sprite WhoImage;

    public ChoiceOptions choiceEntry;
    public AbstractDialogueEntry next;

    public bool nextOnMouseClick;
    public float timeToLive;
    //[SerializeField] private SignalAsset signalOnExit;

    //[SerializeField] private string signalReceiver;
    [SerializeField] private SignalAndTarget[] signalsAndTargets;

    public ValueMemoryChange onEnterMemoryChange;

    public AudioClip audio;

    public override DialogueEntry GetDialogueEntryVariant()
    {
        return this;
    }
    // to do abstract dialogue entry with visit method 
    public override void Visit(Fabler fabler, FablerDialogueState master)
    {

        master.ApplyWho(this);
        master.ApplyWhat(this);
        master.ApplyImage(this);
        master.ApplySound(this);
        if (onEnterMemoryChange != null)
        {
            onEnterMemoryChange.Memorize();
        }


        master.ApplyChoices(fabler, this, HasChoices());


    }

    public override AbstractDialogueEntry Next()
    {
        return next;
    }
    /*/
    public override SignalAsset SignalOnExit()
    {
        return signalOnExit;
    }

    public override string SignalReceiver()
    {
        return signalReceiver;
    }
    /*/
    public override SignalAndTarget[] SignalsAndTargets()
    {
        return signalsAndTargets;
    }

    public override bool NextOnMouseClick()
    {
        return nextOnMouseClick;
    }


    public override float TimeToLive()
    {
        return timeToLive;
    }

    private bool HasChoices()
    {
        return choiceEntry != null &&
                     choiceEntry.texts != null &&
                     choiceEntry.texts.Length > 0;
    }



}

