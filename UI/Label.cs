namespace Peridot.UI;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Peridot;

public class Label : IUIElement
{
    private string _text;
    private Rectangle _bounds;
    private SpriteFont _font;
    private Color _textColor;
    private Color _backgroundColor;
    private Texture2D _pixel;
    private bool _drawBackground;

    public string Text 
    { 
        get => _text; 
        set => _text = value ?? string.Empty; 
    }
    
    public Color TextColor 
    { 
        get => _textColor; 
        set => _textColor = value; 
    }
    
    public Color BackgroundColor 
    { 
        get => _backgroundColor; 
        set 
        { 
            _backgroundColor = value; 
            _drawBackground = value != Color.Transparent;
        } 
    }

    public Label(Rectangle bounds, string text, SpriteFont font, Color textColor, Color? backgroundColor = null)
    {
        _bounds = bounds;
        _text = text ?? string.Empty;
        _font = font;
        _textColor = textColor;
        _backgroundColor = backgroundColor ?? Color.Transparent;
        _drawBackground = backgroundColor.HasValue && backgroundColor != Color.Transparent;

        _pixel = new Texture2D(Core.GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        if (_drawBackground)
        {
            spriteBatch.Draw(_pixel, _bounds, _backgroundColor);
        }

        if (!string.IsNullOrEmpty(_text))
        {
            var textSize = _font.MeasureString(_text);
            var textPosition = new Vector2(
                _bounds.X + (_bounds.Width - textSize.X) / 2,
                _bounds.Y + (_bounds.Height - textSize.Y) / 2
            );

            spriteBatch.DrawString(_font, _text, textPosition, _textColor);
        }
    }

    public void SetText(string text)
    {
        _text = text ?? string.Empty;
    }

    public Rectangle GetBoundingBox()
    {
        return _bounds;
    }

    public void OnClick()
    {
        // Labels typically don't handle clicks
    }

    public void SetBounds(Rectangle bounds)
    {
        _bounds = bounds;
    }

    public void Update(float deltaTime)
    {
        // Labels typically don't need updates
    }

    public void Dispose()
    {
        _pixel?.Dispose();
    }
}