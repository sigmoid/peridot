namespace Peridot.UI;

using System.Collections.Generic;
using Microsoft.Xna.Framework;

/// <summary>
/// Base class for UI elements that can contain child elements.
/// Provides common functionality for child management and recursive searching.
/// </summary>
public abstract class UIContainer : UIElement
{
    protected List<UIElement> _children;

    protected UIContainer()
    {
        _children = new List<UIElement>();
    }

    /// <summary>
    /// Gets the children elements in this container.
    /// </summary>
    public virtual IReadOnlyList<UIElement> Children => _children;

    /// <summary>
    /// Gets the number of child elements in the container.
    /// </summary>
    public virtual int ChildCount => _children.Count;

    /// <summary>
    /// Adds a child element to the container.
    /// </summary>
    public virtual void AddChild(UIElement child)
    {
        if (child != null && !_children.Contains(child))
        {
            _children.Add(child);
            child.SetParent(this);
            OnChildAdded(child);
        }
    }

    /// <summary>
    /// Removes a child element from the container and performs proper cleanup.
    /// This calls OnRemovedFromUI() on the child before removing it.
    /// </summary>
    public virtual void RemoveChild(UIElement child)
    {
        if (child != null && _children.Remove(child))
        {
            // Call cleanup method on the child first
            child.OnRemovedFromUI();
            
            // Clear parent reference
            child.SetParent(null);
            
            // Notify that child was removed
            OnChildRemoved(child);
        }
    }

    /// <summary>
    /// Removes and destroys a child element from the container with deep cleanup.
    /// This performs a complete cleanup of the child and all its descendants.
    /// Use this when you want to completely delete a UI element from the system.
    /// </summary>
    public virtual void DestroyChild(UIElement child)
    {
        if (child != null && _children.Contains(child))
        {
            // Remove from children list first
            _children.Remove(child);
            
            // Perform deep cleanup (this handles nested children and calls OnRemovedFromUI)
            child.DeepCleanup();
            
            // Notify that child was removed
            OnChildRemoved(child);
        }
    }

    /// <summary>
    /// Removes all child elements from the container.
    /// </summary>
    public virtual void ClearChildren()
    {
        // Create a copy to avoid collection modification during iteration
        var childrenCopy = new List<UIElement>(_children);
        
        foreach (var child in childrenCopy)
        {
            child.OnRemovedFromUI();
            child.SetParent(null);
        }
        _children.Clear();
        OnChildrenCleared();
    }

    /// <summary>
    /// Removes and destroys all child elements from the container with deep cleanup.
    /// This performs complete cleanup of all children and their descendants.
    /// </summary>
    public virtual void DestroyAllChildren()
    {
        // Create a copy to avoid collection modification during iteration
        var childrenCopy = new List<UIElement>(_children);
        
        _children.Clear();
        
        foreach (var child in childrenCopy)
        {
            child.DeepCleanup();
        }
        
        OnChildrenCleared();
    }

    /// <summary>
    /// Finds the first child element at the specified position (in screen coordinates).
    /// </summary>
    public virtual UIElement FindChildAt(Vector2 position)
    {
        // Search in reverse order to find the topmost element
        for (int i = _children.Count - 1; i >= 0; i--)
        {
            var child = _children[i];
            if (child.IsVisible() && child.GetBoundingBox().Contains(position))
            {
                return child;
            }
        }
        return null;
    }

    /// <summary>
    /// Gets all child elements at the specified position (in screen coordinates).
    /// </summary>
    public virtual List<UIElement> FindChildrenAt(Vector2 position)
    {
        var result = new List<UIElement>();
        // Create a copy to avoid collection modification during iteration
        var childrenCopy = new List<UIElement>(_children);
        foreach (var child in childrenCopy)
        {
            if (child.IsVisible() && child.GetBoundingBox().Contains(position))
            {
                result.Add(child);
            }
        }
        return result;
    }

