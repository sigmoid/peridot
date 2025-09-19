namespace Peridot.UI;

using Microsoft.Xna.Framework;

public class HorizontalLayoutGroup : LayoutGroup
{
    public enum HorizontalAlignment
    {
        Left,
        Center,
        Right
    }

    public enum VerticalAlignment
    {
        Top,
        Center,
        Bottom
    }

    private HorizontalAlignment _horizontalAlignment;
    private VerticalAlignment _verticalAlignment;

    public HorizontalLayoutGroup(Rectangle bounds, int spacing, 
        HorizontalAlignment horizontalAlignment = HorizontalAlignment.Left,
        VerticalAlignment verticalAlignment = VerticalAlignment.Center,
        Color? backgroundColor = null) 
        : base(bounds, spacing, backgroundColor)
    {
        _horizontalAlignment = horizontalAlignment;
        _verticalAlignment = verticalAlignment;
    }

    protected override void UpdateChildPositions()
    {
        if (_children.Count == 0) return;

        // Calculate total width needed
        int totalChildrenWidth = 0;
        int maxChildHeight = 0;

        foreach (var child in _children)
        {
            var childBounds = child.GetBoundingBox();
            totalChildrenWidth += childBounds.Width;
            maxChildHeight = Math.Max(maxChildHeight, childBounds.Height);
        }

        int totalSpacing = (_children.Count - 1) * _spacing;
        int totalContentWidth = totalChildrenWidth + totalSpacing;

        // Calculate starting X position based on alignment
        int startX = _horizontalAlignment switch
        {
            HorizontalAlignment.Left => _bounds.X,
            HorizontalAlignment.Center => _bounds.X + (_bounds.Width - totalContentWidth) / 2,
            HorizontalAlignment.Right => _bounds.X + _bounds.Width - totalContentWidth,
            _ => _bounds.X
        };

        // Position each child
        int currentX = startX;
        foreach (var child in _children)
        {
            var childBounds = child.GetBoundingBox();
            
            // Calculate Y position based on vertical alignment
            int childY = _verticalAlignment switch
            {
                VerticalAlignment.Top => _bounds.Y,
                VerticalAlignment.Center => _bounds.Y + (_bounds.Height - childBounds.Height) / 2,
                VerticalAlignment.Bottom => _bounds.Y + _bounds.Height - childBounds.Height,
                _ => _bounds.Y
            };

            childBounds.X = currentX;
            childBounds.Y = childY;
            // Don't modify width/height for horizontal layout

            currentX += childBounds.Width + _spacing;

            child.SetBounds(childBounds);
        }
    }

    public void SetHorizontalAlignment(HorizontalAlignment alignment)
    {
        _horizontalAlignment = alignment;
        UpdateChildPositions();
    }

    public void SetVerticalAlignment(VerticalAlignment alignment)
    {
        _verticalAlignment = alignment;
        UpdateChildPositions();
    }
}
