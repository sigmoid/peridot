namespace Peridot.UI;

using Microsoft.Xna.Framework;

public class VerticalLayoutGroup : LayoutGroup
{
    public enum HorizontalAlignment
    {
        Left,
        Center,
        Right,
        Stretch
    }

    public enum VerticalAlignment
    {
        Top,
        Center,
        Bottom
    }

    private HorizontalAlignment _horizontalAlignment;
    private VerticalAlignment _verticalAlignment;

    public VerticalLayoutGroup(Rectangle bounds, int spacing, 
        HorizontalAlignment horizontalAlignment = HorizontalAlignment.Stretch,
        VerticalAlignment verticalAlignment = VerticalAlignment.Top,
        Color? backgroundColor = null) 
        : base(bounds, spacing, backgroundColor)
    {
        _horizontalAlignment = horizontalAlignment;
        _verticalAlignment = verticalAlignment;
    }

    protected override void UpdateChildPositions()
    {
        if (_children.Count == 0) return;

        // Calculate total height needed
        int totalChildrenHeight = 0;
        int maxChildWidth = 0;

        foreach (var child in _children)
        {
            var childBounds = child.GetBoundingBox();
            totalChildrenHeight += childBounds.Height;
            maxChildWidth = Math.Max(maxChildWidth, childBounds.Width);
        }

        int totalSpacing = (_children.Count - 1) * _spacing;
        int totalContentHeight = totalChildrenHeight + totalSpacing;

        // Calculate starting Y position based on alignment
        int startY = _verticalAlignment switch
        {
            VerticalAlignment.Top => _bounds.Y,
            VerticalAlignment.Center => _bounds.Y + (_bounds.Height - totalContentHeight) / 2,
            VerticalAlignment.Bottom => _bounds.Y + _bounds.Height - totalContentHeight,
            _ => _bounds.Y
        };

        // Position each child
        int currentY = startY;
        foreach (var child in _children)
        {
            var childBounds = child.GetBoundingBox();
            
            // Calculate X position and width based on horizontal alignment
            switch (_horizontalAlignment)
            {
                case HorizontalAlignment.Left:
                    childBounds.X = _bounds.X;
                    break;
                case HorizontalAlignment.Center:
                    childBounds.X = _bounds.X + (_bounds.Width - childBounds.Width) / 2;
                    break;
                case HorizontalAlignment.Right:
                    childBounds.X = _bounds.X + _bounds.Width - childBounds.Width;
                    break;
                case HorizontalAlignment.Stretch:
                    childBounds.X = _bounds.X;
                    childBounds.Width = _bounds.Width;
                    break;
            }

            childBounds.Y = currentY;
            currentY += childBounds.Height + _spacing;

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