using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class DailyActionsController : MonoBehaviour
{
    private const string MaxEnergyPointsMemoryKey = "maxEnergyPoints";
    private const string CurrentEnergyPointsMemoryKey = "currentEnergyPoints";


    [Header("Actions")]
    [SerializeField] private AbstractActionProvider[] actionProviders;

    [Header("Action Buttons")]
    [SerializeField] private Transform buttonRoot;
    [SerializeField] private Button actionButtonPrefab;


    private readonly Dictionary<string, AbstractTurnAction> dayActions = new();
    private bool hasWarnedMissingMemoryBehaviour;
    private bool hasWarnedMissingStoryMemory;

    public int MaxEnergyPoints => GetEnergyValue(MaxEnergyPointsMemoryKey);
    public int CurrentEnergyPoints => GetEnergyValue(CurrentEnergyPointsMemoryKey);


    private void Start()
    {
        UpdateDayActions();
        //MouseVisibilityController.Instance.ActivateCursor();
    }


    public void UpdateDayActions()
    {
        dayActions.Clear();
        ClearActionButtons();

        StoryMemory storyMemory = GetCurrentStoryMemory();
        if (storyMemory == null || actionProviders == null)
            return;

        foreach (AbstractActionProvider provider in actionProviders)
        {
            if (provider == null)
            {
                Debug.LogWarning($"{nameof(DailyActionsController)} has an empty action provider slot.", this);
                continue;
            }

            AbstractTurnAction[] actions = provider.GetActionsForDate(storyMemory);
            if (actions == null)
                continue;

            foreach (AbstractTurnAction action in actions)
            {
                if (action == null || string.IsNullOrWhiteSpace(action.KeyName()))
                    continue;

                dayActions[action.KeyName()] = action;
            }
        }

        PopulateActionButtons();
    }


    public void PerformAction(string actionId)
    {
        if (!dayActions.TryGetValue(actionId, out AbstractTurnAction action))
        {
            Debug.Log($"TurnTimeManager.PerformAction: Action is not currently available: {actionId}");
            return;
        }

        PerformAction(action);
    }

    public void ToNextDay()
    {
        StoryMemory storyMemory = GetCurrentStoryMemory();
        if (storyMemory == null)
            return;

        storyMemory.AdvanceToNextDay();
        SetCurrentEnergyPoints(MaxEnergyPoints);
        UpdateDayActions();
    }

    public void RestorePoints()
    {
        SetCurrentEnergyPoints(MaxEnergyPoints);
        PopulateActionButtons();
    }


    public bool CanPerformAction(string actionId)
    {
        return dayActions.TryGetValue(actionId, out AbstractTurnAction action)
            && CanAfford(action.EnergyCost());
    }


    private bool CanAfford(int energyCost)
    {
        return CurrentEnergyPoints >= energyCost;
    }

    private int GetEnergyValue(string memoryKey)
    {
        if (MemoryBehaviour.Instance == null || MemoryBehaviour.Instance.StoryMemory == null)
            return 0;

        return MemoryBehaviour.TryGet(memoryKey, out int value) ? value : 0;
    }

    private void SetCurrentEnergyPoints(int value)
    {
        if (MemoryBehaviour.Instance != null && MemoryBehaviour.Instance.StoryMemory != null)
        {
            MemoryBehaviour.Set(CurrentEnergyPointsMemoryKey, value);
        }
    }

    private void PerformAction(AbstractTurnAction action)
    {
        if (action == null)
            return;

        int energyCost = action.EnergyCost();
        if (!CanAfford(energyCost))
            return;

        SetCurrentEnergyPoints(CurrentEnergyPoints - energyCost);
        PopulateActionButtons();
        action.Execute();
    }

    private StoryMemory GetCurrentStoryMemory()
    {
        if (MemoryBehaviour.Instance == null)
        {
            if (!hasWarnedMissingMemoryBehaviour)
            {
                Debug.LogWarning(
                    $"{nameof(DailyActionsController)} cannot read story memory because {nameof(MemoryBehaviour)}.Instance is not initialized.",
                    this);
                hasWarnedMissingMemoryBehaviour = true;
            }

            return null;
        }

        if (MemoryBehaviour.Instance.StoryMemory == null)
        {
            if (!hasWarnedMissingStoryMemory)
            {
                Debug.LogWarning(
                    $"{nameof(DailyActionsController)} cannot read story memory because {nameof(MemoryBehaviour)} has no {nameof(StoryMemory)} assigned.",
                    this);
                hasWarnedMissingStoryMemory = true;
            }

            return null;
        }

        return MemoryBehaviour.Instance.StoryMemory;
    }

    private void ClearActionButtons()
    {
        if (buttonRoot == null)
            return;

        for (int i = buttonRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(buttonRoot.GetChild(i).gameObject);
        }
    }

    private void PopulateActionButtons()
    {
        if (buttonRoot == null || actionButtonPrefab == null)
            return;

        ClearActionButtons();

        foreach (AbstractTurnAction action in dayActions.Values)
        {
            Button button = Instantiate(actionButtonPrefab, buttonRoot);
            SetButtonLabel(button, action.KeyName());

            AbstractTurnAction actionForButton = action;
            button.onClick.RemoveAllListeners();
            bool canAffordAction = CanAfford(actionForButton.EnergyCost());
            button.interactable = canAffordAction;
            if (canAffordAction)
            {
                button.onClick.AddListener(() => PerformAction(actionForButton));
            }
        }
    }

    private static void SetButtonLabel(Button button, string label)
    {
        TMP_Text tmpText = button.GetComponentInChildren<TMP_Text>();
        if (tmpText != null)
        {
            tmpText.text = label;
            return;
        }

        Text uiText = button.GetComponentInChildren<Text>();
        if (uiText != null)
        {
            uiText.text = label;
        }
    }

}
