using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using TMPro;

public class ExpectKeyPressedUI : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private Key keycode = Key.None;

    [Header("UI")]
    [SerializeField] private RectTransform visuals;
    [SerializeField] private TMP_Text buttonText;

    [Header("Signals")]
    [SerializeField] private SignalAsset pressedSignal;
    [SerializeField] private SignalAsset notPressedSignal;
    [SerializeField] private SignalReceiver signalReceiver;

    private SignalEmitter signalEmitter;
    private bool isExpecting = false;
    private bool wasPressed = false;

    private void Start()
    {
        SetVisualsVisible(false);
    }

    public void StartExpect()
    {
        isExpecting = true;
        wasPressed = false;
        UpdateButtonText();
        SetVisualsVisible(true);
    }

    public void StopExpect()
    {
        SetVisualsVisible(false);

        if (!isExpecting)
        {
            return;
        }

        isExpecting = false;

        if (!wasPressed)
        {
            EmitSignal(notPressedSignal);
        }
    }

    private void Update()
    {
        if (!isExpecting || wasPressed || !IsKeyPressed())
        {
            return;
        }

        wasPressed = true;
        EmitSignal(pressedSignal);
    }

    private void OnDestroy()
    {
        if (signalEmitter != null)
        {
            Destroy(signalEmitter);
            signalEmitter = null;
        }
    }

    private bool IsKeyPressed()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null || keycode == Key.None)
        {
            return false;
        }

        return keyboard[keycode].isPressed;
    }

    private void EmitSignal(SignalAsset signal)
    {
        if (signal == null)
        {
            Debug.LogWarning($"{nameof(ExpectKeyPressed)} on '{gameObject.name}' has no signal assigned.", this);
            return;
        }

        if (signalReceiver == null)
        {
            Debug.LogWarning($"{nameof(ExpectKeyPressed)} on '{gameObject.name}' has no signal receiver assigned.", this);
            return;
        }

        EnsureSignalEmitter();
        signalEmitter.asset = signal;
        signalReceiver.OnNotify(Playable.Null, signalEmitter, null);
    }

    private void EnsureSignalEmitter()
    {
        if (signalEmitter != null)
        {
            return;
        }

        signalEmitter = ScriptableObject.CreateInstance<SignalEmitter>();
    }

    private void UpdateButtonText()
    {
        if (buttonText == null)
        {
            return;
        }

        buttonText.text = keycode.ToString();
    }

    private void SetVisualsVisible(bool visible)
    {
        if (visuals == null)
        {
            return;
        }

        visuals.gameObject.SetActive(visible);
    }
}
