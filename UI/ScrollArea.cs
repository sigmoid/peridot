namespace Peridot.UI;

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Peridot;

public class ScrollArea : UIElement
{
    private Rectangle _bounds;
    private List<UIElement> _children;
    private Vector2 _scrollOffset;
    private Rectangle _contentBounds;
    
    // Scrollbars
    private Slider _verticalScrollbar;
    private Slider _horizontalScrollbar;
    private bool _showVerticalScrollbar;
    private bool _showHorizontalScrollbar;
    private int _scrollbarWidth;
    
    // Scissor test state
    private RasterizerState _scissorRasterizer;
    
    // Input handling
    private MouseState _previousMouseState;
    
    // Configuration
    public bool AutoShowScrollbars { get; set; } = true;
    public bool AlwaysShowVerticalScrollbar { get; set; } = false;
    public bool AlwaysShowHorizontalScrollbar { get; set; } = false;
    public float ScrollSpeed { get; set; } = 20f;
    
    // Events
    public event Action<Vector2> OnScrollChanged;

    public Vector2 ScrollOffset
    {
        get => _scrollOffset;
        set
        {
            Vector2 maxOffset = GetMaxScrollOffset();
            Vector2 newOffset = new Vector2(
                MathHelper.Clamp(value.X, 0, Math.Max(0, maxOffset.X)),
                MathHelper.Clamp(value.Y, 0, Math.Max(0, maxOffset.Y))
            );
            
            if (_scrollOffset != newOffset)
            {
                _scrollOffset = newOffset;
                UpdateScrollbars();
                OnScrollChanged?.Invoke(_scrollOffset);
            }
        }
    }

    public ScrollArea(Rectangle bounds, int scrollbarWidth = 16)
    {
        _bounds = bounds;
        _children = new List<UIElement>();
        _scrollOffset = Vector2.Zero;
        _scrollbarWidth = scrollbarWidth;
        _contentBounds = Rectangle.Empty;
        
        InitializeScrollbars();
        InitializeScissorState();
        
        _previousMouseState = Mouse.GetState();
    }

    private void InitializeScrollbars()
    {
        // Create vertical scrollbar
        Rectangle verticalBounds = new Rectangle(
            _bounds.Right - _scrollbarWidth,
            _bounds.Y,
            _scrollbarWidth,
            _bounds.Height
        );
        
        _verticalScrollbar = new Slider(
            verticalBounds,
            minValue: 0f,
            maxValue: 1f,
            initialValue: 0f,
            isHorizontal: false,
            trackColor: Color.DarkGray,
            fillColor: Color.Gray,
            handleColor: Color.LightGray,
            handleHoverColor: Color.White,
            handlePressedColor: Color.Gray,
            trackHeight: _scrollbarWidth - 4,
            handleSize: _scrollbarWidth - 2
        );
        
        _verticalScrollbar.OnValueChanged += OnVerticalScrollbarChanged;
        _verticalScrollbar.SetParent(this);
        _verticalScrollbar.LocalOrderOffset = -0.05f;
        
        // Create horizontal scrollbar
        Rectangle horizontalBounds = new Rectangle(
            _bounds.X,
            _bounds.Bottom - _scrollbarWidth,
            _bounds.Width,
            _scrollbarWidth
        );
        
        _horizontalScrollbar = new Slider(
            horizontalBounds,
            minValue: 0f,
            maxValue: 1f,
            initialValue: 0f,
            isHorizontal: true,
            trackColor: Color.DarkGray,
            fillColor: Color.Gray,
            handleColor: Color.LightGray,
            handleHoverColor: Color.White,
            handlePressedColor: Color.Gray,
            trackHeight: _scrollbarWidth - 4,
            handleSize: _scrollbarWidth - 2
        );
        
        _horizontalScrollbar.OnValueChanged += OnHorizontalScrollbarChanged;
        _horizontalScrollbar.SetParent(this);
        _horizontalScrollbar.LocalOrderOffset = -0.05f;
    }

    private void InitializeScissorState()
    {
        _scissorRasterizer = new RasterizerState
        {
            ScissorTestEnable = true,
            CullMode = CullMode.None,
            FillMode = FillMode.Solid
        };
    }

    public void AddChild(UIElement child)
    {
        if (child != null && !_children.Contains(child))
        {
            _children.Add(child);
            child.SetParent(this);
            RecalculateContentBounds();
            UpdateScrollbarVisibility();
        }
    }

    public void RemoveChild(UIElement child)
    {
        if (child != null && _children.Contains(child))
        {
            _children.Remove(child);
            child.SetParent(null);
            RecalculateContentBounds();
            UpdateScrollbarVisibility();
        }
    }

