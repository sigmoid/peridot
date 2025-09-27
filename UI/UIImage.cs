using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Peridot;

public class UIImage : IUIElement
{
    private Texture2D _texture;
    private Rectangle _bounds;
    private Color _tintColor;

    public UIImage(Texture2D texture, Rectangle bounds, Color? tintColor = null)
    {
        _texture = texture;
        _bounds = bounds;
        _tintColor = tintColor ?? Color.White;
    }

    public override void Update(float deltaTime)
    {
        // No dynamic behavior for static images
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        if (_texture != null)
        {
            spriteBatch.Draw(_texture, _bounds, _tintColor);
        }
    }

    public override Rectangle GetBoundingBox()
    {
        return _bounds;
    }

    public override void SetBounds(Rectangle bounds)
    {
        _bounds = bounds;
    }

    public void SetTintColor(Color color)
    {
        _tintColor = color;
    }
}