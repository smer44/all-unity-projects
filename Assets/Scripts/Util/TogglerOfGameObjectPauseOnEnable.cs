using UnityEngine;

public class TogglerOfGameObjectPauseOnEnable : TogglerOfGameObject
{
    public override void EnableTargetObject()
    {
        base.EnableTargetObject();
        Time.timeScale = 0f;
    }

    public override void DisableTargetObject()
    {
        base.DisableTargetObject();
        Time.timeScale = 1f;
    }
}
