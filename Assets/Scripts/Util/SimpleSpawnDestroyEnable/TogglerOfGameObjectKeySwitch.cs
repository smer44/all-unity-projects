using UnityEngine;
using UnityEngine.InputSystem;

public class TogglerOfGameObjectKeySwitch : TogglerOfGameObjectSwitch
{
    [SerializeField] private Key[] keys;
    [SerializeField] private bool useKeyboardInput = true;

    public bool UseKeyboardInput { get => useKeyboardInput; set => useKeyboardInput = value; }

    void Update()
    {
        if (!useKeyboardInput)
            return;

        int keyIndex = KeyboardUtils.GetPressedKeyIndex(keys);

        if (keyIndex >= 0)
            SwitchActive(keyIndex);
    }
}
