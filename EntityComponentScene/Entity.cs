using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Peridot.Components;
using System.Xml.Linq;

namespace Peridot;

public class Entity
{
    public string Name { get; set; } = "UnnamedEntity";
    private List<Component> _components;
    private List<Entity> _children = new List<Entity>();
    private Vector2 _localPosition = Vector2.Zero;

    public Entity Parent { get; private set; }

    public Vector2 LocalPosition
    {
        get => _localPosition;
        set => _localPosition = value;
    }

    public Vector2 Position
    {
        get => Parent != null ? Parent.Position + _localPosition : _localPosition;
        set => _localPosition = Parent != null ? value - Parent.Position : value;
    }

    public Entity()
    {
        _components = new List<Component>();
    }

    public XElement Serialize()
    {
        var element = new XElement("Entity",
            new XAttribute("Name", Name));

        // Add Position as its own element with X and Y child elements
        var positionElement = new XElement("Position",
            new XElement("X", LocalPosition.X),
            new XElement("Y", LocalPosition.Y));
        element.Add(positionElement);

        foreach (var component in _components)
        {
            // Use ComponentSerializer.AutoSerialize for all components
            element.Add(component.Serialize());
        }

        // Add children wrapped in a Children element if there are any
        if (_children.Count > 0)
        {
            var childrenElement = new XElement("Children");
            foreach (var child in _children)
            {
                childrenElement.Add(child.Serialize());
            }
            element.Add(childrenElement);
        }

        return element;
    }

    public static Entity FromFile(ContentManager content, string assetName)
    {
        return Peridot.EntityComponentScene.Serialization.EntityFactory.FromFile(content, assetName);
    }

    public static Entity FromContentFile(ContentManager content, string assetPath)
    {
        return Peridot.EntityComponentScene.Serialization.EntityFactory.FromContentFile(content, assetPath);
    }

    public static Entity FromXmlFile(string xmlFilePath)
    {
        return Peridot.EntityComponentScene.Serialization.EntityFactory.FromXmlFile(xmlFilePath);
    }

    public static Entity FromXElement(System.Xml.Linq.XElement element, string baseDirectory = null)
    {
        return Peridot.EntityComponentScene.Serialization.EntityFactory.FromXElement(element, baseDirectory);
    }

    public void AddComponent(Component component)
    {
        component.Entity = this;
        _components.Add(component);
    }

    public void RemoveComponent(Component component)
    {
        _components.Remove(component);
    }

    public void AddChild(Entity child)
    {
        if (child.Parent != null)
        {
            child.Parent.RemoveChild(child);
        }

        child.Parent = this;
        _children.Add(child);
    }

    public void RemoveChild(Entity child)
    {
        if (_children.Remove(child))
        {
            child.Parent = null;
        }
    }

    public List<Entity> GetChildren()
    {
        return new List<Entity>(_children);
    }

    public IReadOnlyList<Entity> Children => _children.AsReadOnly();

    public Entity GetChild(string name)
    {
        return _children.Find(c => c.Name == name);
    }

    /// <summary>
    /// Get the world position of this entity
    /// </summary>
    public Vector2 GetWorldPosition()
    {
        return Position;
    }

    /// <summary>
    /// Set the world position of this entity
    /// </summary>
    public void SetWorldPosition(Vector2 worldPosition)
    {
        Position = worldPosition;
    }

    /// <summary>
    /// Move this entity by the specified offset (affects local position)
    /// </summary>
    public void Move(Vector2 offset)
    {
        LocalPosition += offset;
    }

    public T GetComponent<T>() where T : Component
    {
        foreach (var component in _components)
        {
            if (component is T)
            {
                return component as T;
            }
        }
        return null;
    }

    public T GetComponentInChildren<T>() where T : Component
    {
        foreach (var child in _children)
        {
            var component = child.GetComponent<T>();
            if (component != null)
            {
                return component;
            }
        }
        return null;
    }

    public void Initialize()
    {
        foreach (var component in _components)
        {
            component.Initialize();
        }

        foreach (var child in _children)
        {
            child.Initialize();
        }
    }

    public void Update(GameTime gameTime)
    {
        foreach (var component in _components)
        {
            component.Update(gameTime);
        }

        foreach (var child in _children)
        {
            child.Update(gameTime);
        }
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        foreach (var component in _components)
        {
            component.Draw(spriteBatch);
        }

        foreach (var child in _children)
        {
            child.Draw(spriteBatch);
        }
    }

    /// <summary>
    /// Cleanup the entity and release its name back to the name provider
    /// </summary>
    public void Cleanup()
    {
        // Release the name back to the name provider
        Peridot.EntityComponentScene.Serialization.EntityNameProvider.ReleaseName(Name);

        // Cleanup all children
        foreach (var child in _children)
        {
            child.Cleanup();
        }

        // Clear components and children
        _components.Clear();
        _children.Clear();
    }

    public T RequireComponent<T>(Peridot.Entity entity) where T : Component, new()
    {
        if (entity == null)
            throw new System.ArgumentNullException(nameof(entity));

        var component = entity.GetComponent<T>();
        if (component == null)
        {
            throw new System.InvalidOperationException($"Entity does not have a component of type {typeof(T).Name}");
        }
        return component;
    }

    public void OnCollision(Entity other)
    {
        foreach (var component in _components)
        {
            component.OnCollision(other);
        }
    }
}