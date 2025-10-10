namespace Peridot.UI;

using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public abstract class LayoutGroup : UIElement
{
    protected Rectangle _bounds;
    protected int _spacing;
    protected List<UIElement> _children;
    protected Color _backgroundColor;
    protected bool _drawBackground;

    public LayoutGroup(Rectangle bounds, int spacing, Color? backgroundColor = null)
    {
        _bounds = bounds;
        _spacing = spacing;
        _children = new List<UIElement>();
        _backgroundColor = backgroundColor ?? Color.Transparent;
        _drawBackground = backgroundColor.HasValue;
    }

    public virtual void AddChild(UIElement child)
    {
        _children.Add(child);
        child.SetParent(this);
        UpdateChildPositions();
    }

    public virtual void RemoveChild(UIElement child)
    {
        _children.Remove(child);
        UpdateChildPositions();
    }

    public virtual void ClearChildren()
    {
        _children.Clear();
    }

    public IReadOnlyList<UIElement> Children => _children;

    protected abstract void UpdateChildPositions();

    public override void Update(float deltaTime)
    {
        // Create a defensive copy to prevent concurrent modification exceptions
        var childrenCopy = new List<UIElement>(_children);
        foreach (var child in childrenCopy)
        {
            child.Update(deltaTime);
        }
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        if (_drawBackground)
        {
            // Create a 1x1 white pixel texture for drawing backgrounds
            var pixel = new Texture2D(spriteBatch.GraphicsDevice, 1, 1);
            pixel.SetData(new[] { Color.White });
            spriteBatch.Draw(pixel, _bounds, null, _backgroundColor, 0, Vector2.Zero, SpriteEffects.None, GetActualOrder());
            pixel.Dispose();
        }

        // Create a defensive copy to prevent concurrent modification exceptions
        var childrenCopy = new List<UIElement>(_children);
        foreach (var child in childrenCopy)
        {
            if (child.IsVisible())
            {
                child.Draw(spriteBatch);
            }
        }
    }

    public virtual void OnClick()
    {
        // Layout groups don't typically handle clicks directly,
        // but you can override this if needed
    }

    public override Rectangle GetBoundingBox()
    {
        return _bounds;
    }

    public override void SetBounds(Rectangle bounds)
    {
        _bounds = bounds;
        UpdateChildPositions();
    }

    public void SetSpacing(int spacing)
    {
        _spacing = spacing;
        UpdateChildPositions();
    }

    public void SetBackgroundColor(Color color)
    {
        _backgroundColor = color;
        _drawBackground = true;
    }

    public void HideBackground()
    {
        _drawBackground = false;
    }

    /// <summary>
    /// Recursively searches for a UI element with the specified name.
    /// Returns the first element found, or null if no element with that name exists.
    /// </summary>
    /// <param name="name">The name to search for</param>
    /// <returns>The first UIElement with the matching name, or null if not found</returns>
    public override UIElement FindChildByName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return null;

        // Create a defensive copy to prevent concurrent modification exceptions
        var childrenCopy = new List<UIElement>(_children);
        
        // Check direct children first
        foreach (var child in childrenCopy)
        {
            if (child.Name == name)
                return child;
        }

        // Recursively search in child layout groups
        foreach (var child in childrenCopy)
        {
            if (child is LayoutGroup childLayoutGroup)
            {
                var result = childLayoutGroup.FindChildByName(name);
                if (result != null)
                    return result;
            }
        }

        return null;
    }

    /// <summary>
    /// Recursively searches for all UI elements with the specified name.
    /// Returns a list of all matching elements, or an empty list if no elements with that name exist.
    /// </summary>
    /// <param name="name">The name to search for</param>
    /// <returns>A list of all UIElements with the matching name</returns>
    public override List<UIElement> FindAllChildrenByName(string name)
    {
        var results = new List<UIElement>();

        if (string.IsNullOrEmpty(name))
            return results;

        // Create a defensive copy to prevent concurrent modification exceptions
        var childrenCopy = new List<UIElement>(_children);
        
        // Check direct children
        foreach (var child in childrenCopy)
        {
            if (child.Name == name)
                results.Add(child);
        }

        // Recursively search in child layout groups
        foreach (var child in childrenCopy)
        {
            if (child is LayoutGroup childLayoutGroup)
            {
                results.AddRange(childLayoutGroup.FindAllChildrenByName(name));
            }
        }

        return results;
    }
}
