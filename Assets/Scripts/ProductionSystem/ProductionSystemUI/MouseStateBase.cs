using UnityEngine;

public abstract class MouseStateBase : MonoBehaviour
{
    protected MouseManager mouseManager;

    public virtual void Initialize(MouseManager manager)
    {
        mouseManager = manager;
    }

    public virtual void Enter()
    {
    }

    public virtual void Exit()
    {
    }

    public virtual string DisplayName()
    {
        return "MouseStateBase";
    }
}