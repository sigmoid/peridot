namespace Peridot.UI;

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Peridot;

public class Button : IUIElement
{ 
    private Rectangle _bounds;
    private string _text;
    private SpriteFont _font;
    private Color _backgroundColor;
    private Color _textColor;
    private Action _onClick;
    private Texture2D _pixel;
    private Color _hoverColor = Color.LightGray;
    private Color _defaultColor = Color.DarkGray;
    private bool _isHovered = false;

    public Button(Rectangle bounds, string text, SpriteFont font, Color regularColor, Color hoverColor, Color textColor, Action onClick)
    {
        _bounds = bounds;
        _text = text;
        _font = font;
        _backgroundColor = regularColor;
        _hoverColor = hoverColor;
        _textColor = textColor;
        _onClick = onClick;

        _pixel = new Texture2D(Core.GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
    }

    public void Update(float deltaTime)
    {
        _isHovered = false;

        var mousePosition = Core.InputManager.GetMousePosition() * new Vector2(Core.ScreenWidth, Core.ScreenHeight);

        if (_bounds.Contains(mousePosition))
        {
            _isHovered = true;
        }

        if(_isHovered && Core.InputManager.GetButton("LeftMouse").IsPressed)
        {
            OnClick();
        }
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        spriteBatch.Draw(_pixel, _bounds, _isHovered ? _hoverColor : _defaultColor);

        var textSize = _font.MeasureString(_text);
        var textPosition = new Vector2(
            _bounds.X + (_bounds.Width - textSize.X) / 2,
            _bounds.Y + (_bounds.Height - textSize.Y) / 2
        );

        spriteBatch.DrawString(_font, _text, textPosition, _textColor);
    }

    public void OnClick()
    {
        _onClick?.Invoke();
    }

    public Rectangle GetBoundingBox()
    {
        return _bounds;
    }

    public void SetBounds(Rectangle bounds)
    {
        _bounds = bounds;
    }
}