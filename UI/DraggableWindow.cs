namespace Peridot.UI;

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Peridot;

public class DraggableWindow : UIContainer
{
    private Rectangle _bounds;
    private Rectangle _titleBarBounds;
    private Rectangle _contentBounds;

    private readonly SpriteFont _titleFont;

    private readonly Texture2D _pixel;

    private string _title;
    private Color _backgroundColor;
    private Color _titleBarColor;
    private Color _titleTextColor;
    private Color _borderColor;
    private int _borderThickness;

    private bool _isDragging = false;
    private Point _dragStartMouse;
    private Point _dragStartWindow;

    public DraggableWindow(
        Rectangle bounds,
        string title,
        SpriteFont titleFont,
        Color? backgroundColor = null,
        Color? titleBarColor = null,
        Color? titleTextColor = null,
        Color? borderColor = null,
        int borderThickness = 2
    )
    {
        _bounds = bounds;
        _title = title ?? string.Empty;
        _titleFont = titleFont;

        _backgroundColor = backgroundColor ?? Color.White;
        _titleBarColor = titleBarColor ?? Color.DarkSlateGray;
        _titleTextColor = titleTextColor ?? Color.White;
        _borderColor = borderColor ?? Color.Black;
        _borderThickness = borderThickness;

        _pixel = new Texture2D(Core.GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });

        RecalculateSubBounds();
    }

    private void RecalculateSubBounds()
    {
        int titleBarHeight = (int)_titleFont.MeasureString("A").Y + 10; // padding
        _titleBarBounds = new Rectangle(_bounds.X, _bounds.Y, _bounds.Width, titleBarHeight);
        _contentBounds = new Rectangle(
            _bounds.X + _borderThickness,
            _bounds.Y + titleBarHeight + _borderThickness,
            _bounds.Width - _borderThickness * 2,
            _bounds.Height - titleBarHeight - _borderThickness * 2
        );
    }

    public override void Update(float deltaTime)
    {
        if (!IsVisible()) return;

        var mouse = Mouse.GetState();
        var mousePoint = new Point(mouse.X, mouse.Y);

        bool mouseDown = mouse.LeftButton == ButtonState.Pressed;

        if (!_isDragging)
        {
            // Start drag when pressing on title bar
            if (mouseDown && _titleBarBounds.Contains(mousePoint))
            {
                _isDragging = true;
                _dragStartMouse = mousePoint;
                _dragStartWindow = new Point(_bounds.X, _bounds.Y);
                // Bring to front if parent is a container
                (GetParent() as UIContainer)?.BringChildToFront(this);
            }
        }
        else
        {
            // Continue drag
            var delta = mousePoint - _dragStartMouse;
            var newPos = _dragStartWindow + delta;
            SetBounds(new Rectangle(newPos.X, newPos.Y, _bounds.Width, _bounds.Height));

            // End drag on release
            if (!mouseDown)
            {
                _isDragging = false;
            }
        }

        // Update children
        UpdateChildren(deltaTime);
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        if (!IsVisible()) return;

        // Border
        spriteBatch.Draw(_pixel, new Rectangle(_bounds.X - _borderThickness, _bounds.Y - _borderThickness,
                                               _bounds.Width + _borderThickness * 2, _bounds.Height + _borderThickness * 2),
                         null, _borderColor, 0, Vector2.Zero, SpriteEffects.None, GetActualOrder());

        // Background
        spriteBatch.Draw(_pixel, _bounds, null, _backgroundColor, 0, Vector2.Zero, SpriteEffects.None, GetActualOrder() + 0.01f);

        // Title bar
        spriteBatch.Draw(_pixel, _titleBarBounds, null, _titleBarColor, 0, Vector2.Zero, SpriteEffects.None, GetActualOrder() + 0.02f);

        // Title text
        if (!string.IsNullOrEmpty(_title))
        {
            var titleSize = _titleFont.MeasureString(_title);
            var titlePos = new Vector2(
                _titleBarBounds.X + 10,
                _titleBarBounds.Y + (_titleBarBounds.Height - titleSize.Y) / 2
            );
            spriteBatch.DrawString(_titleFont, _title, titlePos, _titleTextColor);
        }

        // Draw children as-is (no clipping)
        foreach (var child in Children)
        {
            if (child.IsVisible())
            {
                child.Draw(spriteBatch);
            }
        }
    }

    public override Rectangle GetBoundingBox()
    {
        return _bounds;
    }

    public override void SetBounds(Rectangle bounds)
    {
        // Move window and maintain relative positions of children
        var deltaX = bounds.X - _bounds.X;
        var deltaY = bounds.Y - _bounds.Y;

        _bounds = bounds;
        RecalculateSubBounds();

        // Shift children by delta
        // Make a copy to avoid modification during iteration issues
        var childrenCopy = new System.Collections.Generic.List<UIElement>(Children);
        foreach (var child in childrenCopy)
        {
            var cb = child.GetBoundingBox();
            if (!cb.IsEmpty)
            {
                child.SetBounds(new Rectangle(cb.X + deltaX, cb.Y + deltaY, cb.Width, cb.Height));
            }
        }
    }

    public Rectangle GetContentBounds() => _contentBounds;

    public void SetTitle(string title)
    {
        _title = title ?? string.Empty;
        RecalculateSubBounds();
    }

    public void SetColors(Color? background = null, Color? titleBar = null, Color? titleText = null, Color? border = null)
    {
        if (background.HasValue) _backgroundColor = background.Value;
        if (titleBar.HasValue) _titleBarColor = titleBar.Value;
        if (titleText.HasValue) _titleTextColor = titleText.Value;
        if (border.HasValue) _borderColor = border.Value;
    }

    public void SetBorderThickness(int thickness)
    {
        _borderThickness = Math.Max(0, thickness);
        RecalculateSubBounds();
    }
}
