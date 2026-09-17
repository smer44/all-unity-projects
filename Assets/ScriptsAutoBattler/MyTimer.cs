using System;

[Serializable]
public class MyTimer
{
    [NonSerialized]
    private float currentTime;

    public virtual bool Tick(float delta, float interval)
    {
        if (interval <= 0f)
            return false;

        currentTime += delta;

        if (currentTime < interval)
            return false;

        currentTime -= interval;
        return true;
    }

    public void Reset()
    {
        currentTime = 0f;
    }
}
