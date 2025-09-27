namespace Peridot.UI;

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Peridot;

public class Modal : IUIElement
{
    private Rectangle _screenBounds;
    private Rectangle _modalBounds;
    private Rectangle _titleBarBounds;
    private Rectangle _contentBounds;
    private Rectangle _closeButtonBounds;
    
    private string _title;
    private SpriteFont _font;
    private SpriteFont _titleFont;
    
    private Color _overlayColor;
    private Color _modalBackgroundColor;
    private Color _titleBarColor;
    private Color _titleTextColor;
    private Color _borderColor;
    
    private Texture2D _pixel;
    private List<IUIElement> _contentElements;
    
    private Action _onClose;
    private bool _isClosable;
    private bool _closeOnOverlayClick;
    private bool _wasMousePressed;
    
    private Button _closeButton;
    private int _borderThickness;

    public Modal(Rectangle screenBounds, Rectangle modalBounds, string title, SpriteFont font, SpriteFont titleFont = null, 
                Color? overlayColor = null, Color? backgroundColor = null, Color? titleBarColor = null, 
                Action onClose = null, bool isClosable = true, bool closeOnOverlayClick = true)
    {
        _screenBounds = screenBounds;
        _modalBounds = modalBounds;
        _title = title ?? string.Empty;
        _font = font;
        _titleFont = titleFont ?? font;
        
        _overlayColor = overlayColor ?? Color.Transparent; // Semi-transparent overlay
        _modalBackgroundColor = backgroundColor ?? Color.White;
        _titleBarColor = titleBarColor ?? Color.DarkBlue;
        _titleTextColor = Color.White;
        _borderColor = Color.DarkGray;
        
        _onClose = onClose;
        _isClosable = isClosable;
        _closeOnOverlayClick = closeOnOverlayClick;
        _borderThickness = 2;
        
        _contentElements = new List<IUIElement>();
        
        // Create pixel texture for drawing
        _pixel = new Texture2D(Core.GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
        
        CalculateBounds();
        CreateCloseButton();
    }

    private void CalculateBounds()
    {
        // Title bar is at the top of the modal
        int titleBarHeight = (int)_titleFont.MeasureString("A").Y + 10; // Add padding
        _titleBarBounds = new Rectangle(_modalBounds.X, _modalBounds.Y, _modalBounds.Width, titleBarHeight);
        
        // Content area is below the title bar
        _contentBounds = new Rectangle(
            _modalBounds.X + _borderThickness, 
            _modalBounds.Y + titleBarHeight + _borderThickness, 
            _modalBounds.Width - (_borderThickness * 2), 
            _modalBounds.Height - titleBarHeight - (_borderThickness * 2)
        );
        
        // Close button in top-right corner of title bar
        int buttonSize = titleBarHeight - 4;
        _closeButtonBounds = new Rectangle(
            _titleBarBounds.Right - buttonSize - 2, 
            _titleBarBounds.Y + 2, 
            buttonSize, 
            buttonSize
        );
    }

    private void CreateCloseButton()
    {
        if (_isClosable)
        {
            _closeButton = new Button(
                _closeButtonBounds,
                "X",
                _font,
                Color.Transparent,
                Color.Red * 0.3f,
                Color.White,
                () => Close()
            );
        }
    }

    public void AddContentElement(IUIElement element)
    {
        _contentElements.Add(element);
        
        // Adjust element position to be relative to content area if needed
        var elementBounds = element.GetBoundingBox();
        if (elementBounds.X < _contentBounds.X || elementBounds.Y < _contentBounds.Y)
        {
            // If element seems to be positioned absolutely, make it relative to content area
            var newBounds = new Rectangle(
                _contentBounds.X + elementBounds.X,
                _contentBounds.Y + elementBounds.Y,
                elementBounds.Width,
                elementBounds.Height
            );
            element.SetBounds(newBounds);
        }
    }

    public void RemoveContentElement(IUIElement element)
    {
        _contentElements.Remove(element);
    }

    public void ClearContentElements()
    {
        _contentElements.Clear();
    }

    public IReadOnlyList<IUIElement> ContentElements => _contentElements;

    public override void Update(float deltaTime)
    {
        if (!IsVisible()) return;

        var mouseState = Mouse.GetState();
        var mousePosition = new Vector2(mouseState.X, mouseState.Y);
        
        bool isMousePressed = mouseState.LeftButton == ButtonState.Pressed;
        bool isMouseClick = isMousePressed && !_wasMousePressed;
        _wasMousePressed = isMousePressed;

        // Handle overlay click to close
        if (_closeOnOverlayClick && isMouseClick)
        {
            // Check if click is outside the modal bounds (on the overlay)
            if (!_modalBounds.Contains(mousePosition))
            {
                Close();
                return;
            }
        }

        // Update close button if closable
        if (_isClosable && _closeButton != null)
        {
            _closeButton.Update(deltaTime);
        }

        // Update content elements
        foreach (var element in _contentElements)
        {
            if (element.IsVisible())
            {
                element.Update(deltaTime);
            }
        }
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        if (!IsVisible()) return;

        // Draw overlay background
        spriteBatch.Draw(_pixel, _screenBounds, _overlayColor);
        
        // Draw modal border
        spriteBatch.Draw(_pixel, new Rectangle(_modalBounds.X - _borderThickness, _modalBounds.Y - _borderThickness, 
                                             _modalBounds.Width + (_borderThickness * 2), _modalBounds.Height + (_borderThickness * 2)), _borderColor);
        
        // Draw modal background
        spriteBatch.Draw(_pixel, _modalBounds, _modalBackgroundColor);
        
        // Draw title bar
        spriteBatch.Draw(_pixel, _titleBarBounds, _titleBarColor);
        
        // Draw title text
        if (!string.IsNullOrEmpty(_title))
        {
            var titleSize = _titleFont.MeasureString(_title);
            var titlePosition = new Vector2(
                _titleBarBounds.X + 10, // Left padding
                _titleBarBounds.Y + (_titleBarBounds.Height - titleSize.Y) / 2 // Center vertically
            );
            spriteBatch.DrawString(_titleFont, _title, titlePosition, _titleTextColor);
        }
        
        // Draw close button if closable
        if (_isClosable && _closeButton != null)
        {
            _closeButton.Draw(spriteBatch);
        }
        
        // Draw content elements
        foreach (var element in _contentElements)
        {
            if (element.IsVisible())
            {
                element.Draw(spriteBatch);
            }
        }
    }

    public void Show()
    {
        SetVisibility(true);
    }

    public void Close()
    {
        SetVisibility(false);
        _onClose?.Invoke();
    }

    public void SetTitle(string title)
    {
        _title = title ?? string.Empty;
    }

    public void SetOnCloseCallback(Action onClose)
    {
        _onClose = onClose;
    }

    public void SetClosable(bool closable)
    {
        _isClosable = closable;
        if (!closable)
        {
            _closeButton = null;
        }
        else if (_closeButton == null)
        {
            CreateCloseButton();
        }
    }

    public void SetCloseOnOverlayClick(bool closeOnClick)
    {
        _closeOnOverlayClick = closeOnClick;
    }

    public override Rectangle GetBoundingBox()
    {
        return _modalBounds;
    }

    public Rectangle GetContentBounds()
    {
        return _contentBounds;
    }

    public override void SetBounds(Rectangle bounds)
    {
        _modalBounds = bounds;
        CalculateBounds();
        if (_isClosable)
        {
            CreateCloseButton();
        }
    }

    public void SetScreenBounds(Rectangle screenBounds)
    {
        _screenBounds = screenBounds;
    }

    public void CenterOnScreen()
    {
        var centeredBounds = new Rectangle(
            _screenBounds.X + (_screenBounds.Width - _modalBounds.Width) / 2,
            _screenBounds.Y + (_screenBounds.Height - _modalBounds.Height) / 2,
            _modalBounds.Width,
            _modalBounds.Height
        );
        SetBounds(centeredBounds);
    }

    public void Dispose()
    {
        _pixel?.Dispose();
        _closeButton = null;
    }

    // Factory methods for common modal types
    public static Modal CreateConfirmationModal(Rectangle screenBounds, string title, string message, 
                                               SpriteFont font, Action onConfirm = null, Action onCancel = null)
    {
        var modalBounds = new Rectangle(0, 0, 400, 200);
        var modal = new Modal(screenBounds, modalBounds, title, font);
        modal.CenterOnScreen();

        // Add message label
        var messageLabel = new Label(
            new Rectangle(20, 20, 360, 80),
            message,
            font,
            Color.Black
        );
        modal.AddContentElement(messageLabel);

        // Add confirm button
        var confirmButton = new Button(
            new Rectangle(220, 120, 80, 35),
            "OK",
            font,
            Color.Green,
            Color.LightGreen,
            Color.White,
            () => { modal.Close(); onConfirm?.Invoke(); }
        );
        modal.AddContentElement(confirmButton);

        // Add cancel button
        var cancelButton = new Button(
            new Rectangle(310, 120, 80, 35),
            "Cancel",
            font,
            Color.Gray,
            Color.LightGray,
            Color.White,
            () => { modal.Close(); onCancel?.Invoke(); }
        );
        modal.AddContentElement(cancelButton);

        return modal;
    }

    public static Modal CreateMessageModal(Rectangle screenBounds, string title, string message, 
                                         SpriteFont font, Action onClose = null)
    {
        var modalBounds = new Rectangle(0, 0, 350, 150);
        var modal = new Modal(screenBounds, modalBounds, title, font, null, null, null, null, onClose);
        modal.CenterOnScreen();

        // Add message label
        var messageLabel = new Label(
            new Rectangle(20, 20, 310, 60),
            message,
            font,
            Color.Black
        );
        modal.AddContentElement(messageLabel);

        // Add OK button
        var okButton = new Button(
            new Rectangle(135, 90, 80, 35),
            "OK",
            font,
            Color.Blue,
            Color.LightBlue,
            Color.White,
            () => { modal.Close(); onClose?.Invoke(); }
        );
        modal.AddContentElement(okButton);

        return modal;
    }
}