namespace Peridot.UI;

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Peridot;

public class Slider : UIElement
{
    private Rectangle _bounds;
    private float _value;
    private float _minValue;
    private float _maxValue;
    private float _step;
    private bool _isHorizontal;
    
    // Visual styling
    private Color _trackColor;
    private Color _fillColor;
    private Color _handleColor;
    private Color _handleHoverColor;
    private Color _handlePressedColor;
    private int _trackHeight;
    private int _handleSize;
    private int _handleBorderSize;
    
    // State tracking
    private bool _isDragging;
    private bool _isHovered;
    private bool _wasMousePressed;
    private Vector2 _dragOffset;
    private Texture2D _pixel;
    
    // Events
    public event Action<float> OnValueChanged;
    
    public float Value 
    { 
        get => _value; 
        set 
        {
            float newValue = MathHelper.Clamp(value, _minValue, _maxValue);
            if (_step > 0)
            {
                newValue = (float)(Math.Round((newValue - _minValue) / _step) * _step + _minValue);
            }
            
            if (Math.Abs(_value - newValue) > float.Epsilon)
            {
                _value = newValue;
                OnValueChanged?.Invoke(_value);
            }
        }
    }
    
    public float MinValue 
    { 
        get => _minValue; 
        set 
        { 
            _minValue = value; 
            Value = _value; // Revalidate current value
        } 
    }
    
    public float MaxValue 
    { 
        get => _maxValue; 
        set 
        { 
            _maxValue = value; 
            Value = _value; // Revalidate current value
        } 
    }
    
    public float Step 
    { 
        get => _step; 
        set => _step = Math.Max(0, value); 
    }
    
    public bool IsHorizontal 
    { 
        get => _isHorizontal; 
        set => _isHorizontal = value; 
    }