    public void ClearChildren()
    {
        foreach (var child in _children)
        {
            child.SetParent(null);
        }
        _children.Clear();
        RecalculateContentBounds();
        UpdateScrollbarVisibility();
    }

    private void RecalculateContentBounds()
    {
        if (_children.Count == 0)
        {
            _contentBounds = Rectangle.Empty;
            return;
        }

        int minX = int.MaxValue;
        int minY = int.MaxValue;
        int maxX = int.MinValue;
        int maxY = int.MinValue;

        foreach (var child in _children)
        {
            Rectangle childBounds = child.GetBoundingBox();
            if (childBounds.IsEmpty) continue;

            minX = Math.Min(minX, childBounds.X);
            minY = Math.Min(minY, childBounds.Y);
            maxX = Math.Max(maxX, childBounds.Right);
            maxY = Math.Max(maxY, childBounds.Bottom);
        }

        if (minX != int.MaxValue)
        {
            _contentBounds = new Rectangle(minX, minY, maxX - minX, maxY - minY);
        }
        else
        {
            _contentBounds = Rectangle.Empty;
        }
    }

    private void UpdateScrollbarVisibility()
    {
        Rectangle viewportBounds = GetViewportBounds();
        
        _showVerticalScrollbar = AlwaysShowVerticalScrollbar || 
            (AutoShowScrollbars && _contentBounds.Height > viewportBounds.Height);
        
        _showHorizontalScrollbar = AlwaysShowHorizontalScrollbar || 
            (AutoShowScrollbars && _contentBounds.Width > viewportBounds.Width);

        // Update scrollbar bounds if both are visible
        if (_showVerticalScrollbar && _showHorizontalScrollbar)
        {
            // Adjust vertical scrollbar to not overlap with horizontal
            Rectangle verticalBounds = new Rectangle(
                _bounds.Right - _scrollbarWidth,
                _bounds.Y,
                _scrollbarWidth,
                _bounds.Height - _scrollbarWidth
            );
            _verticalScrollbar.SetBounds(verticalBounds);
            
            // Adjust horizontal scrollbar to not overlap with vertical
            Rectangle horizontalBounds = new Rectangle(
                _bounds.X,
                _bounds.Bottom - _scrollbarWidth,
                _bounds.Width - _scrollbarWidth,
                _scrollbarWidth
            );
            _horizontalScrollbar.SetBounds(horizontalBounds);
        }
        else
        {
            // Reset to full bounds
            _verticalScrollbar.SetBounds(new Rectangle(
                _bounds.Right - _scrollbarWidth,
                _bounds.Y,
                _scrollbarWidth,
                _bounds.Height
            ));
            
            _horizontalScrollbar.SetBounds(new Rectangle(
                _bounds.X,
                _bounds.Bottom - _scrollbarWidth,
                _bounds.Width,
                _scrollbarWidth
            ));
        }

        UpdateScrollbars();
    }

    private Rectangle GetViewportBounds()
    {
        int width = _bounds.Width;
        int height = _bounds.Height;
        
        if (_showVerticalScrollbar) width -= _scrollbarWidth;
        if (_showHorizontalScrollbar) height -= _scrollbarWidth;
        
        return new Rectangle(_bounds.X, _bounds.Y, width, height);
    }

    private Vector2 GetMaxScrollOffset()
    {
        Rectangle viewportBounds = GetViewportBounds();
        
        float maxX = Math.Max(0, _contentBounds.Width - viewportBounds.Width);
        float maxY = Math.Max(0, _contentBounds.Height - viewportBounds.Height);
        
        return new Vector2(maxX, maxY);
    }

    private void UpdateScrollbars()
    {
        Vector2 maxOffset = GetMaxScrollOffset();
        
        if (_showVerticalScrollbar && maxOffset.Y > 0)
        {
            // Invert the percentage for vertical scrollbar so top = 0% scroll, bottom = 100% scroll
            float percentage = 1.0f - (_scrollOffset.Y / maxOffset.Y);
            _verticalScrollbar.SetPercentage(percentage);
        }
        
        if (_showHorizontalScrollbar && maxOffset.X > 0)
        {
            float percentage = _scrollOffset.X / maxOffset.X;
            _horizontalScrollbar.SetPercentage(percentage);
        }
    }

    private void OnVerticalScrollbarChanged(float value)
    {
        Vector2 maxOffset = GetMaxScrollOffset();
        // Invert the value for vertical scrollbar so slider top = no scroll, slider bottom = max scroll
        ScrollOffset = new Vector2(_scrollOffset.X, (1.0f - value) * maxOffset.Y);
    }

