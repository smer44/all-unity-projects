using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public static class KeyboardUtils
{
    public static int GetPressedKeyIndex(Key[] keys)
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null || keys == null || keys.Length == 0)
            return -1;

        for (int i = 0; i < keys.Length; i++)
        {
            if (WasKeyPressedThisFrame(keyboard, keys[i]))
                return i;
        }

        return -1;
    }

    public static bool WasKeyPressedThisFrame(Key key)
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
            return false;

        return WasKeyPressedThisFrame(keyboard, key);
    }

    private static bool WasKeyPressedThisFrame(Keyboard keyboard, Key key)
    {
        KeyControl keyControl = keyboard[key];
        return keyControl != null && keyControl.wasPressedThisFrame;
    }
}
