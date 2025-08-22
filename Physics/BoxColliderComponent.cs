using System;
using System.Xml.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Peridot;
using Peridot.Components;

public class BoxColliderComponent : Component
{
    [Peridot.EntityComponentScene.Serialization.ComponentProperty]
    public Vector2 Size { get; set; } = new Vector2(32, 32);

    [Peridot.EntityComponentScene.Serialization.ComponentProperty]
    public Vector2 Offset { get; set; } = Vector2.Zero;

    /// <summary>
    /// The collision layer this collider belongs to
    /// </summary>
    [Peridot.EntityComponentScene.Serialization.ComponentProperty]
    public CollisionLayer Layer { get; set; } = CollisionLayer.Default;

    /// <summary>
    /// Mask defining which layers this collider can collide with
    /// </summary>
    [Peridot.EntityComponentScene.Serialization.ComponentProperty]
    public CollisionLayer CollisionMask { get; set; } = CollisionLayer.All;

    /// <summary>
    /// Whether this collider should participate in physics collision resolution
    /// </summary>
    [Peridot.EntityComponentScene.Serialization.ComponentProperty]
    public bool IsSolid { get; set; } = true;

    /// <summary>
    /// Whether this collider is a trigger (detects collisions but doesn't block movement)
    /// </summary>
    [Peridot.EntityComponentScene.Serialization.ComponentProperty]
    public bool IsTrigger { get; set; } = false;

    public BoxColliderComponent()
    {
        
        // Default constructor for serialization or other purposes
    }

    public BoxColliderComponent(Vector2 size, Vector2 offset = default, CollisionLayer layer = CollisionLayer.Default, CollisionLayer collisionMask = CollisionLayer.All)
    {
        Size = size;
        Offset = offset;
        Layer = layer;
        CollisionMask = collisionMask;
    }

    /// <summary>
    /// Check if this collider can collide with another collider based on layers
    /// </summary>
    public bool CanCollideWith(BoxColliderComponent other)
    {
        if (other == null) return false;
        
        // Check if our collision mask includes the other's layer
        // AND if the other's collision mask includes our layer
        return CollisionMask.Contains(other.Layer) && other.CollisionMask.Contains(Layer);
    }

    /// <summary>
    /// Check if this collider can collide with a specific layer
    /// </summary>
    public bool CanCollideWith(CollisionLayer layer)
    {
        return CollisionMask.Contains(layer);
    }    public override void Draw(SpriteBatch spriteBatch)
    {

    }

    public override void Initialize()
    {
    }

    public override void Update(GameTime gameTime)
    {
    }

    public AABB GetBoundingBox()
    {
        var position = Entity.Position;

        return new AABB(new Vector2(position.X + Offset.X, position.Y + Offset.Y), new Vector2(position.X + Size.X + Offset.X, position.Y + Size.Y + Offset.Y));
    }
}