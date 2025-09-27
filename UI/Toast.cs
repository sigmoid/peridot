namespace Peridot.UI;

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Peridot;

/// <summary>
/// A Toast notification that displays temporary messages to the user.
/// Toasts automatically fade in, display for a duration, then fade out and disappear.
/// 
/// Example usage:
/// 
/// // Simple toast
/// var toast = Toast.Show("Message saved!", font, screenBounds);
/// 
/// // Custom styled toast
/// var toast = Toast.Show("Error occurred!", font, screenBounds, 
///     ToastType.Error, duration: 3.0f, position: ToastPosition.TopCenter);
/// 
/// // Add to UI system
/// uiSystem.AddElement(toast);
/// </summary>
public class Toast : UIElement
{
    public enum ToastType
    {
        Info,
        Success,
        Warning,
        Error
    }

    public enum ToastPosition
    {
        TopLeft,
        TopCenter,
        TopRight,
        MiddleLeft,
        MiddleCenter,
        MiddleRight,
        BottomLeft,
        BottomCenter,
        BottomRight
    }

    public enum ToastState
    {
        FadeIn,
        Display,
        FadeOut,
        Finished
    }

    private Rectangle _bounds;
    private Rectangle _screenBounds;
    private string _message;
    private SpriteFont _font;
    private Texture2D _pixel;
    
    // Styling
    private Color _backgroundColor;
    private Color _textColor;
    private Color _borderColor;
    private int _padding;
    private int _borderWidth;
    private int _cornerRadius; // For future rounded corners
    
    // Animation and timing
    private float _duration;
    private float _fadeInDuration;
    private float _fadeOutDuration;
    private float _currentTime;
    private ToastState _state;
    private float _alpha;
    
    // Positioning
    private ToastPosition _position;
    private Vector2 _offset;
    
    // Events
    public event Action OnToastFinished;