    public Slider(Rectangle bounds, float minValue = 0f, float maxValue = 1f, float initialValue = 0f, 
        float step = 0f, bool isHorizontal = true,
        Color? trackColor = null, Color? fillColor = null, Color? handleColor = null,
        Color? handleHoverColor = null, Color? handlePressedColor = null,
        int trackHeight = 6, int handleSize = 20, int handleBorderSize = 2)
    {
        _bounds = bounds;
        _minValue = minValue;
        _maxValue = maxValue;
        _step = step;
        _isHorizontal = isHorizontal;
        _trackHeight = trackHeight;
        _handleSize = handleSize;
        _handleBorderSize = handleBorderSize;
        
        // Set default colors
        _trackColor = trackColor ?? Color.LightGray;
        _fillColor = fillColor ?? Color.CornflowerBlue;
        _handleColor = handleColor ?? Color.White;
        _handleHoverColor = handleHoverColor ?? Color.LightGray;
        _handlePressedColor = handlePressedColor ?? Color.Gray;
        
        // Initialize state
        _isDragging = false;
        _isHovered = false;
        _wasMousePressed = false;
        
        // Create pixel texture for drawing
        _pixel = new Texture2D(Core.GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
        
        // Set initial value (this will trigger validation)
        Value = initialValue;
    }

    public override void Update(float deltaTime)
    {
        var mouseState = Mouse.GetState();
        var mousePosition = new Vector2(mouseState.X, mouseState.Y);
        
        bool isMousePressed = mouseState.LeftButton == ButtonState.Pressed;
        bool isMouseClick = isMousePressed && !_wasMousePressed;
        bool isMouseRelease = !isMousePressed && _wasMousePressed;
        
        _wasMousePressed = isMousePressed;
        
        // Check if mouse is over the slider
        bool isOverSlider = _bounds.Contains(mousePosition);
        Rectangle handleBounds = GetHandleBounds();
        bool isOverHandle = handleBounds.Contains(mousePosition);
        
        _isHovered = isOverSlider || isOverHandle;
        
        // Handle mouse interactions
        if (isMouseClick && (isOverSlider || isOverHandle))
        {
            _isDragging = true;
            
            if (isOverHandle)
            {
                // Calculate offset for smooth dragging
                _dragOffset = mousePosition - new Vector2(
                    handleBounds.X + handleBounds.Width / 2f,
                    handleBounds.Y + handleBounds.Height / 2f
                );
            }
            else
            {
                // Clicked on track, jump to that position
                _dragOffset = Vector2.Zero;
                UpdateValueFromMousePosition(mousePosition);
            }
        }
        else if (isMouseRelease)
        {
            _isDragging = false;
            _dragOffset = Vector2.Zero;
        }
        
        // Update value while dragging
        if (_isDragging && isMousePressed)
        {
            Vector2 adjustedMousePosition = mousePosition - _dragOffset;
            UpdateValueFromMousePosition(adjustedMousePosition);
        }
    }
    
    private void UpdateValueFromMousePosition(Vector2 mousePosition)
    {
        Rectangle trackBounds = GetTrackBounds();
        
        float percentage;
        if (_isHorizontal)
        {
            float relativeX = mousePosition.X - trackBounds.X;
            percentage = MathHelper.Clamp(relativeX / trackBounds.Width, 0f, 1f);
        }
        else
        {
            float relativeY = mousePosition.Y - trackBounds.Y;
            percentage = 1f - MathHelper.Clamp(relativeY / trackBounds.Height, 0f, 1f); 
        }
        
        float newValue = _minValue + percentage * (_maxValue - _minValue);
        Value = newValue; 
    }
    
    private Rectangle GetTrackBounds()
    {
        if (_isHorizontal)
        {
            int trackY = _bounds.Y + (_bounds.Height - _trackHeight) / 2;
            return new Rectangle(_bounds.X, trackY, _bounds.Width, _trackHeight);
        }
        else
        {
            int trackX = _bounds.X + (_bounds.Width - _trackHeight) / 2;
            return new Rectangle(trackX, _bounds.Y, _trackHeight, _bounds.Height);
        }
    }
    
    private Rectangle GetHandleBounds()
    {
        float percentage = (_value - _minValue) / (_maxValue - _minValue);
        
        if (_isHorizontal)
        {
            int handleX = _bounds.X + (int)(percentage * (_bounds.Width - _handleSize));
            int handleY = _bounds.Y + (_bounds.Height - _handleSize) / 2;
            return new Rectangle(handleX, handleY, _handleSize, _handleSize);
        }
        else
        {
            int handleX = _bounds.X + (_bounds.Width - _handleSize) / 2;
            int handleY = _bounds.Y + (int)((1f - percentage) * (_bounds.Height - _handleSize)); 
            return new Rectangle(handleX, handleY, _handleSize, _handleSize);
        }
    }
    
    private Rectangle GetFillBounds()
    {
        Rectangle trackBounds = GetTrackBounds();
        float percentage = (_value - _minValue) / (_maxValue - _minValue);
        
        if (_isHorizontal)
        {
            int fillWidth = (int)(trackBounds.Width * percentage);
            return new Rectangle(trackBounds.X, trackBounds.Y, fillWidth, trackBounds.Height);
        }
        else
        {
            int fillHeight = (int)(trackBounds.Height * percentage);
            int fillY = trackBounds.Y + trackBounds.Height - fillHeight;
            return new Rectangle(trackBounds.X, fillY, trackBounds.Width, fillHeight);
        }
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        Rectangle trackBounds = GetTrackBounds();
        Rectangle fillBounds = GetFillBounds();
        Rectangle handleBounds = GetHandleBounds();
        
        spriteBatch.Draw(_pixel, trackBounds, null, _trackColor, 0, Vector2.Zero, SpriteEffects.None, GetActualOrder());
        
        if (fillBounds.Width > 0 && fillBounds.Height > 0)
        {
            spriteBatch.Draw(_pixel, fillBounds, null, _fillColor, 0, Vector2.Zero, SpriteEffects.None, GetActualOrder() + 0.01f);
        }
        
        Color handleDrawColor = _handleColor;
        if (_isDragging)
            handleDrawColor = _handlePressedColor;
        else if (_isHovered)
            handleDrawColor = _handleHoverColor;
            
        if (_handleBorderSize > 0)
        {
            Rectangle borderBounds = new Rectangle(
                handleBounds.X - _handleBorderSize,
                handleBounds.Y - _handleBorderSize,
                handleBounds.Width + _handleBorderSize * 2,
                handleBounds.Height + _handleBorderSize * 2
            );
            spriteBatch.Draw(_pixel, borderBounds, null, Color.Black, 0, Vector2.Zero, SpriteEffects.None, GetActualOrder() + 0.02f);
        }
        
        spriteBatch.Draw(_pixel, handleBounds, null, handleDrawColor, 0, Vector2.Zero, SpriteEffects.None, GetActualOrder() + 0.03f);
    }

    public override Rectangle GetBoundingBox()
    {
        return _bounds;
    }

    public override void SetBounds(Rectangle bounds)
    {
        _bounds = bounds;
    }
    
    // Additional utility methods
    public void SetRange(float minValue, float maxValue)
    {
        _minValue = minValue;
        _maxValue = maxValue;
        Value = _value; // Revalidate current value
    }
    
    public float GetPercentage()
    {
        return (_value - _minValue) / (_maxValue - _minValue);
    }
    
    public void SetPercentage(float percentage)
    {
        percentage = MathHelper.Clamp(percentage, 0f, 1f);
        Value = _minValue + percentage * (_maxValue - _minValue);
    }
    
    public void Dispose()
    {
        _pixel?.Dispose();
    }
}