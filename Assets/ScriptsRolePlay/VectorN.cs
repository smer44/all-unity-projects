using System;

[Serializable]
public class VectorN
{
    protected float[] values;

    public int Length => values.Length;

    public VectorN(int length)
    {
        values = new float[length];
    }

    public VectorN(params float[] values)
    {
        this.values = values ?? new float[0];
    }

    public float GetElement(int index)
    {
        return values[index];
    }

    public static VectorN operator +(VectorN left, VectorN right)
    {
        int length = Math.Min(left.Length, right.Length);
        VectorN result = new VectorN(length);

        for (int i = 0; i < length; i++)
        {
            result.values[i] = left.values[i] + right.values[i];
        }

        return result;
    }

    public static VectorN operator *(VectorN vector, float multiplier)
    {
        VectorN result = new VectorN(vector.Length);

        for (int i = 0; i < vector.Length; i++)
        {
            result.values[i] = vector.values[i] * multiplier;
        }

        return result;
    }

    public static VectorN operator *(float multiplier, VectorN vector)
    {
        return vector * multiplier;
    }

    public static VectorN operator *(VectorN left, VectorN right)
    {
        int length = Math.Min(left.Length, right.Length);
        VectorN result = new VectorN(length);

        for (int i = 0; i < length; i++)
        {
            result.values[i] = left.values[i] * right.values[i];
        }

        return result;
    }

    public void AddInPlace(VectorN other)
    {
        int length = Math.Min(Length, other.Length);

        for (int i = 0; i < length; i++)
        {
            values[i] += other.values[i];
        }
    }

    public void AddMultInPlace(VectorN other, float multiplier)
    {
        int length = Math.Min(Length, other.Length);

        for (int i = 0; i < length; i++)
        {
            values[i] += other.values[i] * multiplier;
        }
    }

    public void MultiplyInPlace(float multiplier)
    {
        for (int i = 0; i < Length; i++)
        {
            values[i] *= multiplier;
        }
    }

    public void MultiplyInPlace(VectorN other)
    {
        int length = Math.Min(Length, other.Length);

        for (int i = 0; i < length; i++)
        {
            values[i] *= other.values[i];
        }
    }
}
