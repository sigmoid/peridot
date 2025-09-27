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
public class Canvas : IUIElement
{
    private Rectangle _bounds;
    private List<IUIElement> _children;
    private Color _backgroundColor;
    private bool _drawBackground;
    private bool _clipToBounds;
    private Texture2D _pixel;

    /// <summary>
    /// Gets the children elements in this canvas.
    /// </summary>
    public IReadOnlyList<IUIElement> Children => _children;

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
        _children = new List<IUIElement>();
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
    public virtual void AddChild(IUIElement child)
    {
        if (child != null && !_children.Contains(child))
        {
            _children.Add(child);
        }
    }

    /// <summary>
    /// Adds a child element at a specific position relative to the canvas.
    /// </summary>
    public virtual void AddChild(IUIElement child, Vector2 position)
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
    public virtual void AddChild(IUIElement child, Rectangle relativeBounds)
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
    public virtual void RemoveChild(IUIElement child)
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
    public IUIElement FindChildAt(Vector2 position)
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
    public List<IUIElement> FindChildrenAt(Vector2 position)
    {
        var result = new List<IUIElement>();
        foreach (var child in _children)
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
        foreach (var child in _children)
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
    public void BringChildToFront(IUIElement child)
    {
        if (_children.Remove(child))
        {
            _children.Add(child);
        }
    }

    /// <summary>
    /// Sends a child element to the back (drawn first, appears behind others).
    /// </summary>
    public void SendChildToBack(IUIElement child)
    {
        if (_children.Remove(child))
        {
            _children.Insert(0, child);
        }
    }

    public override void Update(float deltaTime)
    {
        if (!IsVisible()) return;

        foreach (var child in _children)
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

        // Store original scissor state if clipping
        Rectangle? originalScissor = null;
        if (_clipToBounds && spriteBatch.GraphicsDevice.ScissorRectangle != _bounds)
        {
            originalScissor = spriteBatch.GraphicsDevice.ScissorRectangle;
            
            // End the current batch to change render state
            spriteBatch.End();
            
            // Set scissor rectangle for clipping
            spriteBatch.GraphicsDevice.ScissorRectangle = _bounds;
            
            // Restart batch with scissor test enabled
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, 
                            SamplerState.PointClamp, DepthStencilState.None, 
                            new RasterizerState { ScissorTestEnable = true });
        }

        // Draw background if enabled
        if (_drawBackground)
        {
            spriteBatch.Draw(_pixel, _bounds, _backgroundColor);
        }

        // Draw all visible children
        foreach (var child in _children)
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

        // Restore original scissor state if we changed it
        if (originalScissor.HasValue)
        {
            spriteBatch.End();
            spriteBatch.GraphicsDevice.ScissorRectangle = originalScissor.Value;
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, 
                            SamplerState.PointClamp, DepthStencilState.None, 
                            RasterizerState.CullCounterClockwise);
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

        foreach (var child in _children)
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
    /// Disposes resources used by the canvas.
    /// </summary>
    public void Dispose()
    {
        _pixel?.Dispose();
    }
}