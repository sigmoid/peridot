namespace Peridot.UI;

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Peridot;

public class ImageButton : UIElement
{
    private Rectangle _bounds;
    private Texture2D _texture;
    private Rectangle? _sourceRectangle;
    private Color _tintColor;
    private Color _hoverTintColor;
    private Color _pressedTintColor;
    private Color _disabledTintColor;
    private Action _onClick;
    private Texture2D _pixel;
    
    // State tracking
    private bool _isHovered = false;
    private bool _isPressed = false;
    private bool _wasMousePressed = false;
    private bool _isEnabled = true;
    
    // Optional background
    private bool _drawBackground = false;
    private Color _backgroundColor = Color.Transparent;
    private Color _hoverBackgroundColor = Color.LightGray;
    private Color _pressedBackgroundColor = Color.Gray;
    
    // Image scaling and positioning
    private bool _scaleToFit = true;
    private Vector2 _imageScale = Vector2.One;
    private Vector2 _imageOffset = Vector2.Zero;

    public bool IsEnabled 
    { 
        get => _isEnabled; 
        set => _isEnabled = value; 
    }

    public Color TintColor
    {
        get => _tintColor;
        set => _tintColor = value;
    }

    public bool DrawBackground
    {
        get => _drawBackground;
        set => _drawBackground = value;
    }

    public Color BackgroundColor
    {
        get => _backgroundColor;
        set => _backgroundColor = value;
    }

    public bool ScaleToFit
    {
        get => _scaleToFit;
        set => _scaleToFit = value;
    }

    public ImageButton(Rectangle bounds, Texture2D texture, Action onClick, 
        Rectangle? sourceRectangle = null,
        Color? tintColor = null, 
        Color? hoverTintColor = null,
        Color? pressedTintColor = null,
        Color? disabledTintColor = null,
        bool drawBackground = false,
        Color? backgroundColor = null,
        Color? hoverBackgroundColor = null,
        Color? pressedBackgroundColor = null)
    {
        _bounds = bounds;
        _texture = texture ?? throw new ArgumentNullException(nameof(texture));
        _sourceRectangle = sourceRectangle;
        _onClick = onClick;
        
        // Set default colors
        _tintColor = tintColor ?? Color.White;
        _hoverTintColor = hoverTintColor ?? Color.Lerp(_tintColor, Color.White, 0.2f);
        _pressedTintColor = pressedTintColor ?? Color.Lerp(_tintColor, Color.Black, 0.2f);
        _disabledTintColor = disabledTintColor ?? Color.Lerp(_tintColor, Color.Gray, 0.5f);
        
        _drawBackground = drawBackground;
        _backgroundColor = backgroundColor ?? Color.Transparent;
        _hoverBackgroundColor = hoverBackgroundColor ?? Color.LightGray;
        _pressedBackgroundColor = pressedBackgroundColor ?? Color.Gray;

        // Create pixel texture for background drawing
        _pixel = new Texture2D(Core.GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
        
        CalculateImageScaling();
    }

    private void CalculateImageScaling()
    {
        if (!_scaleToFit || _texture == null) 
        {
            _imageScale = Vector2.One;
            _imageOffset = Vector2.Zero;
            return;
        }

        Rectangle sourceRect = _sourceRectangle ?? new Rectangle(0, 0, _texture.Width, _texture.Height);
        
        float scaleX = (float)_bounds.Width / sourceRect.Width;
        float scaleY = (float)_bounds.Height / sourceRect.Height;
        
        // Use the smaller scale to maintain aspect ratio while fitting within bounds
        float scale = Math.Min(scaleX, scaleY);
        _imageScale = new Vector2(scale);
        
        // Calculate offset to center the image
        float scaledWidth = sourceRect.Width * scale;
        float scaledHeight = sourceRect.Height * scale;
        _imageOffset = new Vector2(
            (_bounds.Width - scaledWidth) / 2f,
            (_bounds.Height - scaledHeight) / 2f
        );
    }

    public override void Update(float deltaTime)
    {
        if (!_isEnabled)
        {
            _isHovered = false;
            _isPressed = false;
            return;
        }

        var mouseState = Mouse.GetState();
        var mousePosition = new Vector2(mouseState.X, mouseState.Y);

        _isHovered = _bounds.Contains(mousePosition);
        
        bool isMousePressed = mouseState.LeftButton == ButtonState.Pressed;
        bool isMouseClick = isMousePressed && !_wasMousePressed;
        bool isMouseRelease = !isMousePressed && _wasMousePressed;
        
        _isPressed = _isHovered && isMousePressed;
        _wasMousePressed = isMousePressed;

        // Trigger click on mouse release while still hovered
        if (_isHovered && isMouseRelease)
        {
            OnClick();
        }
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        // Draw background if enabled
        if (_drawBackground)
        {
            Color bgColor = _backgroundColor;
            if (_isEnabled)
            {
                if (_isPressed)
                    bgColor = _pressedBackgroundColor;
                else if (_isHovered)
                    bgColor = _hoverBackgroundColor;
            }
            
            spriteBatch.Draw(_pixel, _bounds, null, bgColor, 0, Vector2.Zero, SpriteEffects.None, 0f);
        }

        // Draw image
        if (_texture != null)
        {
            Color imageTint = _tintColor;
            if (!_isEnabled)
            {
                imageTint = _disabledTintColor;
            }
            else if (_isPressed)
            {
                imageTint = _pressedTintColor;
            }
            else if (_isHovered)
            {
                imageTint = _hoverTintColor;
            }

            Vector2 imagePosition = new Vector2(_bounds.X, _bounds.Y) + _imageOffset;
            
            if (_scaleToFit)
            {
                spriteBatch.Draw(
                    _texture,
                    imagePosition,
                    _sourceRectangle,
                    imageTint,
                    0f,
                    Vector2.Zero,
                    _imageScale,
                    SpriteEffects.None,
                    0f
                );
            }
            else
            {
                Rectangle destRect = _sourceRectangle.HasValue 
                    ? new Rectangle((int)imagePosition.X, (int)imagePosition.Y, _sourceRectangle.Value.Width, _sourceRectangle.Value.Height)
                    : new Rectangle((int)imagePosition.X, (int)imagePosition.Y, _texture.Width, _texture.Height);
                    
                spriteBatch.Draw(
                    _texture,
                    destRect,
                    _sourceRectangle,
                    imageTint,
                    0f,
                    Vector2.Zero,
                    SpriteEffects.None,
                    0f
                );
            }
        }
    }

    public void OnClick()
    {
        if (_isEnabled)
        {
            _onClick?.Invoke();
        }
    }

    public override Rectangle GetBoundingBox()
    {
        return _bounds;
    }

    public override void SetBounds(Rectangle bounds)
    {
        _bounds = bounds;
        CalculateImageScaling();
    }

    public void SetTexture(Texture2D texture, Rectangle? sourceRectangle = null)
    {
        _texture = texture;
        _sourceRectangle = sourceRectangle;
        CalculateImageScaling();
    }

    public void SetImageOffset(Vector2 offset)
    {
        _imageOffset = offset;
    }

    public void SetImageScale(Vector2 scale)
    {
        _imageScale = scale;
        _scaleToFit = false; // Manual scaling overrides auto-fit
    }

    public void Dispose()
    {
        _pixel?.Dispose();
        // Note: Don't dispose _texture as it might be shared
    }
}