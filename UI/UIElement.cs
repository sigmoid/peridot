using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public class IUIElement
{
    protected bool _isVisible = true;
    public virtual void Update(float deltaTime) { }
    public virtual void Draw(SpriteBatch spriteBatch) { }
    public virtual Rectangle GetBoundingBox() { return Rectangle.Empty; }
    public virtual void SetBounds(Rectangle bounds) { }
    public virtual bool IsVisible() { return _isVisible; }
    public virtual void SetVisibility(bool isVisible) { _isVisible = isVisible; }
}