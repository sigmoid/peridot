using System.Xml.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Peridot;
using Peridot.Components;
using Peridot.EntityComponentScene.Serialization;

public class RigidbodyComponent : Component
{
    public Vector2 LocalPosition { get; set; }
    public Vector2 Position => Entity.Position + LocalPosition;
    public bool IsStatic { get; set; } = false; // Indicates if the Rigidbody is static or dynamic

    public RigidbodyComponent(bool IsStatic = false)
    {
        this.IsStatic = IsStatic;
    }

    public override void Initialize()
    {
        // Initialization logic if needed
    }

    public override void Update(GameTime gameTime)
    {
        // Update logic if needed
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        // Draw logic if needed
    }
}