    private void OnHorizontalScrollbarChanged(float value)
    {
        Vector2 maxOffset = GetMaxScrollOffset();
        ScrollOffset = new Vector2(value * maxOffset.X, _scrollOffset.Y);
    }

    public override void Update(float deltaTime)
    {
        // Handle mouse wheel scrolling
        MouseState currentMouseState = Mouse.GetState();
        Rectangle viewportBounds = GetViewportBounds();
        
        if (viewportBounds.Contains(currentMouseState.Position))
        {
            int wheelDelta = currentMouseState.ScrollWheelValue - _previousMouseState.ScrollWheelValue;
            if (wheelDelta != 0)
            {
                float scrollAmount = wheelDelta > 0 ? -ScrollSpeed : ScrollSpeed;
                ScrollOffset = new Vector2(_scrollOffset.X, _scrollOffset.Y + scrollAmount);
            }
        }
        
        _previousMouseState = currentMouseState;
        
        // Update scrollbars
        if (_showVerticalScrollbar)
        {
            _verticalScrollbar.Update(deltaTime);
        }
        
        if (_showHorizontalScrollbar)
        {
            _horizontalScrollbar.Update(deltaTime);
        }
        
        // Update children (with original bounds for proper input handling)
        foreach (var child in _children)
        {
            child.Update(deltaTime);
        }
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        Rectangle viewportBounds = GetViewportBounds();
        
        // Store original state
        Rectangle originalScissor = spriteBatch.GraphicsDevice.ScissorRectangle;
        RasterizerState originalRaster = spriteBatch.GraphicsDevice.RasterizerState;
        
        // End the current batch
        spriteBatch.End();
        
        try
        {
            // Set scissor rectangle for clipping
            spriteBatch.GraphicsDevice.ScissorRectangle = viewportBounds;
            spriteBatch.GraphicsDevice.RasterizerState = _scissorRasterizer;
            
            // Begin new batch with scissor test
            spriteBatch.Begin(
                SpriteSortMode.FrontToBack,
                BlendState.AlphaBlend,
                SamplerState.PointClamp,
                DepthStencilState.None,
                _scissorRasterizer
            );
            
            // Draw children with scroll offset applied
            foreach (var child in _children)
            {
                if (child.IsVisible())
                {
                    // Temporarily modify child position for rendering
                    Rectangle originalBounds = child.GetBoundingBox();
                    Rectangle scrolledBounds = new Rectangle(
                        originalBounds.X + viewportBounds.X - (int)_scrollOffset.X,
                        originalBounds.Y + viewportBounds.Y - (int)_scrollOffset.Y,
                        originalBounds.Width,
                        originalBounds.Height
                    );
                    
                    // Set scrolled bounds temporarily for drawing
                    child.SetBounds(scrolledBounds);
                    child.Draw(spriteBatch);
                    // Restore original bounds immediately after drawing
                    child.SetBounds(originalBounds);
                }
            }
        }
        finally
        {
            // Always restore state in finally block
            spriteBatch.End();
            
            // Restore original state
            spriteBatch.GraphicsDevice.RasterizerState = originalRaster;
            spriteBatch.GraphicsDevice.ScissorRectangle = originalScissor;
            
            // Begin batch again with original settings
            spriteBatch.Begin(
                SpriteSortMode.FrontToBack,
                BlendState.AlphaBlend,
                SamplerState.PointClamp,
                DepthStencilState.None,
                originalRaster
            );
        }
        
        // Draw scrollbars outside the clipped area
        if (_showVerticalScrollbar)
        {
            _verticalScrollbar.Draw(spriteBatch);
        }
        
        if (_showHorizontalScrollbar)
        {
            _horizontalScrollbar.Draw(spriteBatch);
        }
    }

    public override Rectangle GetBoundingBox()
    {
        return _bounds;
    }

    public override void SetBounds(Rectangle bounds)
    {
        _bounds = bounds;
        UpdateScrollbarVisibility();
    }

    public void ScrollToTop()
    {
        ScrollOffset = new Vector2(_scrollOffset.X, 0);
    }

    public void ScrollToBottom()
    {
        Vector2 maxOffset = GetMaxScrollOffset();
        ScrollOffset = new Vector2(_scrollOffset.X, maxOffset.Y);
    }

    public void ScrollToLeft()
    {
        ScrollOffset = new Vector2(0, _scrollOffset.Y);
    }

    public void ScrollToRight()
    {
        Vector2 maxOffset = GetMaxScrollOffset();
        ScrollOffset = new Vector2(maxOffset.X, _scrollOffset.Y);
    }

    public void Dispose()
    {
        _verticalScrollbar?.Dispose();
        _horizontalScrollbar?.Dispose();
        _scissorRasterizer?.Dispose();
    }
}