using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Peridot.UI
{

public class UIElement
{
    public string Name = "";
    protected bool _isVisible = true;
    private UIElement _parent = null;
    public float Order { get; set; } = 0.5f;
    public float LocalOrderOffset = 0.0f;
    public virtual void Update(float deltaTime) { }
    public virtual void Draw(SpriteBatch spriteBatch) { }
    public virtual Rectangle GetBoundingBox() { return Rectangle.Empty; }
    public virtual void SetBounds(Rectangle bounds) { }
    public virtual bool IsVisible() { return _isVisible; }
    public virtual void SetVisibility(bool isVisible) { _isVisible = isVisible; }
    public virtual void SetParent(UIElement parent) { _parent = parent; return; }
    public UIElement GetParent() { return _parent; }
    public UIElement GetRoot()
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

    /// <summary>
    /// Called when this UI element is being removed from the UI system.
    /// Override this method to perform cleanup, dispose resources, unsubscribe from events, etc.
    /// This is called automatically by RemoveChild() before the element is actually removed.
    /// </summary>
    public virtual void OnRemovedFromUI()
    {
        // Default implementation - override in derived classes for cleanup
        // Examples of what to do here:
        // - Dispose textures or other resources
        // - Unsubscribe from events
        // - Clear references to prevent memory leaks
        // - Save state if needed
    }

    /// <summary>
    /// Recursively cleans up this UI element and all its children (if it's a container).
    /// This method ensures proper cleanup of the entire UI subtree.
    /// </summary>
    public virtual void DeepCleanup()
    {
        // If this is a container, cleanup all children first
        if (this is UIContainer container)
        {
            // Create a copy to avoid collection modification during iteration
            var childrenCopy = new System.Collections.Generic.List<UIElement>(container.Children);
            foreach (var child in childrenCopy)
            {
                child.DeepCleanup();
            }
            container.ClearChildren();
        }

        // Clean up this element
        OnRemovedFromUI();
        
        // Clear parent reference
        SetParent(null);
        
        // Hide the element
        SetVisibility(false);
    }
}

}