using System;
using System.Xml.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Peridot.Components;

public abstract class Component
{
    public Guid Id { get; } = Guid.NewGuid();
    public Entity Entity { get; set; }
    public virtual void Initialize() { }
    public virtual void Update(GameTime gameTime) { }
    public virtual void Draw(SpriteBatch spriteBatch) { }
    public virtual string SerializeData() => string.Empty;
    public virtual void OnDeserializeData(string data) { }

    public virtual void OnCollision(Entity other) { }

    public T RequireComponent<T>() where T : Component
    {
        var component = Entity.GetComponent<T>();
        if (component == null)
        {
            throw new InvalidOperationException($"Component of type {typeof(T).Name} is required but not found on entity {Entity.Name}");
        }
        return component;
    }
    public virtual XElement Serialize()
    {
        return ComponentSerializer.AutoSerialize(this);
    }

    public virtual void Deserialize(XElement element)
    {
        if (element == null) throw new ArgumentNullException(nameof(element));

    }
}