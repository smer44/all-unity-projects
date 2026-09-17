using System.Collections;
using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.Playables;


public class SignalOnEnterInteractible : AbstractInteractable
{
    [System.Serializable]
    private struct SignalReceiverPair
    {
        public SignalAsset signal;
        public SignalReceiver receiver;
    }

    [SerializeField] private int layerOnFocus = 8;
    [SerializeField] private SignalReceiverPair[] signalReceiverPairs;

    private Coroutine exitRoutine;
    private bool isCompletingInteraction;
    private PlayerController player;

    private SignalEmitter signalEmitter;

    private void Awake()
    {
        CacheDefaultLayers();
    }

    private void Start()
    {
        EnsureSignalEmitter();
    }

    public override void OnEnter(PlayerController player)
    {
        this.player = player;
        StopActiveRoutine();
        exitRoutine = StartCoroutine(EmitSignalAndExit());
    }

    public override void OnExit()
    {
        if (!isCompletingInteraction)
        {
            StopActiveRoutine();
        }

        isCompletingInteraction = false;
        player = null;
    }

    public override void KillInteraction()
    {
        var currentPlayer = player;

        StopActiveRoutine();
        isCompletingInteraction = false;
        player = null;

        if (currentPlayer != null)
        {
            currentPlayer.SetState(currentPlayer.IdleState);
        }

        Destroy(this);
    }

    public override bool AllowsBreak()
    {
        return false;
    }

    public override void OnFocusEnter()
    {
        ApplyLayerToHierarchy(layerOnFocus);
    }

    public override void OnFocusExit()
    {
        RestoreDefaultLayers();
    }

    private IEnumerator EmitSignalAndExit()
    {
        if (signalReceiverPairs == null || signalReceiverPairs.Length == 0)
        {
            Debug.LogWarning(
                $"{nameof(SignalOnEnterInteractible)} on '{gameObject.name}' has no signal receiver pairs assigned.",
                this);
        }
        else
        {
            foreach (var signalReceiverPair in signalReceiverPairs)
            {
                EmitSignal(signalReceiverPair.signal, signalReceiverPair.receiver);
            }
        }

        yield return null;

        if (player != null)
        {
            var currentPlayer = player;
            isCompletingInteraction = true;
            currentPlayer.SetState(currentPlayer.IdleState);
        }

        exitRoutine = null;
        isCompletingInteraction = false;
    }

    private void EmitSignal(SignalAsset signal, SignalReceiver receiver)
    {
        Debug.Log(
            $"{nameof(SignalOnEnterInteractible)} on '{gameObject.name}' is sending signal '{(signal != null ? signal.name : "null")}' " +
            $"to target '{(receiver != null ? receiver.gameObject.name : "null")}'.",
            this);

        if (signal == null)
        {
            Debug.LogWarning(
                $"{nameof(SignalOnEnterInteractible)} on '{gameObject.name}' has no signal assigned.",
                this);
            return;
        }

        if (receiver == null)
        {
            Debug.LogWarning(
                $"{nameof(SignalOnEnterInteractible)} on '{gameObject.name}' has no signal receiver assigned.",
                this);
            return;
        }

        EnsureSignalEmitter();
        signalEmitter.asset = signal;
        receiver.OnNotify(Playable.Null, signalEmitter, null);
    }

    private void EnsureSignalEmitter()
    {
        if (signalEmitter != null)
        {
            return;
        }

        signalEmitter = ScriptableObject.CreateInstance<SignalEmitter>();
    }

    private void StopActiveRoutine()
    {
        if (exitRoutine == null)
        {
            return;
        }

        StopCoroutine(exitRoutine);
        exitRoutine = null;
    }
}
