using UnityEngine;
using UnityEngine.InputSystem;

public class TogglerOnKey : MonoBehaviour
{
    [SerializeField] private Key key;
    [SerializeField] private TogglerOfGameObject toggler;

    private void Update()
    {
        if (toggler == null || key == Key.None)
            return;

        if (KeyboardUtils.WasKeyPressedThisFrame(key))
            toggler.ToggleTargetObject();
    }
}
