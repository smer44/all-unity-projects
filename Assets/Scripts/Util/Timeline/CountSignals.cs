using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Timeline;

public class CountSignals : MonoBehaviour
{
    public const string SignalEventHookName = "CountSignals.Signal";

    [Header("Incoming")]
    [SerializeField] private SignalAsset signalToCount;
    [SerializeField] private GameObject incomingTarget;

    [Header("Outgoing")]
    [SerializeField] private SignalAsset signalToEmit;
    [SerializeField] private GameObject outgoingTarget;

    [Header("Count")]
    [SerializeField, Min(1)] private int requiredCount = 1;

    [NonSerialized, HideInInspector] private int currentCount;
    [NonSerialized, HideInInspector] private bool isCompleted;

    private Action<EmptyEventArgs> incomingHandler;
    private EventHook incomingHook;
    private bool isRegistered;

    public static EventHook BuildHook(SignalAsset signal, GameObject target = null)
    {
        return new EventHook(SignalEventHookName, target, signal);
    }

    public static void EmitSignal(SignalAsset signal, GameObject target = null)
    {
        if (signal == null)
        {
            Debug.LogWarning($"{nameof(CountSignals)}.{nameof(EmitSignal)} skipped because signal is null.");
            return;
        }

        EventBus.Trigger(BuildHook(signal, target));
    }

    private void Awake()
    {
        requiredCount = Mathf.Max(1, requiredCount);
        incomingHandler = HandleIncomingSignal;
    }

    private void OnValidate()
    {
        requiredCount = Mathf.Max(1, requiredCount);
    }

    private void OnEnable()
    {
        if (isCompleted)
        {
            enabled = false;
            return;
        }

        if (signalToCount == null)
        {
            Debug.LogWarning($"{nameof(CountSignals)} on '{gameObject.name}' has no incoming signal assigned.", this);
            return;
        }

        incomingHook = BuildHook(signalToCount, incomingTarget);
        EventBus.Register(incomingHook, incomingHandler);
        isRegistered = true;
    }

    private void OnDisable()
    {
        Unregister();
    }

    private void HandleIncomingSignal(EmptyEventArgs _)
    {
        if (isCompleted)
        {
            return;
        }

        currentCount++;

        if (currentCount < requiredCount)
        {
            return;
        }

        Complete();
    }

    private void Complete()
    {
        if (isCompleted)
        {
            return;
        }

        isCompleted = true;
        Unregister();

        if (signalToEmit != null)
        {
            EmitSignal(signalToEmit, outgoingTarget);
        }
        else
        {
            Debug.LogWarning(
                $"{nameof(CountSignals)} on '{gameObject.name}' reached {requiredCount} counted signals but has no outgoing signal assigned.",
                this);
        }

        enabled = false;
    }

    private void Unregister()
    {
        if (!isRegistered || incomingHandler == null)
        {
            return;
        }

        EventBus.Unregister(incomingHook, incomingHandler);
        isRegistered = false;
    }
}
