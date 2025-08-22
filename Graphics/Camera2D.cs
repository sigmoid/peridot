using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public class Camera2D
{
    private Viewport _viewport;
    public Vector2 Position { get; set; } = Vector2.Zero;
    public float Rotation { get; set; } = 0f;
    public float Zoom { get; set; } = 1f;

    public Camera2D(Viewport viewport)
    {
        _viewport = viewport;
    }

    public Matrix GetViewMatrix()
    {
        return
            Matrix.CreateTranslation(new Vector3(-Position, 0f)) *
            Matrix.CreateRotationZ(Rotation) *
            Matrix.CreateScale(Zoom, Zoom, 1f) *
            Matrix.CreateTranslation(new Vector3(_viewport.Width * 0.5f, _viewport.Height * 0.5f, 0f));
    }

    public void Move(Vector2 amount)
    {
        Position += amount;
    }

    public void CenterOn(Vector2 target)
    {
        Position = target;
    }

    public Vector4 GetWorldSpaceBounds()
    {
        Vector2 topLeft = Position - new Vector2(_viewport.Width / 2f, _viewport.Height / 2f) / Zoom;
        Vector2 bottomRight = Position + new Vector2(_viewport.Width / 2f, _viewport.Height / 2f) / Zoom;
        return new Vector4(topLeft.X, topLeft.Y, bottomRight.X, bottomRight.Y);
    }
}