namespace Peridot.UI;

using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Peridot;

/// <summary>
/// A Canvas is a simple container that groups UI elements together without enforcing any layout.
/// Elements maintain their absolute positions and the Canvas simply manages their visibility,
/// updates, and drawing as a group. Optionally provides clipping to canvas bounds.
/// </summary>
public class Canvas : UIElement
{
    private Rectangle _bounds;
    private List<UIElement> _children;
    private Color _backgroundColor;
    private bool _drawBackground;
    private bool _clipToBounds;
    private Texture2D _pixel;

    /// <summary>
    /// Gets the children elements in this canvas.
    /// </summary>
    public IReadOnlyList<UIElement> Children => _children;

    /// <summary>
    /// Gets or sets whether child elements should be clipped to the canvas bounds.
    /// </summary>
    public bool ClipToBounds 
    { 
        get => _clipToBounds; 
        set => _clipToBounds = value; 
    }

    public Canvas(Rectangle bounds, Color? backgroundColor = null, bool clipToBounds = false)
    {
        _bounds = bounds;
        _children = new List<UIElement>();
        _backgroundColor = backgroundColor ?? Color.Transparent;
        _drawBackground = backgroundColor.HasValue && backgroundColor != Color.Transparent;
        _clipToBounds = clipToBounds;

        // Create pixel texture for drawing background
        _pixel = new Texture2D(Core.GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
    }

    /// <summary>
    /// Adds a child element to the canvas. The element keeps its current position.
    /// </summary>
    public virtual void AddChild(UIElement child)
    {
        if (child != null && !_children.Contains(child))
        {
            _children.Add(child);
            child.SetParent(this);
        }
    }

    /// <summary>
    /// Adds a child element at a specific position relative to the canvas.
    /// </summary>
    public virtual void AddChild(UIElement child, Vector2 position)
    {
        if (child != null)
        {
            var childBounds = child.GetBoundingBox();
            var newBounds = new Rectangle(
                _bounds.X + (int)position.X,
                _bounds.Y + (int)position.Y,
                childBounds.Width,
                childBounds.Height
            );
            child.SetBounds(newBounds);
            AddChild(child);
        }
    }

    /// <summary>
    /// Adds a child element at a specific position and size relative to the canvas.
    /// </summary>
    public virtual void AddChild(UIElement child, Rectangle relativeBounds)
    {
        if (child != null)
        {
            var absoluteBounds = new Rectangle(
                _bounds.X + relativeBounds.X,
                _bounds.Y + relativeBounds.Y,
                relativeBounds.Width,
                relativeBounds.Height
            );
            child.SetBounds(absoluteBounds);
            AddChild(child);
        }
    }

    /// <summary>
    /// Removes a child element from the canvas.
    /// </summary>
    public virtual void RemoveChild(UIElement child)
    {
        _children.Remove(child);
    }

    /// <summary>
    /// Removes all child elements from the canvas.
    /// </summary>
    public virtual void ClearChildren()
    {
        _children.Clear();
    }

    /// <summary>
    /// Finds the first child element at the specified position (in screen coordinates).
    /// </summary>
    public UIElement FindChildAt(Vector2 position)
    {
        // Search in reverse order to find the topmost element
        for (int i = _children.Count - 1; i >= 0; i--)
        {
            var child = _children[i];
            if (child.IsVisible() && child.GetBoundingBox().Contains(position))
            {
                return child;
            }
        }
        return null;
    }

    /// <summary>
    /// Gets all child elements at the specified position (in screen coordinates).
    /// </summary>
    public List<UIElement> FindChildrenAt(Vector2 position)
    {
        var result = new List<UIElement>();
        // Create a copy to avoid collection modification during iteration
        var childrenCopy = new List<UIElement>(_children);
        foreach (var child in childrenCopy)
        {
            if (child.IsVisible() && child.GetBoundingBox().Contains(position))
            {
                result.Add(child);
            }
        }
        return result;
    }

    /// <summary>
    /// Moves all child elements by the specified offset.
    /// </summary>
    public void MoveChildren(Vector2 offset)
    {
        // Create a copy to avoid collection modification during iteration
        var childrenCopy = new List<UIElement>(_children);
        foreach (var child in childrenCopy)
        {
            var childBounds = child.GetBoundingBox();
            var newBounds = new Rectangle(
                childBounds.X + (int)offset.X,
                childBounds.Y + (int)offset.Y,
                childBounds.Width,
                childBounds.Height
            );
            child.SetBounds(newBounds);
        }
    }

    /// <summary>
    /// Brings a child element to the front (drawn last, appears on top).
    /// </summary>
    public void BringChildToFront(UIElement child)
    {
        if (_children.Remove(child))
        {
            _children.Add(child);
        }
    }

    /// <summary>
    /// Sends a child element to the back (drawn first, appears behind others).
    /// </summary>
    public void SendChildToBack(UIElement child)
    {
        if (_children.Remove(child))
        {
            _children.Insert(0, child);
        }
    }

    public override void Update(float deltaTime)
    {
        if (!IsVisible()) return;

        // Create a copy to avoid collection modification during iteration
        var childrenCopy = new List<UIElement>(_children);
        foreach (var child in childrenCopy)
        {
            if (child.IsVisible())
            {
                child.Update(deltaTime);
            }
        }
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        if (!IsVisible()) return;

        // Draw background if enabled
        if (_drawBackground)
        {
            spriteBatch.Draw(_pixel, _bounds, null, _backgroundColor, 0, Vector2.Zero, SpriteEffects.None, GetActualOrder());
        }

        // Create a copy to avoid collection modification during iteration
        var childrenCopy = new List<UIElement>(_children);
        
        // Draw all visible children
        foreach (var child in childrenCopy)
        {
            if (child.IsVisible())
            {
                // If clipping is enabled, only draw children that intersect with bounds
                if (!_clipToBounds || _bounds.Intersects(child.GetBoundingBox()))
                {
                    child.Draw(spriteBatch);
                }
            }
        }
    }

    public override Rectangle GetBoundingBox()
    {
        return _bounds;
    }

    public override void SetBounds(Rectangle bounds)
    {
        // Calculate the offset for moving children
        var offset = new Vector2(bounds.X - _bounds.X, bounds.Y - _bounds.Y);
        
        // Update canvas bounds
        _bounds = bounds;
        
        // Move all children by the same offset to maintain relative positions
        if (offset != Vector2.Zero)
        {
            MoveChildren(offset);
        }
    }

    /// <summary>
    /// Sets the background color and enables background drawing.
    /// </summary>
    public void SetBackgroundColor(Color color)
    {
        _backgroundColor = color;
        _drawBackground = color != Color.Transparent;
    }

    /// <summary>
    /// Disables background drawing.
    /// </summary>
    public void HideBackground()
    {
        _drawBackground = false;
    }

    /// <summary>
    /// Shows the background using the current background color.
    /// </summary>
    public void ShowBackground()
    {
        _drawBackground = true;
    }

    /// <summary>
    /// Gets the number of child elements in the canvas.
    /// </summary>
    public int ChildCount => _children.Count;

    /// <summary>
    /// Calculates the bounding rectangle that contains all visible children.
    /// </summary>
    public Rectangle GetChildrenBounds()
    {
        if (_children.Count == 0) return Rectangle.Empty;

        Rectangle bounds = Rectangle.Empty;
        bool first = true;

        // Create a copy to avoid collection modification during iteration
        var childrenCopy = new List<UIElement>(_children);
        foreach (var child in childrenCopy)
        {
            if (child.IsVisible())
            {
                var childBounds = child.GetBoundingBox();
                if (first)
                {
                    bounds = childBounds;
                    first = false;
                }
                else
                {
                    bounds = Rectangle.Union(bounds, childBounds);
                }
            }
        }

        return bounds;
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

        // Create a copy to avoid collection modification during iteration
        var childrenCopy = new List<UIElement>(_children);

        // Check direct children first
        foreach (var child in childrenCopy)
        {
            if (child.Name == name)
                return child;
        }

        // Recursively search in child containers (Canvas and LayoutGroups)
        foreach (var child in childrenCopy)
        {
            if (child is Canvas childCanvas)
            {
                var result = childCanvas.FindChildByName(name);
                if (result != null)
                    return result;
            }
            else if (child is LayoutGroup childLayoutGroup)
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

        // Create a copy to avoid collection modification during iteration
        var childrenCopy = new List<UIElement>(_children);

        // Check direct children
        foreach (var child in childrenCopy)
        {
            if (child.Name == name)
                results.Add(child);
        }

        // Recursively search in child containers (Canvas and LayoutGroups)
        foreach (var child in childrenCopy)
        {
            if (child is Canvas childCanvas)
            {
                results.AddRange(childCanvas.FindAllChildrenByName(name));
            }
            else if (child is LayoutGroup childLayoutGroup)
            {
                results.AddRange(childLayoutGroup.FindAllChildrenByName(name));
            }
        }

        return results;
    }

    /// <summary>
    /// Disposes resources used by the canvas.
    /// </summary>
    public void Dispose()
    {
        _pixel?.Dispose();
    }
}