    /// <summary>
    /// Recursively searches for a UI element with the specified name.
    /// Returns the first element found, or null if no element with that name exists.
    /// </summary>
    /// <param name="name">The name to search for</param>
    /// <returns>The first UIElement with the matching name, or null if not found</returns>
    public override UIElement FindChildByName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return null;

        // Create a copy to avoid collection modification during iteration
        var childrenCopy = new List<UIElement>(_children);

        // Check direct children first
        foreach (var child in childrenCopy)
        {
            if (child.Name == name)
                return child;
        }

        // Recursively search in child containers
        foreach (var child in childrenCopy)
        {
            if (child is UIContainer childContainer)
            {
                var result = childContainer.FindChildByName(name);
                if (result != null)
                    return result;
            }
        }

        return null;
    }

    /// <summary>
    /// Recursively searches for all UI elements with the specified name.
    /// Returns a list of all matching elements, or an empty list if no elements with that name exist.
    /// </summary>
    /// <param name="name">The name to search for</param>
    /// <returns>A list of all UIElements with the matching name</returns>
    public override List<UIElement> FindAllChildrenByName(string name)
    {
        var results = new List<UIElement>();

        if (string.IsNullOrEmpty(name))
            return results;

        // Create a copy to avoid collection modification during iteration
        var childrenCopy = new List<UIElement>(_children);

        // Check direct children
        foreach (var child in childrenCopy)
        {
            if (child.Name == name)
                results.Add(child);
        }

        // Recursively search in child containers
        foreach (var child in childrenCopy)
        {
            if (child is UIContainer childContainer)
            {
                results.AddRange(childContainer.FindAllChildrenByName(name));
            }
        }

        return results;
    }

    /// <summary>
    /// Brings a child element to the front (drawn last, appears on top).
    /// </summary>
    public virtual void BringChildToFront(UIElement child)
    {
        if (_children.Remove(child))
        {
            _children.Add(child);
        }
    }

    /// <summary>
    /// Sends a child element to the back (drawn first, appears behind others).
    /// </summary>
    public virtual void SendChildToBack(UIElement child)
    {
        if (_children.Remove(child))
        {
            _children.Insert(0, child);
        }
    }

    /// <summary>
    /// Calculates the bounding rectangle that contains all visible children.
    /// </summary>
    public virtual Rectangle GetChildrenBounds()
    {
        if (_children.Count == 0) return Rectangle.Empty;

        Rectangle bounds = Rectangle.Empty;
        bool first = true;

        // Create a copy to avoid collection modification during iteration
        var childrenCopy = new List<UIElement>(_children);
        foreach (var child in childrenCopy)
        {
            if (child.IsVisible())
            {
                var childBounds = child.GetBoundingBox();
                if (first)
                {
                    bounds = childBounds;
                    first = false;
                }
                else
                {
                    bounds = Rectangle.Union(bounds, childBounds);
                }
            }
        }

        return bounds;
    }

    /// <summary>
    /// Updates all child elements.
    /// </summary>
    protected virtual void UpdateChildren(float deltaTime)
    {
        // Create a copy to avoid collection modification during iteration
        var childrenCopy = new List<UIElement>(_children);
        foreach (var child in childrenCopy)
        {
            if (child.IsVisible())
            {
                child.Update(deltaTime);
            }
        }
    }

    /// <summary>
    /// Called when a child element is added. Override to implement custom behavior.
    /// </summary>
    /// <param name="child">The child that was added</param>
    protected virtual void OnChildAdded(UIElement child)
    {
        // Override in derived classes if needed
    }

    /// <summary>
    /// Called when a child element is removed. Override to implement custom behavior.
    /// </summary>
    /// <param name="child">The child that was removed</param>
    protected virtual void OnChildRemoved(UIElement child)
    {
        // Override in derived classes if needed
    }

    /// <summary>
    /// Called when all children are cleared. Override to implement custom behavior.
    /// </summary>
    protected virtual void OnChildrenCleared()
    {
        // Override in derived classes if needed
    }
}