using UnityEngine;

public abstract class TickAction : ScriptableObject
{
    [SerializeField] private MyTimer timer = new MyTimer();

    public abstract bool Tick(float delta, TickActionContext context);

    protected bool TickTimer(float delta, float interval)
    {
        if (timer == null)
            timer = new MyTimer();

        return timer.Tick(delta, interval);
    }

    protected void ResetTimer()
    {
        timer?.Reset();
    }
}
