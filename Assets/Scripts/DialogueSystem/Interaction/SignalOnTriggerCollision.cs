using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.Playables;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class SignalOnTriggerCollision : MonoBehaviour
{
    [System.Serializable]
    private struct SignalReceiverPair
    {
        public SignalReceiver signalReceiver;
        public SignalAsset signalAsset;
    }

    [SerializeField] private SignalReceiverPair[] signalReceiverPairs;
    [SerializeField] private string triggeringTag = "Player";
    [SerializeField, Min(1)] private int maxSignalsToEmit = 1;

    private int emittedSignalsCount;
    private SignalEmitter[] signalEmitters;

    private void Start()
    {
        RebuildSignalEmitters();
    }

    private void OnDestroy()
    {
        DestroySignalEmitters();
    }

    private void DestroySignalEmitters()
    {
        if (signalEmitters == null)
        {
            return;
        }

        for (int i = 0; i < signalEmitters.Length; i++)
        {
            if (signalEmitters[i] != null)
            {
                Destroy(signalEmitters[i]);
                signalEmitters[i] = null;
            }
        }

        signalEmitters = null;
    }

    private void Reset()
    {
        var triggerCollider = GetComponent<Collider>();
        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
        }
    }

    private void OnValidate()
    {
        maxSignalsToEmit = Mathf.Max(1, maxSignalsToEmit);

        if (signalEmitters == null)
        {
            return;
        }

        for (int i = 0; i < signalEmitters.Length; i++)
        {
            if (signalEmitters[i] != null)
            {
                signalEmitters[i].asset = GetSignalAsset(i);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsMatchingTag(other))
        {
            return;
        }

        if (emittedSignalsCount >= maxSignalsToEmit)
        {
            enabled = false;
            return;
        }

        if (signalReceiverPairs == null || signalReceiverPairs.Length == 0)
        {
            Debug.LogWarning($"{nameof(SignalOnTriggerCollision)} on '{gameObject.name}' has no signal receiver pairs assigned.", this);
            return;
        }

        bool emittedAnySignal = false;
        for (int i = 0; i < signalReceiverPairs.Length; i++)
        {
            emittedAnySignal |= TryEmitSignal(i);
        }

        if (emittedAnySignal)
        {
            emittedSignalsCount++;

            if (emittedSignalsCount >= maxSignalsToEmit)
            {
                enabled = false;
            }
        }
    }

    private bool TryEmitSignal(int index)
    {
        if (signalReceiverPairs == null || index < 0 || index >= signalReceiverPairs.Length)
        {
            return false;
        }

        SignalReceiverPair pair = signalReceiverPairs[index];
        if (pair.signalAsset == null)
        {
            Debug.LogWarning($"{nameof(SignalOnTriggerCollision)} on '{gameObject.name}' has no signal assigned at pair index {index}.", this);
            return false;
        }

        if (pair.signalReceiver == null)
        {
            Debug.LogWarning($"{nameof(SignalOnTriggerCollision)} on '{gameObject.name}' has no signalReceiver assigned at pair index {index}.", this);
            return false;
        }

        SignalEmitter signalEmitter = GetSignalEmitter(index);
        if (signalEmitter == null)
        {
            Debug.LogWarning($"{nameof(SignalOnTriggerCollision)} on '{gameObject.name}' has no cached signalEmitter at pair index {index}.", this);
            return false;
        }

        signalEmitter.asset = pair.signalAsset;
        pair.signalReceiver.OnNotify(Playable.Null, signalEmitter, null);
        Debug.Log($"SignalOnTriggerCollision : emitted signal {pair.signalAsset}");
        return true;
    }

    private SignalEmitter GetSignalEmitter(int index)
    {
        if (signalReceiverPairs == null || index < 0 || index >= signalReceiverPairs.Length)
        {
            return null;
        }

        if (signalEmitters == null || signalEmitters.Length != signalReceiverPairs.Length)
        {
            RebuildSignalEmitters();
        }

        if (signalEmitters[index] == null)
        {
            signalEmitters[index] = ScriptableObject.CreateInstance<SignalEmitter>();
        }

        signalEmitters[index].asset = signalReceiverPairs[index].signalAsset;
        return signalEmitters[index];
    }

    private SignalAsset GetSignalAsset(int index)
    {
        if (signalReceiverPairs == null || index < 0 || index >= signalReceiverPairs.Length)
        {
            return null;
        }

        return signalReceiverPairs[index].signalAsset;
    }

    private void RebuildSignalEmitters()
    {
        DestroySignalEmitters();

        if (signalReceiverPairs == null)
        {
            return;
        }

        signalEmitters = new SignalEmitter[signalReceiverPairs.Length];
        for (int i = 0; i < signalReceiverPairs.Length; i++)
        {
            signalEmitters[i] = ScriptableObject.CreateInstance<SignalEmitter>();
            signalEmitters[i].asset = signalReceiverPairs[i].signalAsset;
        }
    }

    private bool IsMatchingTag(Collider other)
    {
        if (other == null || string.IsNullOrWhiteSpace(triggeringTag))
        {
            return false;
        }

        if (other.CompareTag(triggeringTag))
        {
            return true;
        }

        return other.attachedRigidbody != null && other.attachedRigidbody.CompareTag(triggeringTag);
    }
}
