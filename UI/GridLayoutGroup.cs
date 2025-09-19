namespace Peridot.UI;

using Microsoft.Xna.Framework;

public class GridLayoutGroup : LayoutGroup
{
    private int _columns;
    private int _rows;
    private int _horizontalSpacing;
    private int _verticalSpacing;

    public GridLayoutGroup(Rectangle bounds, int columns, int rows, 
        int horizontalSpacing = 5, int verticalSpacing = 5,
        Color? backgroundColor = null) 
        : base(bounds, horizontalSpacing, backgroundColor)
    {
        _columns = columns;
        _rows = rows;
        _horizontalSpacing = horizontalSpacing;
        _verticalSpacing = verticalSpacing;
    }

    protected override void UpdateChildPositions()
    {
        if (_children.Count == 0) return;

        // Calculate cell dimensions
        int totalHorizontalSpacing = (_columns - 1) * _horizontalSpacing;
        int totalVerticalSpacing = (_rows - 1) * _verticalSpacing;
        
        int cellWidth = (_bounds.Width - totalHorizontalSpacing) / _columns;
        int cellHeight = (_bounds.Height - totalVerticalSpacing) / _rows;

        // Position each child in grid cells
        for (int i = 0; i < _children.Count && i < (_columns * _rows); i++)
        {
            int row = i / _columns;
            int col = i % _columns;

            int x = _bounds.X + col * (cellWidth + _horizontalSpacing);
            int y = _bounds.Y + row * (cellHeight + _verticalSpacing);

            var cellBounds = new Rectangle(x, y, cellWidth, cellHeight);
            _children[i].SetBounds(cellBounds);
        }
    }

    public void SetGridSize(int columns, int rows)
    {
        _columns = columns;
        _rows = rows;
        UpdateChildPositions();
    }

    public void SetSpacing(int horizontalSpacing, int verticalSpacing)
    {
        _horizontalSpacing = horizontalSpacing;
        _verticalSpacing = verticalSpacing;
        UpdateChildPositions();
    }

    public int MaxChildren => _columns * _rows;
    public int Columns => _columns;
    public int Rows => _rows;
}
