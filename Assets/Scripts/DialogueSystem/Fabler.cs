using UnityEngine;

public sealed class Fabler : MonoBehaviour
{
    [SerializeField] public FablerDialogueState dialogueState;
    [SerializeField] public FreeDialogueState freeState;
    [SerializeField] private AbstractDialogueEntry initialEntry;
    [SerializeField] private bool playOnStart;

    private FablerState currentState;

    //public AbstractFablerState CurrentState => currentState;
    //public FablerDialogueState DialogueState => dialogueState;
    //public FreeDialogueState FreeState => freeState;

    private void Start()
    {   
        dialogueState.HideAll();
        currentState = freeState;
        if (playOnStart)
        {
            StartDialogue(initialEntry);
        }

    }

    private void Update()
    {
        currentState.OnUpdate(this);
    }

    public void ChangeState(FablerState nextState)
    {
        currentState.OnExit(this);
        Debug.Log($"Fabler: changing state to {nextState}");
        currentState = nextState;
        currentState.OnEnter(this);
    }

    public void StartDialogue(AbstractDialogueEntry entry)
    {
        Enter(entry);
    }

    public void Enter(AbstractDialogueEntry entry)
    {
        currentState.EnterEntry(this, entry);

    }


}
