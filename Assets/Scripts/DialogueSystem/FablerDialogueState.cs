using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Timeline;

[Serializable]
public sealed class FablerDialogueState : FablerState
{
    [SerializeField] private DialougeUI dialougeUI;

    [SerializeField] private SignalReceiver signalReceiver;

    [SerializeField] private AudioSource audioSource;
    [SerializeField] private float minimumTime = 2f;

    private AbstractDialogueEntry currentEntry;
    private bool hasChoices;
    private Coroutine transitionCoroutine;

    private bool MouseAdvance = true;


    public void SetMouseAdvance(bool adv)
    {
        MouseAdvance = adv;
    }

    public override void OnUpdate(Fabler fabler)
    {
    }

    private AbstractDialogueEntry GetDialogueEntryVariant(AbstractDialogueEntry entry)
    {
        if (entry == null)
        {
            return entry;
        }
        return entry.GetDialogueEntryVariant();
    }

    public override void EnterEntry(Fabler fabler, AbstractDialogueEntry entry)
    {
        StopTransitionQuery();
        currentEntry = GetDialogueEntryVariant(entry);

        if (currentEntry == null)
        {
            HideAll();
            fabler.ChangeState(fabler.freeState);
            return;
        }

        currentEntry.Visit(fabler, this);
        StartTransitionQuery(fabler, currentEntry);
    }

    public override void OnExit(Fabler fabler)
    {
        StopTransitionQuery();
    }



    public void ApplySound(DialogueEntry entry)
    {
        if (entry == null || entry.audio == null)
        {
            return;
        }

        if (audioSource == null)
        {
            Debug.LogWarning($"FablerDialogueState:ApplySound failed: AudioSource is not assigned but entry {entry} has audio");
            return;
        }

        audioSource.clip = entry.audio;
        audioSource.Play();



    }

    public void ApplyWho(DialogueEntry entry)
    {
        if (dialougeUI != null)
            dialougeUI.ApplyWho(entry.Who);
    }

    public void ApplyWhat(DialogueEntry entry)
    {
        if (dialougeUI != null)
            dialougeUI.ApplyWhat(entry.What);
    }

    public void ApplyImage(DialogueEntry entry)
    {
        if (dialougeUI != null)
            dialougeUI.ApplyImage(entry.WhoImage);
    }


    public void ApplyChoices(Fabler fabler, DialogueEntry entry, bool hasChoicesInput)
    {
        hasChoices = hasChoicesInput;
        if (dialougeUI != null)
            dialougeUI.SetChoicesButtonsActive(hasChoices);

        if (!hasChoices || dialougeUI == null || !dialougeUI.CanCreateChoiceButtons)
            return;

        ChoiceOptions choiceEntry = entry.choiceEntry;


        MouseVisibilityController.Instance.BackupCursor();
        MouseVisibilityController.Instance.ActivateCursor();

        var transitions = choiceEntry.transitions;

        for (int i = 0; i < choiceEntry.texts.Length; i++)
        {
            string choiceText = choiceEntry.texts[i] ?? string.Empty;
            DialogueEntry nextEntry = transitions != null && i < transitions.Length ? transitions[i] : null;

            var button = dialougeUI.CreateChoiceButton();
            if (button == null)
                return;

            var buttonText = button.GetComponentInChildren<TMP_Text>();
            if (buttonText != null)
                buttonText.text = choiceText;

            button.onClick.AddListener(() =>
            {
                MouseVisibilityController.Instance.RestoreCursor();
                ClearChoiceButtons();
                AdvanceTo(fabler, nextEntry);
            });
        }
    }

    private void AdvanceTo(Fabler fabler, AbstractDialogueEntry nextEntry)
    {
        StopTransitionQuery();
        var exitingEntry = currentEntry;
        InvokeAllExitSignals(exitingEntry);
        fabler.Enter(nextEntry);
    }

    private void StartTransitionQuery(Fabler fabler, AbstractDialogueEntry entry)
    {
        if (fabler == null || entry == null || hasChoices)
        {
            return;
        }

        transitionCoroutine = StartCoroutine(QueryTransition(fabler, entry));
    }

