namespace Peridot.UI;

using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public abstract class LayoutGroup : IUIElement
{
    protected Rectangle _bounds;
    protected int _spacing;
    protected List<IUIElement> _children;
    protected Color _backgroundColor;
    protected bool _drawBackground;

    public LayoutGroup(Rectangle bounds, int spacing, Color? backgroundColor = null)
    {
        _bounds = bounds;
        _spacing = spacing;
        _children = new List<IUIElement>();
        _backgroundColor = backgroundColor ?? Color.Transparent;
        _drawBackground = backgroundColor.HasValue;
    }

    public virtual void AddChild(IUIElement child)
    {
        _children.Add(child);
        UpdateChildPositions();
    }

    public virtual void RemoveChild(IUIElement child)
    {
        _children.Remove(child);
        UpdateChildPositions();
    }

    public virtual void ClearChildren()
    {
        _children.Clear();
    }

    public IReadOnlyList<IUIElement> Children => _children;

    protected abstract void UpdateChildPositions();

    public virtual void Update(float deltaTime)
    {
        foreach (var child in _children)
        {
            child.Update(deltaTime);
        }
    }

    public virtual void Draw(SpriteBatch spriteBatch)
    {
        if (_drawBackground)
        {
            // Create a 1x1 white pixel texture for drawing backgrounds
            var pixel = new Texture2D(spriteBatch.GraphicsDevice, 1, 1);
            pixel.SetData(new[] { Color.White });
            spriteBatch.Draw(pixel, _bounds, _backgroundColor);
            pixel.Dispose();
        }

        foreach (var child in _children)
        {
            child.Draw(spriteBatch);
        }
    }

    public virtual void OnClick()
    {
        // Layout groups don't typically handle clicks directly,
        // but you can override this if needed
    }

    public Rectangle GetBoundingBox()
    {
        return _bounds;
    }

    public virtual void SetBounds(Rectangle bounds)
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
}
