public class Emotions : VectorN
{
    public Emotions() : base(3)
    {
    }

    public Emotions(float confidence, float satisfaction, float joy)
        : base(confidence, satisfaction, joy)
    {
    }

    public float GetConfidence()
    {
        return values[0]; // - fear
    }

    public float GetSatisfaction()
    {
        return values[1]; // - anger
    }

    public float GetJoy()
    {
        return values[2]; // - sorrow
    }

    public static Emotions dangerReaction = new Emotions(-0.3f, -0.1f, -0.1f);
    public static Emotions zero => new Emotions();
}
