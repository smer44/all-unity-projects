using UnityEngine;
using UnityEngine.InputSystem;

public class ToggleCursor : MonoBehaviour
{
    [SerializeField] private Key key;

    private void Update()
    {
        if (!KeyboardUtils.WasKeyPressedThisFrame(key))
            return;

        MouseVisibilityController.Instance.TogleCursor();
    }
}
