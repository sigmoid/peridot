using System.Numerics;

public static class Random
{
    private static System.Random _random = new System.Random();

    public static int Next(int minValue, int maxValue)
    {
        return _random.Next(minValue, maxValue);
    }

    public static float NextFloat(float minValue, float maxValue)
    {
        return (float)_random.NextDouble() * (maxValue - minValue) + minValue;
    }

    public static float Range(float minValue, float maxValue)
    {
        return NextFloat(minValue, maxValue);
    }

    public static void SetSeed(int seed)
    {
        _random = new System.Random(seed);
    }

    public static Vector2 Vector2()
    {
        return new Vector2(
            (float)_random.NextDouble(),
            (float)_random.NextDouble()
        );
    }
}