namespace Peridot.UI;

using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public abstract class LayoutGroup : UIContainer
{
    protected Rectangle _bounds;
    protected int _spacing;
    protected Color _backgroundColor;
    protected bool _drawBackground;

    public LayoutGroup(Rectangle bounds, int spacing, Color? backgroundColor = null)
    {
        _bounds = bounds;
        _spacing = spacing;
        _backgroundColor = backgroundColor ?? Color.Transparent;
        _drawBackground = backgroundColor.HasValue;
    }

    protected override void OnChildAdded(UIElement child)
    {
        UpdateChildPositions();
    }

    protected override void OnChildRemoved(UIElement child)
    {
        UpdateChildPositions();
    }

    protected override void OnChildrenCleared()
    {
        // No need to update positions when all children are cleared
    }

    protected abstract void UpdateChildPositions();

    public override void Update(float deltaTime)
    {
        UpdateChildren(deltaTime);
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


}
