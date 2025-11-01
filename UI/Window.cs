namespace Peridot.UI;

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Peridot;

public class Window : UIContainer
{
    private Rectangle _bounds;
    private Rectangle _titleBarBounds;
    private Rectangle _contentBounds;
    private Rectangle _closeButtonBounds;

    private readonly SpriteFont _titleFont;

    private readonly Texture2D _pixel;

    private string _title;
    private Color _backgroundColor;
    private Color _titleBarColor;
    private Color _titleTextColor;
    private Color _borderColor;
    private Color _closeButtonColor;
    private Color _closeButtonHoverColor;
    private Color _closeButtonTextColor;
    private int _borderThickness;

    private bool _isDragging = false;
    private Point _dragStartMouse;
    private Point _dragStartWindow;
    private bool _isCloseButtonHovered = false;
    private bool _previousMousePressed = false;

    // Static tracking to prevent multiple windows from dragging simultaneously
    private static Window _currentlyDragging = null;

    // Event for when the window is closed
    public event Action<Window> OnWindowClosed;

    public Window(
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
        _closeButtonColor = Color.DarkRed;
        _closeButtonHoverColor = Color.Red;
        _closeButtonTextColor = Color.White;
        _borderThickness = borderThickness;

        _pixel = new Texture2D(Core.GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });

        RecalculateSubBounds();
    }

    private void RecalculateSubBounds()
    {
        int titleBarHeight = (int)_titleFont.MeasureString("A").Y + 10; // padding
        _titleBarBounds = new Rectangle(_bounds.X, _bounds.Y, _bounds.Width, titleBarHeight);
        
        // Close button is a square on the right side of the title bar
        int closeButtonSize = titleBarHeight - 4; // slightly smaller than title bar height
        _closeButtonBounds = new Rectangle(
            _bounds.X + _bounds.Width - closeButtonSize - 2, 
            _bounds.Y + 2, 
            closeButtonSize, 
            closeButtonSize
        );
        
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
        bool mousePressed = mouseDown && !_previousMousePressed;
        _previousMousePressed = mouseDown;

        // Check if mouse is over close button
        _isCloseButtonHovered = _closeButtonBounds.Contains(mousePoint);

        // Handle close button click
        if (mousePressed && _isCloseButtonHovered)
        {
            Close();
            return; // Exit early since window is being closed
        }

        if (!_isDragging)
        {
            // Only start drag if no other window is currently being dragged
            // and the mouse is down on this window's title bar (but not on close button)
            if (mouseDown && _titleBarBounds.Contains(mousePoint) && !_closeButtonBounds.Contains(mousePoint) && _currentlyDragging == null)
            {
                _isDragging = true;
                _currentlyDragging = this;
                _dragStartMouse = mousePoint;
                _dragStartWindow = new Point(_bounds.X, _bounds.Y);
                // Bring to front if parent is a container
                (GetParent() as UIContainer)?.BringChildToFront(this);
            }
        }
        else
        {
            // Continue drag only if this window is the one currently dragging
            if (_currentlyDragging == this)
            {
                var delta = mousePoint - _dragStartMouse;
                var newPos = _dragStartWindow + delta;
                SetBounds(new Rectangle(newPos.X, newPos.Y, _bounds.Width, _bounds.Height));

                // End drag on release
                if (!mouseDown)
                {
                    _isDragging = false;
                    _currentlyDragging = null;
                }
            }
            else
            {
                // Safety check: if somehow this window thinks it's dragging but another window is marked as dragging
                _isDragging = false;
            }
        }

        // Update children
        UpdateChildren(deltaTime);
    }

    public void Close()
    {
        // Trigger the close event
        OnWindowClosed?.Invoke(this);
        
        // Remove from parent if it exists (this will trigger proper cleanup)
        var parent = GetParent() as UIContainer;
        parent?.DestroyChild(this);
    }

    public override void OnRemovedFromUI()
    {
        // Clear dragging state if this window was being dragged
        if (_currentlyDragging == this)
        {
            _currentlyDragging = null;
            _isDragging = false;
        }

        // Clean up all children first
        DestroyAllChildren();

        // Dispose the pixel texture if we created it
        _pixel?.Dispose();

        // Call base cleanup
        base.OnRemovedFromUI();
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

        // Close button
        Color closeButtonCurrentColor = _isCloseButtonHovered ? _closeButtonHoverColor : _closeButtonColor;
        spriteBatch.Draw(_pixel, _closeButtonBounds, null, closeButtonCurrentColor, 0, Vector2.Zero, SpriteEffects.None, GetActualOrder() + 0.03f);
        
        // Close button X symbol
        string closeText = "X";
        var closeTextSize = _titleFont.MeasureString(closeText);
        var closeTextPos = new Vector2(
            _closeButtonBounds.X + (_closeButtonBounds.Width - closeTextSize.X) / 2,
            _closeButtonBounds.Y + (_closeButtonBounds.Height - closeTextSize.Y) / 2
        );
        spriteBatch.DrawString(_titleFont, closeText, closeTextPos, _closeButtonTextColor, 0, Vector2.Zero, 1.0f, SpriteEffects.None, GetActualOrder() + 0.04f);

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

    public void SetCloseButtonColors(Color? closeButton = null, Color? closeButtonHover = null, Color? closeButtonText = null)
    {
        if (closeButton.HasValue) _closeButtonColor = closeButton.Value;
        if (closeButtonHover.HasValue) _closeButtonHoverColor = closeButtonHover.Value;
        if (closeButtonText.HasValue) _closeButtonTextColor = closeButtonText.Value;
    }

    public void SetBorderThickness(int thickness)
    {
        _borderThickness = Math.Max(0, thickness);
        RecalculateSubBounds();
    }

    // Static method to reset dragging state if needed (e.g., when switching scenes)
    public static void ResetDragState()
    {
        _currentlyDragging = null;
    }

    // Property to check if any window is currently being dragged
    public static bool IsAnyWindowDragging => _currentlyDragging != null;
}