    private void StopTransitionQuery()
    {
        if (transitionCoroutine == null)
        {
            return;
        }

        StopCoroutine(transitionCoroutine);
        transitionCoroutine = null;
    }

    private IEnumerator QueryTransition(Fabler fabler, AbstractDialogueEntry entry)
    {
        if (entry.NextOnMouseClick())
        {
            while (currentEntry == entry)
            {
                var mouse = Mouse.current;
                if (mouse == null || !mouse.leftButton.isPressed)
                {
                    break;
                }

                yield return null;
            }

            while (currentEntry == entry)
            {
                if (MouseAdvance && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                {
                    break;
                }

                yield return null;
            }
        }
        else
        {
            var transitionDelay = Mathf.Max(entry.TimeToLive(), minimumTime);
            yield return new WaitForSeconds(transitionDelay);
        }

        transitionCoroutine = null;

        if (fabler == null || currentEntry != entry || hasChoices)
        {
            yield break;
        }

        AdvanceTo(fabler, entry.Next());
    }

    private void InvokeAllExitSignals(AbstractDialogueEntry nextEntry)
    {
        if (nextEntry == null)
        {
            return;
        }
        var pairs = nextEntry.SignalsAndTargets();
        if( pairs == null)
        {
            return;
        }
        foreach (var pair in pairs)
        {
            InvokeExitSignal(pair.signal, pair.receiver);
        }
    }

    private void InvokeExitSignal(SignalAsset signal,  string signalReceiverName)
    {

        var signalOnExit = signal;

        if (signalOnExit == null)
        {
            return;
        }

        signalReceiver = FindSignalReceiverInCurrentScene(signalReceiverName);
        
        if (signalReceiver == null)
        {
            Debug.LogWarning(
                $"{nameof(FablerDialogueState)}.{nameof(InvokeExitSignal)} skipped: " +
                $"could not find signalReceiver for{signal} to {signalReceiverName} in scene {gameObject.scene.name}",
                this);
            return;
        }

        var reaction = signalReceiver.GetReaction(signalOnExit);
        if (reaction == null)
        {
            Debug.LogWarning(
                $"{nameof(FablerDialogueState)}.{nameof(InvokeExitSignal)} skipped: " +
                $"{nameof(signalReceiver)} '{signalReceiver.gameObject.name}' has no reaction bound for signal '{signalOnExit.name}'",
                signalReceiver);
            return;
        }

        reaction.Invoke();
    }

    private SignalReceiver FindSignalReceiverInCurrentScene(string signalReceiverName)
    {
        if (string.IsNullOrWhiteSpace(signalReceiverName))
        {
            return null;
        }

        var scene = gameObject.scene;
        var receivers = FindObjectsByType<SignalReceiver>(FindObjectsInactive.Include);
        SignalReceiver foundReceiver = null;
        int matchCount = 0;

        for (int i = 0; i < receivers.Length; i++)
        {
            var receiver = receivers[i];
            if (receiver == null || receiver.gameObject.scene != scene)
            {
                continue;
            }

            if (string.Equals(receiver.gameObject.name, signalReceiverName, StringComparison.Ordinal))
            {
                if (foundReceiver == null)
                {
                    foundReceiver = receiver;
                }

                matchCount++;
            }
        }

        if (matchCount > 1)
        {
            Debug.LogWarning(
                $"{nameof(FablerDialogueState)}.{nameof(FindSignalReceiverInCurrentScene)} found {matchCount} " +
                $"{nameof(SignalReceiver)} components on GameObjects named '{signalReceiverName}' in scene '{scene.name}'. " +
                $"Using the first match on '{foundReceiver.gameObject.name}'.",
                foundReceiver);
        }

        return foundReceiver;
    }

    private void ClearChoiceButtons()
    {
        if (dialougeUI != null)
            dialougeUI.ClearChoiceButtons();
    }

    public void HideAll()
    {
        StopTransitionQuery();
        hasChoices = false;
        if (dialougeUI != null)
            dialougeUI.HideAll();
    }
}
