using UnityEngine;

public abstract class AbstractCameraState
{
    protected readonly CameraController Controller;
    protected bool Smooth;

    protected AbstractCameraState(CameraController controller, bool smooth)
    {
        Controller = controller;
        Smooth = smooth;
    }

    public virtual Transform CameraPivot => Controller.CameraPivotTransform;
    public virtual Transform CameraPosition => Controller.GetDirection();

    public virtual void OnEnter()
    {
    }

    public virtual void OnExit()
    {
    }

    public abstract void Update();
}
