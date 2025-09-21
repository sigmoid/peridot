using Genbox.VelcroPhysics.Dynamics;
using Microsoft.Xna.Framework;

namespace peridot.Physics;

public class PhysicsSystem
{
    private readonly World _world;
    private float _accumulator;
    private const float StepTime = 1f / 60f;   // 60 Hz

    public PhysicsSystem(Vector2 gravity)
    {
        _world = new World(gravity);
        Genbox.VelcroPhysics.Settings.MaxPolygonVertices = 64;
    }

    public World World => _world;

    public void Update(float deltaSeconds)
    {
        _accumulator += deltaSeconds;
        if (_accumulator > 0.25f) _accumulator = 0.25f;

        while (_accumulator >= StepTime)
        {
            _world.Step(StepTime);
            _accumulator -= StepTime;
        }
    }

    public static Vector2 ToSimUnits(Vector2 displayUnits)
	{
		return displayUnits / 100f; // Assuming 100 pixels = 1 meter
	}

	public static Vector2 ToDisplayUnits(Vector2 simUnits)
	{
		return simUnits * 100f; // Assuming 100 pixels = 1 meter
	}
}