using System.Reflection.Metadata;

public static class Math
{
    public static float PI = 3.14159265358979323846f;

    public static float Cos(float angle)
    {
        return (float)System.Math.Cos(angle);
    }

    public static float Sin(float angle)
    {
        return (float)System.Math.Sin(angle);
    }

    public static float Max(float a, float b)
    {
        return a > b ? a : b;
    }

    public static float Min(float a, float b)
    {
        return a < b ? a : b;
    }

    public static int Max(int a, int b)
    {
        return a > b ? a : b;
    }

    public static int Min(int a, int b)
    {
        return a < b ? a : b;
    }

    public static int RoundToInt(float value)
    {
        return (int)System.Math.Round(value);
    }

    public static int FloorToInt(float value)
    {
        return (int)System.Math.Floor(value);
    }

    public static float Lerp(float start, float end, float amount)
    {
        return start + (end - start) * amount;
    }

    public static float Sqrt(float value)
    {
        return (float)System.Math.Sqrt(value);
    }

    public static float Clamp(float value, float min, float max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }
}