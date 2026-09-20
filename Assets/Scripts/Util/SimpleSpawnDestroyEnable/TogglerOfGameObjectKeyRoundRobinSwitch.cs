using UnityEngine;
using UnityEngine.InputSystem;

public class TogglerOfGameObjectKeyRoundRobinSwitch : TogglerOfGameObjectSwitch
{
    [SerializeField] private Key key;

    void Update()
    {
        if (!KeyboardUtils.WasKeyPressedThisFrame(key))
            return;

        int gameObjectCount = GetGameObjectCount();

        if (gameObjectCount == 0)
            return;

        int nextIndex = (GetActiveIndex() + 1) % gameObjectCount;
        SwitchActive(nextIndex);
    }
}
