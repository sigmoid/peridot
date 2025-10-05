using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public class UIElement
{
    protected bool _isVisible = true;
    private UIElement? _parent = null;
    public float Order { get; set; } = 0.5f;
    public float LocalOrderOffset = 0.0f;
    public virtual void Update(float deltaTime) { }
    public virtual void Draw(SpriteBatch spriteBatch) { }
    public virtual Rectangle GetBoundingBox() { return Rectangle.Empty; }
    public virtual void SetBounds(Rectangle bounds) { }
    public virtual bool IsVisible() { return _isVisible; }
    public virtual void SetVisibility(bool isVisible) { _isVisible = isVisible; }
    public virtual void SetParent(UIElement? parent) { _parent = parent; return; }
    public UIElement? GetParent() { return _parent; }
    public UIElement? GetRoot()
    {
        UIElement current = this;
        while (current.GetParent() != null)
        {
            current = current.GetParent();
        }
        return current;
    }

    public float GetActualOrder()
    {
        if(_parent != null)
        {
            return _parent.GetActualOrder() + LocalOrderOffset;
        }
        return Order;
    }
}