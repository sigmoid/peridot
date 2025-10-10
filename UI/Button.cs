namespace Peridot.UI;

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Peridot;

public class Button : UIElement
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
    private bool _wasMousePressed = false;

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

    public override void Update(float deltaTime)
    {
        if(!IsVisible()) return;
        
        _isHovered = false;

        var mouseState = Mouse.GetState();
        var mousePosition = new Vector2(mouseState.X, mouseState.Y);

        Console.WriteLine($"Mouse Position: {mousePosition}");
        Console.WriteLine($"Button Bounds: {_bounds}");
        if (_bounds.Contains(mousePosition))
        {
            _isHovered = true;
        }

        bool isMousePressed = mouseState.LeftButton == ButtonState.Pressed;
        bool isMouseClick = isMousePressed && !_wasMousePressed;
        _wasMousePressed = isMousePressed;

        if(_isHovered && isMouseClick)
        {
            OnClick();
        }
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        spriteBatch.Draw(_pixel, _bounds, null, _isHovered ? _hoverColor : _defaultColor, 0, Vector2.Zero, SpriteEffects.None, 0f);

        var textSize = _font.MeasureString(_text);
        var textPosition = new Vector2(
            _bounds.X + (_bounds.Width - textSize.X) / 2,
            _bounds.Y + (_bounds.Height - textSize.Y) / 2
        );

        spriteBatch.DrawString(_font, _text, textPosition, _textColor, 0, Vector2.Zero, 1.0f, SpriteEffects.None, 0f);
    }

    public void OnClick()
    {
        _onClick?.Invoke();
    }

    public override Rectangle GetBoundingBox()
    {
        return _bounds;
    }

    public override void SetBounds(Rectangle bounds)
    {
        _bounds = bounds;
    }
}