using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public class UIElement
{
    public string Name = "";
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

    /// <summary>
    /// Searches for a UI element with the specified name starting from this element.
    /// This method provides a generic search interface that delegates to container-specific implementations.
    /// </summary>
    /// <param name="name">The name to search for</param>
    /// <returns>The first UIElement with the matching name, or null if not found</returns>
    public virtual UIElement FindChildByName(string name)
    {
        // Default implementation for non-container elements
        // Container elements (Canvas, LayoutGroup) override this method
        return null;
    }

    /// <summary>
    /// Searches for all UI elements with the specified name starting from this element.
    /// This method provides a generic search interface that delegates to container-specific implementations.
    /// </summary>
    /// <param name="name">The name to search for</param>
    /// <returns>A list of all UIElements with the matching name</returns>
    public virtual System.Collections.Generic.List<UIElement> FindAllChildrenByName(string name)
    {
        // Default implementation for non-container elements
        // Container elements (Canvas, LayoutGroup) override this method
        return new System.Collections.Generic.List<UIElement>();
    }
}