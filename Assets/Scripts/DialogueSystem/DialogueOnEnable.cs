using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class DialogueOnEnable : MonoBehaviour
{
    [SerializeField] private Fabler fabler;
    [SerializeField] private AbstractDialogueEntry entry;

    private Coroutine startDialogueRoutine;

    private void OnEnable()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        if (startDialogueRoutine != null)
        {
            StopCoroutine(startDialogueRoutine);
        }

        startDialogueRoutine = StartCoroutine(StartDialogueNextFrame());
    }

    private void OnDisable()
    {
        if (startDialogueRoutine == null)
        {
            return;
        }

        StopCoroutine(startDialogueRoutine);
        startDialogueRoutine = null;
    }

    private IEnumerator StartDialogueNextFrame()
    {
        // Fabler initializes its active state in Start(), so wait one frame before starting dialogue.
        yield return null;
        startDialogueRoutine = null;
        TryStartDialogue();
    }

    public bool TryStartDialogue(Fabler dialogueController = null)
    {
        if (entry == null)
        {
            Debug.LogWarning($"{nameof(DialogueOnEnable)} on {gameObject.name} has no {nameof(AbstractDialogueEntry)} assigned.");
            return false;
        }

        dialogueController ??= GetFabler();
        if (dialogueController == null)
        {
            Debug.LogWarning($"{nameof(DialogueOnEnable)} on {gameObject.name} could not find a {nameof(Fabler)}.");
            return false;
        }

        dialogueController.StartDialogue(entry);
        return true;
    }

    private Fabler GetFabler()
    {
        if (fabler == null)
        {
            fabler = FindAnyObjectByType<Fabler>();
        }

        return fabler;
    }
}
