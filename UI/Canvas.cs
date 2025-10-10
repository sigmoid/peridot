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
public class Canvas : UIContainer
{
    private Rectangle _bounds;
    private Color _backgroundColor;
    private bool _drawBackground;
    private bool _clipToBounds;
    private Texture2D _pixel;

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
    public override void AddChild(UIElement child)
    {
        base.AddChild(child);
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



    public override void Update(float deltaTime)
    {
        if (!IsVisible()) return;

        UpdateChildren(deltaTime);
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
    /// Disposes resources used by the canvas.
    /// </summary>
    public void Dispose()
    {
        _pixel?.Dispose();
    }
}