    private Toast(string message, SpriteFont font, Rectangle screenBounds, 
                 ToastType type, float duration, ToastPosition position, Vector2? offset)
    {
        _message = message ?? string.Empty;
        _font = font ?? throw new ArgumentNullException(nameof(font));
        _screenBounds = screenBounds;
        _duration = duration;
        _position = position;
        _offset = offset ?? Vector2.Zero;
        
        _fadeInDuration = 0.3f;
        _fadeOutDuration = 0.5f;
        _padding = 16;
        _borderWidth = 2;
        _cornerRadius = 4;
        
        _currentTime = 0f;
        _state = ToastState.FadeIn;
        _alpha = 0f;
        
        // Set colors based on toast type
        SetColorsForType(type);
        
        // Create pixel texture
        _pixel = new Texture2D(Core.GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
        
        // Calculate bounds
        CalculateBounds();
    }

    private void SetColorsForType(ToastType type)
    {
        switch (type)
        {
            case ToastType.Info:
                _backgroundColor = Color.CornflowerBlue;
                _textColor = Color.White;
                _borderColor = Color.DarkBlue;
                break;
            case ToastType.Success:
                _backgroundColor = Color.Green;
                _textColor = Color.White;
                _borderColor = Color.DarkGreen;
                break;
            case ToastType.Warning:
                _backgroundColor = Color.Orange;
                _textColor = Color.Black;
                _borderColor = Color.DarkOrange;
                break;
            case ToastType.Error:
                _backgroundColor = Color.Red;
                _textColor = Color.White;
                _borderColor = Color.DarkRed;
                break;
        }
    }

    private void CalculateBounds()
    {
        // Measure text to determine toast size
        var textSize = _font.MeasureString(_message);
        int toastWidth = (int)textSize.X + (_padding * 2) + (_borderWidth * 2);
        int toastHeight = (int)textSize.Y + (_padding * 2) + (_borderWidth * 2);
        
        // Calculate position based on ToastPosition
        int x, y;
        
        switch (_position)
        {
            case ToastPosition.TopLeft:
                x = _screenBounds.X + 20;
                y = _screenBounds.Y + 20;
                break;
            case ToastPosition.TopCenter:
                x = _screenBounds.X + (_screenBounds.Width - toastWidth) / 2;
                y = _screenBounds.Y + 20;
                break;
            case ToastPosition.TopRight:
                x = _screenBounds.X + _screenBounds.Width - toastWidth - 20;
                y = _screenBounds.Y + 20;
                break;
            case ToastPosition.MiddleLeft:
                x = _screenBounds.X + 20;
                y = _screenBounds.Y + (_screenBounds.Height - toastHeight) / 2;
                break;
            case ToastPosition.MiddleCenter:
                x = _screenBounds.X + (_screenBounds.Width - toastWidth) / 2;
                y = _screenBounds.Y + (_screenBounds.Height - toastHeight) / 2;
                break;
            case ToastPosition.MiddleRight:
                x = _screenBounds.X + _screenBounds.Width - toastWidth - 20;
                y = _screenBounds.Y + (_screenBounds.Height - toastHeight) / 2;
                break;
            case ToastPosition.BottomLeft:
                x = _screenBounds.X + 20;
                y = _screenBounds.Y + _screenBounds.Height - toastHeight - 20;
                break;
            case ToastPosition.BottomCenter:
                x = _screenBounds.X + (_screenBounds.Width - toastWidth) / 2;
                y = _screenBounds.Y + _screenBounds.Height - toastHeight - 20;
                break;
            case ToastPosition.BottomRight:
            default:
                x = _screenBounds.X + _screenBounds.Width - toastWidth - 20;
                y = _screenBounds.Y + _screenBounds.Height - toastHeight - 20;
                break;
        }
        
        // Apply offset
        x += (int)_offset.X;
        y += (int)_offset.Y;
        
        _bounds = new Rectangle(x, y, toastWidth, toastHeight);
    }

    public override void Update(float deltaTime)
    {
        if (!IsVisible()) return;

        _currentTime += deltaTime;

        switch (_state)
        {
            case ToastState.FadeIn:
                _alpha = Math.Min(1f, _currentTime / _fadeInDuration);
                if (_currentTime >= _fadeInDuration)
                {
                    _state = ToastState.Display;
                    _currentTime = 0f;
                    _alpha = 1f;
                }
                break;

            case ToastState.Display:
                if (_currentTime >= _duration)
                {
                    _state = ToastState.FadeOut;
                    _currentTime = 0f;
                }
                break;

            case ToastState.FadeOut:
                _alpha = Math.Max(0f, 1f - (_currentTime / _fadeOutDuration));
                if (_currentTime >= _fadeOutDuration)
                {
                    _state = ToastState.Finished;
                    _alpha = 0f;
                    SetVisibility(false);
                    OnToastFinished?.Invoke();
                }
                break;

            case ToastState.Finished:
                // Toast is finished, can be removed
                break;
        }
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        if (!IsVisible() || _alpha <= 0f) return;

        // Apply alpha to colors
        Color backgroundColorWithAlpha = _backgroundColor * _alpha;
        Color textColorWithAlpha = _textColor * _alpha;
        Color borderColorWithAlpha = _borderColor * _alpha;

        // Draw border
        spriteBatch.Draw(_pixel, _bounds, null, borderColorWithAlpha, 0, Vector2.Zero, SpriteEffects.None, GetActualOrder());

        // Draw background
        var backgroundBounds = new Rectangle(
            _bounds.X + _borderWidth,
            _bounds.Y + _borderWidth,
            _bounds.Width - (_borderWidth * 2),
            _bounds.Height - (_borderWidth * 2)
        );
        spriteBatch.Draw(_pixel, backgroundBounds, null, backgroundColorWithAlpha, 0, Vector2.Zero, SpriteEffects.None, GetActualOrder() + 0.01f);

        // Draw text
        if (!string.IsNullOrEmpty(_message))
        {
            var textPosition = new Vector2(
                _bounds.X + _borderWidth + _padding,
                _bounds.Y + _borderWidth + _padding
            );

            spriteBatch.DrawString(_font, _message, textPosition, textColorWithAlpha, 0, Vector2.Zero, 1.0f, SpriteEffects.None, GetActualOrder() + 0.02f);
        }
    }

    public override Rectangle GetBoundingBox()
    {
        return _bounds;
    }

    public override void SetBounds(Rectangle bounds)
    {
        _bounds = bounds;
    }

    /// <summary>
    /// Gets the current state of the toast animation.
    /// </summary>
    public ToastState State => _state;

    /// <summary>
    /// Gets whether the toast has finished its lifecycle.
    /// </summary>
    public bool IsFinished => _state == ToastState.Finished;

    /// <summary>
    /// Immediately dismisses the toast by starting the fade-out animation.
    /// </summary>
    public void Dismiss()
    {
        if (_state != ToastState.Finished)
        {
            _state = ToastState.FadeOut;
            _currentTime = 0f;
        }
    }

    /// <summary>
    /// Immediately hides the toast without animation.
    /// </summary>
    public void Hide()
    {
        _state = ToastState.Finished;
        _alpha = 0f;
        SetVisibility(false);
        OnToastFinished?.Invoke();
    }

    /// <summary>
    /// Updates the screen bounds (useful when screen is resized).
    /// </summary>
    public void UpdateScreenBounds(Rectangle newScreenBounds)
    {
        _screenBounds = newScreenBounds;
        CalculateBounds();
    }

    public void Dispose()
    {
        _pixel?.Dispose();
    }

    // Factory methods for easy toast creation
    
    /// <summary>
    /// Shows a toast notification with default settings.
    /// </summary>
    public static Toast Show(string message, SpriteFont font, Rectangle screenBounds)
    {
        return Show(message, font, screenBounds, ToastType.Info, 3f, ToastPosition.BottomRight);
    }

    /// <summary>
    /// Shows a toast notification with specified type and duration.
    /// </summary>
    public static Toast Show(string message, SpriteFont font, Rectangle screenBounds, 
                           ToastType type, float duration = 3f)
    {
        return Show(message, font, screenBounds, type, duration, ToastPosition.BottomRight);
    }

    /// <summary>
    /// Shows a toast notification with full customization options.
    /// </summary>
    public static Toast Show(string message, SpriteFont font, Rectangle screenBounds,
                           ToastType type, float duration, ToastPosition position, Vector2? offset = null)
    {
        return new Toast(message, font, screenBounds, type, duration, position, offset);
    }

    /// <summary>
    /// Creates an info toast.
    /// </summary>
    public static Toast ShowInfo(string message, SpriteFont font, Rectangle screenBounds, 
                               float duration = 3f, ToastPosition position = ToastPosition.BottomRight)
    {
        return Show(message, font, screenBounds, ToastType.Info, duration, position);
    }

    /// <summary>
    /// Creates a success toast.
    /// </summary>
    public static Toast ShowSuccess(string message, SpriteFont font, Rectangle screenBounds, 
                                  float duration = 3f, ToastPosition position = ToastPosition.BottomRight)
    {
        return Show(message, font, screenBounds, ToastType.Success, duration, position);
    }

    /// <summary>
    /// Creates a warning toast.
    /// </summary>
    public static Toast ShowWarning(string message, SpriteFont font, Rectangle screenBounds, 
                                  float duration = 4f, ToastPosition position = ToastPosition.BottomRight)
    {
        return Show(message, font, screenBounds, ToastType.Warning, duration, position);
    }

    /// <summary>
    /// Creates an error toast.
    /// </summary>
    public static Toast ShowError(string message, SpriteFont font, Rectangle screenBounds, 
                                float duration = 5f, ToastPosition position = ToastPosition.BottomRight)
    {
        return Show(message, font, screenBounds, ToastType.Error, duration, position);
    }
}