using UnityEngine;
using UnityEngine.InputSystem;

[UnityEngine.DefaultExecutionOrder(100)]
public class SpawnOnMouseClickTimerLoop : SpawnTimerLoop
{
    [SerializeField] private bool useUnitControls;
    private PlayerController owner;

    private void Awake()
    {
        owner = GetComponentInParent<PlayerController>(true);
    }

    private void Update()
    {
        if (IsFireRequested())
        {   
            StartOrContinueSpawn();
        }
        else
        {
            StopSpawn();
        }
    }

    private bool IsFireRequested()
    {
        if (useUnitControls)
            return owner != null && owner.IsGunSelected() && owner.IsPunchButtonPresssed();

        Mouse mouse = Mouse.current;
        return mouse != null && mouse.leftButton.isPressed;
    }
}
