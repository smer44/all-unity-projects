using System;
using UnityEngine;
using UnityEngine.InputSystem;

[CreateAssetMenu(fileName = "Action3DButtonControls", menuName = "ScriptableObjects/Player/Action3DButtonControls")]
public class Action3DButtonControls : AbstractUnitControls
{
    [SerializeField] private ButtonBinding moveLeft = new ButtonBinding(Key.A, MouseButtonBinding.None);
    [SerializeField] private ButtonBinding moveRight = new ButtonBinding(Key.D, MouseButtonBinding.None);
    [SerializeField] private ButtonBinding moveBack = new ButtonBinding(Key.S, MouseButtonBinding.None);
    [SerializeField] private ButtonBinding moveForward = new ButtonBinding(Key.W, MouseButtonBinding.None);
    [SerializeField] private ButtonBinding jump = new ButtonBinding(Key.Space, MouseButtonBinding.None);
    [SerializeField] private ButtonBinding descend = new ButtonBinding(Key.LeftCtrl, MouseButtonBinding.None);
    [SerializeField] private ButtonBinding descendAlt = new ButtonBinding(Key.RightCtrl, MouseButtonBinding.None);
    [SerializeField] private ButtonBinding interact = new ButtonBinding(Key.E, MouseButtonBinding.None);
    [SerializeField] private ButtonBinding cancel = new ButtonBinding(Key.Q, MouseButtonBinding.None);
    [SerializeField] private ButtonBinding punch = new ButtonBinding(Key.None, MouseButtonBinding.Left);
    [SerializeField] private ButtonBinding block = new ButtonBinding(Key.None, MouseButtonBinding.Right);
    [SerializeField] private ButtonBinding evadeModifier = new ButtonBinding(Key.LeftShift, MouseButtonBinding.None);
    [SerializeField] private ButtonBinding evadeModifierAlt = new ButtonBinding(Key.RightShift, MouseButtonBinding.None);
    [SerializeField] private ButtonBinding runToggle = new ButtonBinding(Key.LeftAlt, MouseButtonBinding.None);
    [SerializeField] private ButtonBinding flightToggle = new ButtonBinding(Key.Tab, MouseButtonBinding.None);
    [SerializeField] private ButtonBinding flyingDash = new ButtonBinding(Key.LeftAlt, MouseButtonBinding.None);

    public override Vector2 GetMove2D() => GetPlanarMoveInput().normalized;

    public override Vector3 GetFlyingMove3D()
    {
        Vector2 planarInput = GetPlanarMoveInput();
        float verticalInput = (jump.IsPressed() ? 1f : 0f) - (descend.IsPressed() ? 1f : 0f);
        return new Vector3(planarInput.x, verticalInput, planarInput.y).normalized;
    }

    private Vector2 GetPlanarMoveInput()
    {
        float x = 0f;
        float y = 0f;

        if (moveLeft.IsPressed())
        {
            x -= 1f;
        }

        if (moveRight.IsPressed())
        {
            x += 1f;
        }

        if (moveBack.IsPressed())
        {
            y -= 1f;
        }

        if (moveForward.IsPressed())
        {
            y += 1f;
        }

        return new Vector2(x, y);
    }

    public override Vector3 GetMove3D()
    {
        float x = 0f;
        float y = 0f;
        float z = 0f;

        if (moveLeft.IsPressed())
        {
            x -= 1f;
        }

        if (moveRight.IsPressed())
        {
            x += 1f;
        }

        if (moveBack.IsPressed())
        {
            z -= 1f;
        }

        if (moveForward.IsPressed())
        {
            z += 1f;
        }

        if (jump.IsPressed())
        {
            y += 1f;
        }

        if (IsDescendPressed())
        {
            y -= 1f;
        }

        return new Vector3(x, y, z).normalized;
    }

    public override Vector2 GetMouseMove2D()
    {
        var mouse = Mouse.current;
        return mouse != null ? mouse.delta.ReadValue() : Vector2.zero;
    }

    public override bool IsJumpPressed()
    {
        return jump.IsPressed();
    }

    public override bool IsInteractButtonPressed()
    {
        return interact.IsPressed();
    }

    public override bool IsCancelButtonPressed()
    {
        return cancel.IsPressed();
    }

    public override bool IsPunchButtonPresssed()
    {
        return punch.IsPressed();
    }

    public override bool IsBlockButtonPressed()
    {
        return block.IsPressed();
    }

    public override bool IsAimButtonPressed()
    {
        return block.IsPressed();
    }

    public override bool IsEvadeModifierPressed()
    {
        return evadeModifier.IsPressed() || evadeModifierAlt.IsPressed();
    }

    public override bool WasRunWalkTogglePressed()
    {
        return runToggle.WasPressedThisFrame();
    }

    public override bool WasFlightTogglePressed()
    {
        return flightToggle.WasPressedThisFrame();
    }

    public override bool WasFlyingDashPressed() => flyingDash.WasPressedThisFrame();

    public override bool IsFlyingDashPressed() => flyingDash.IsPressed();

    private bool IsDescendPressed()
    {
        return descend.IsPressed() || descendAlt.IsPressed();
    }
}

[Serializable]
public struct ButtonBinding
{
    [SerializeField] private Key keyboardKey;
    [SerializeField] private MouseButtonBinding mouseButton;

    public ButtonBinding(Key keyboardKey, MouseButtonBinding mouseButton)
    {
        this.keyboardKey = keyboardKey;
        this.mouseButton = mouseButton;
    }

    public bool IsPressed()
    {
        return IsKeyboardPressed() || IsMousePressed();
    }

    public bool WasPressedThisFrame()
    {
        return WasKeyboardPressedThisFrame() || WasMousePressedThisFrame();
    }

    private bool IsKeyboardPressed()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null || keyboardKey == Key.None)
        {
            return false;
        }

        return keyboard[keyboardKey].isPressed;
    }

    private bool WasKeyboardPressedThisFrame()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null || keyboardKey == Key.None)
        {
            return false;
        }

        return keyboard[keyboardKey].wasPressedThisFrame;
    }

    private bool IsMousePressed()
    {
        var mouse = Mouse.current;
        if (mouse == null)
        {
            return false;
        }

        return mouseButton switch
        {
            MouseButtonBinding.Left => mouse.leftButton.isPressed,
            MouseButtonBinding.Right => mouse.rightButton.isPressed,
            MouseButtonBinding.Middle => mouse.middleButton.isPressed,
            MouseButtonBinding.Forward => mouse.forwardButton.isPressed,
            MouseButtonBinding.Back => mouse.backButton.isPressed,
            _ => false
        };
    }

    private bool WasMousePressedThisFrame()
    {
        var mouse = Mouse.current;
        if (mouse == null)
        {
            return false;
        }

        return mouseButton switch
        {
            MouseButtonBinding.Left => mouse.leftButton.wasPressedThisFrame,
            MouseButtonBinding.Right => mouse.rightButton.wasPressedThisFrame,
            MouseButtonBinding.Middle => mouse.middleButton.wasPressedThisFrame,
            MouseButtonBinding.Forward => mouse.forwardButton.wasPressedThisFrame,
            MouseButtonBinding.Back => mouse.backButton.wasPressedThisFrame,
            _ => false
        };
    }
}

public enum MouseButtonBinding
{
    None,
    Left,
    Right,
    Middle,
    Forward,
    Back
}
