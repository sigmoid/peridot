namespace Peridot.UI;

using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Peridot;

public class TextInput : UIElement
{
    private Rectangle _bounds;
    private string _text;
    private string _placeholder;
    private SpriteFont _font;
    private Color _backgroundColor;
    private Color _textColor;
    private Color _placeholderColor;
    private Color _borderColor;
    private Color _focusedBorderColor;
    private Color _cursorColor;
    private Texture2D _pixel;
    
    // Input state
    private bool _isFocused;
    private int _cursorPosition;
    private float _cursorBlinkTimer;
    private bool _cursorVisible;
    private const float CURSOR_BLINK_INTERVAL = 0.5f;
    
    // Keyboard state tracking
    private KeyboardState _previousKeyboardState;
    private MouseState _previousMouseState;
    private float _keyRepeatTimer;
    private Keys _repeatingKey;
    private const float KEY_REPEAT_DELAY = 0.5f;
    private const float KEY_REPEAT_INTERVAL = 0.05f;
    
    // Selection (future enhancement)
    private int _selectionStart;
    private int _selectionEnd;
    
    // Padding and styling
    private int _padding;
    private int _borderWidth;
    
    // Events
    public event Action<string> OnTextChanged;
    public event Action OnFocusGained;
    public event Action OnFocusLost;
    public event Action<string> OnEnterPressed;

    public string Text 
    { 
        get => _text; 
        set 
        { 
            if (_text != value)
            {
                _text = value ?? string.Empty;
                _cursorPosition = Math.Min(_cursorPosition, _text.Length);
                OnTextChanged?.Invoke(_text);
            }
        } 
    }
    
    public string Placeholder 
    { 
        get => _placeholder; 
        set => _placeholder = value ?? string.Empty; 
    }
    
    public bool IsFocused => _isFocused;
    
    public int MaxLength { get; set; } = int.MaxValue;

    public TextInput(Rectangle bounds, SpriteFont font, string placeholder = "", 
        Color? backgroundColor = null, Color? textColor = null, 
        Color? borderColor = null, Color? focusedBorderColor = null,
        int padding = 8, int borderWidth = 2)
    {
        _bounds = bounds;
        _font = font ?? throw new ArgumentNullException(nameof(font));
        _text = string.Empty;
        _placeholder = placeholder ?? string.Empty;
        _backgroundColor = backgroundColor ?? Color.White;
        _textColor = textColor ?? Color.Black;
        _placeholderColor = Color.Gray;
        _borderColor = borderColor ?? Color.DarkGray;
        _focusedBorderColor = focusedBorderColor ?? Color.CornflowerBlue;
        _cursorColor = Color.Black;
        _padding = padding;
        _borderWidth = borderWidth;
        
        _cursorPosition = 0;
        _cursorBlinkTimer = 0f;
        _cursorVisible = true;
        
        // Create pixel texture for drawing
        _pixel = new Texture2D(Core.GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
        
        _previousKeyboardState = Keyboard.GetState();
        _previousMouseState = Mouse.GetState();
    }

    public override void Update(float deltaTime)
    {
        var mouseState = Mouse.GetState();
        var mousePosition = new Vector2(mouseState.X, mouseState.Y);
        var keyboardState = Keyboard.GetState();
        
        // Handle mouse clicks for focus
        bool isMousePressed = mouseState.LeftButton == ButtonState.Pressed;
        bool isMouseClick = isMousePressed && _previousMouseState.LeftButton == ButtonState.Released;
        
        if (isMouseClick)
        {
            bool wasClicked = _bounds.Contains(mousePosition);
            
            if (wasClicked && !_isFocused)
            {
                SetFocus(true);
                // Position cursor at click location
                _cursorPosition = GetCursorPositionFromMouse(mousePosition);
            }
            else if (!wasClicked && _isFocused)
            {
                SetFocus(false);
            }
        }
        
        if (_isFocused)
        {
            // Update cursor blink
            _cursorBlinkTimer += deltaTime;
            if (_cursorBlinkTimer >= CURSOR_BLINK_INTERVAL)
            {
                _cursorVisible = !_cursorVisible;
                _cursorBlinkTimer = 0f;
            }
            
            // Handle keyboard input
            HandleKeyboardInput(keyboardState, deltaTime);
        }
        
        _previousKeyboardState = keyboardState;
        _previousMouseState = mouseState;
    }

    private void HandleKeyboardInput(KeyboardState keyboardState, float deltaTime)
    {
        var pressedKeys = keyboardState.GetPressedKeys();
        var previousPressedKeys = _previousKeyboardState.GetPressedKeys();
        
        // Handle key repeat timing
        if (_repeatingKey != Keys.None && keyboardState.IsKeyUp(_repeatingKey))
        {
            _repeatingKey = Keys.None;
            _keyRepeatTimer = 0f;
        }
        
        if (_repeatingKey != Keys.None)
        {
            _keyRepeatTimer += deltaTime;
            if (_keyRepeatTimer >= KEY_REPEAT_INTERVAL)
            {
                ProcessKey(_repeatingKey, keyboardState);
                _keyRepeatTimer = 0f;
            }
        }
        
        // Handle newly pressed keys
        foreach (var key in pressedKeys.Except(previousPressedKeys))
        {
            ProcessKey(key, keyboardState);
            
            // Start key repeat for navigation and deletion keys
            if (IsRepeatableKey(key))
            {
                _repeatingKey = key;
                _keyRepeatTimer = -KEY_REPEAT_DELAY; // Negative delay for initial press
            }
        }
    }

    private bool IsRepeatableKey(Keys key)
    {
        return key == Keys.Left || key == Keys.Right || key == Keys.Back || key == Keys.Delete ||
               key == Keys.Home || key == Keys.End;
    }

    private void ProcessKey(Keys key, KeyboardState keyboardState)
    {
        // Reset cursor blink when typing
        _cursorBlinkTimer = 0f;
        _cursorVisible = true;
        
        bool shift = keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift);
        bool ctrl = keyboardState.IsKeyDown(Keys.LeftControl) || keyboardState.IsKeyDown(Keys.RightControl);
        
        switch (key)
        {
            case Keys.Back:
                if (_cursorPosition > 0)
                {
                    _text = _text.Remove(_cursorPosition - 1, 1);
                    _cursorPosition--;
                    OnTextChanged?.Invoke(_text);
                }
                break;
                
            case Keys.Delete:
                if (_cursorPosition < _text.Length)
                {
                    _text = _text.Remove(_cursorPosition, 1);
                    OnTextChanged?.Invoke(_text);
                }
                break;
                
            case Keys.Left:
                if (_cursorPosition > 0)
                {
                    if (ctrl)
                        _cursorPosition = GetPreviousWordBoundary();
                    else
                        _cursorPosition--;
                }
                break;
                
            case Keys.Right:
                if (_cursorPosition < _text.Length)
                {
                    if (ctrl)
                        _cursorPosition = GetNextWordBoundary();
                    else
                        _cursorPosition++;
                }
                break;
                
            case Keys.Home:
                _cursorPosition = 0;
                break;
                
            case Keys.End:
                _cursorPosition = _text.Length;
                break;
                
            case Keys.Enter:
                OnEnterPressed?.Invoke(_text);
                break;
                
            case Keys.A:
                if (ctrl)
                {
                    // Select all (future enhancement)
                    _selectionStart = 0;
                    _selectionEnd = _text.Length;
                }
                else
                {
                    InsertCharacter(shift ? 'A' : 'a');
                }
                break;
                
            case Keys.C:
                if (ctrl)
                {
                    // Copy selected text to clipboard, or entire text if no selection
                    string textToCopy = GetSelectedText();
                    if (string.IsNullOrEmpty(textToCopy))
                        textToCopy = _text; // Copy entire text if no selection
                    
                    Core.UISystem.SetClipboardText(textToCopy);
                }
                else
                {
                    InsertCharacter(shift ? 'C' : 'c');
                }
                break;
                
            case Keys.V:
                if (ctrl)
                {
                    // Paste from clipboard
                    string clipboardText = Core.UISystem.GetClipboardText();
                    if (!string.IsNullOrEmpty(clipboardText))
                    {
                        // Remove any newlines for single-line text input
                        clipboardText = clipboardText.Replace("\r\n", "").Replace("\n", "").Replace("\r", "");
                        
                        // Delete selected text first if any
                        DeleteSelectedText();
                        
                        // Insert the clipboard text, respecting MaxLength
                        foreach (char c in clipboardText)
                        {
                            if (char.IsControl(c))
                                continue; // Skip control characters
                            if (_text.Length >= MaxLength)
                                break; // Stop if we've reached the maximum length
                            InsertCharacter(c);
                        }
                    }
                }
                else
                {
                    InsertCharacter(shift ? 'V' : 'v');
                }
                break;
                
            case Keys.X:
                if (ctrl)
                {
                    // Cut selected text to clipboard, or entire text if no selection
                    string textToCut = GetSelectedText();
                    if (string.IsNullOrEmpty(textToCut))
                        textToCut = _text; // Cut entire text if no selection
                    
                    if (!string.IsNullOrEmpty(textToCut))
                    {
                        Core.UISystem.SetClipboardText(textToCut);
                        
                        // Delete the cut text
                        if (HasSelection())
                        {
                            DeleteSelectedText();
                        }
                        else
                        {
                            // Clear entire text
                            _text = string.Empty;
                            _cursorPosition = 0;
                            OnTextChanged?.Invoke(_text);
                        }
                    }
                }
                else
                {
                    InsertCharacter(shift ? 'X' : 'x');
                }
                break;
                
            default:
                // Handle character input
                char character = GetCharacterFromKey(key, shift);
                if (character != '\0')
                {
                    InsertCharacter(character);
                }
                break;
        }
    }

    private void InsertCharacter(char character)
    {
        if (_text.Length < MaxLength)
        {
            _text = _text.Insert(_cursorPosition, character.ToString());
            _cursorPosition++;
            OnTextChanged?.Invoke(_text);
        }
    }

    private char GetCharacterFromKey(Keys key, bool shift)
    {
        // Handle letters
        if (key >= Keys.A && key <= Keys.Z)
        {
            char baseChar = (char)('a' + (key - Keys.A));
            return shift ? char.ToUpper(baseChar) : baseChar;
        }
        
        // Handle numbers and symbols
        return key switch
        {
            Keys.D0 => shift ? ')' : '0',
            Keys.D1 => shift ? '!' : '1',
            Keys.D2 => shift ? '@' : '2',
            Keys.D3 => shift ? '#' : '3',
            Keys.D4 => shift ? '$' : '4',
            Keys.D5 => shift ? '%' : '5',
            Keys.D6 => shift ? '^' : '6',
            Keys.D7 => shift ? '&' : '7',
            Keys.D8 => shift ? '*' : '8',
            Keys.D9 => shift ? '(' : '9',
            Keys.Space => ' ',
            Keys.OemPeriod => shift ? '>' : '.',
            Keys.Decimal => '.', // Numpad decimal point
            Keys.NumPad0 => '0',
            Keys.NumPad1 => '1',
            Keys.NumPad2 => '2',
            Keys.NumPad3 => '3',
            Keys.NumPad4 => '4',
            Keys.NumPad5 => '5',
            Keys.NumPad6 => '6',
            Keys.NumPad7 => '7',
            Keys.NumPad8 => '8',
            Keys.NumPad9 => '9',
            Keys.OemComma => shift ? '<' : ',',
            Keys.OemQuestion => shift ? '?' : '/',
            Keys.OemSemicolon => shift ? ':' : ';',
            Keys.OemQuotes => shift ? '"' : '\'',
            Keys.OemOpenBrackets => shift ? '{' : '[',
            Keys.OemCloseBrackets => shift ? '}' : ']',
            Keys.OemPipe => shift ? '|' : '\\',
            Keys.OemMinus => shift ? '_' : '-',
            Keys.OemPlus => shift ? '+' : '=',
            Keys.OemTilde => shift ? '~' : '`',
            _ => '\0'
        };
    }

    private int GetPreviousWordBoundary()
    {
        int position = _cursorPosition - 1;
        while (position > 0 && char.IsWhiteSpace(_text[position]))
            position--;
        while (position > 0 && !char.IsWhiteSpace(_text[position - 1]))
            position--;
        return position;
    }

    private int GetNextWordBoundary()
    {
        int position = _cursorPosition;
        while (position < _text.Length && !char.IsWhiteSpace(_text[position]))
            position++;
        while (position < _text.Length && char.IsWhiteSpace(_text[position]))
            position++;
        return position;
    }

    private int GetCursorPositionFromMouse(Vector2 mousePosition)
    {
        var textBounds = GetTextBounds();
        float relativeX = mousePosition.X - textBounds.X;
        
        if (relativeX <= 0) return 0;
        if (string.IsNullOrEmpty(_text)) return 0;
        
        // Find the closest character position
        for (int i = 0; i <= _text.Length; i++)
        {
            string substring = _text.Substring(0, i);
            float width = _font.MeasureString(substring).X;
            
            if (width > relativeX)
            {
                // Check if we're closer to this position or the previous one
                if (i > 0)
                {
                    float prevWidth = _font.MeasureString(_text.Substring(0, i - 1)).X;
                    float currentDistance = Math.Abs(width - relativeX);
                    float prevDistance = Math.Abs(prevWidth - relativeX);
                    return prevDistance < currentDistance ? i - 1 : i;
                }
                return i;
            }
        }
        
        return _text.Length;
    }

    private Rectangle GetTextBounds()
    {
        return new Rectangle(
            _bounds.X + _borderWidth + _padding,
            _bounds.Y + _borderWidth + _padding,
            _bounds.Width - 2 * (_borderWidth + _padding),
            _bounds.Height - 2 * (_borderWidth + _padding)
        );
    }

    public void SetFocus(bool focused)
    {
        if (_isFocused != focused)
        {
            _isFocused = focused;
            _cursorBlinkTimer = 0f;
            _cursorVisible = true;
            
            if (focused)
            {
                OnFocusGained?.Invoke();
            }
            else
            {
                OnFocusLost?.Invoke();
                _repeatingKey = Keys.None;
                _keyRepeatTimer = 0f;
            }
        }
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        // Draw border
        Color currentBorderColor = _isFocused ? _focusedBorderColor : _borderColor;
        spriteBatch.Draw(_pixel, _bounds, null, currentBorderColor, 0, Vector2.Zero, SpriteEffects.None, GetActualOrder());
        
        // Draw background
        var backgroundBounds = new Rectangle(
            _bounds.X + _borderWidth,
            _bounds.Y + _borderWidth,
            _bounds.Width - 2 * _borderWidth,
            _bounds.Height - 2 * _borderWidth
        );
        spriteBatch.Draw(_pixel, backgroundBounds, null, _backgroundColor, 0, Vector2.Zero, SpriteEffects.None, GetActualOrder() + 0.01f);
        
        var textBounds = GetTextBounds();
        
        // Draw text or placeholder
        string displayText = string.IsNullOrEmpty(_text) ? _placeholder : _text;
        Color displayColor = string.IsNullOrEmpty(_text) ? _placeholderColor : _textColor;
        
        if (!string.IsNullOrEmpty(displayText))
        {
            // Calculate text position (vertically centered)
            var textSize = _font.MeasureString(displayText);
            var textPosition = new Vector2(
                textBounds.X,
                textBounds.Y + (textBounds.Height - textSize.Y) / 2
            );
            
            spriteBatch.DrawString(_font, displayText, textPosition, displayColor, 0, Vector2.Zero, 1.0f, SpriteEffects.None, GetActualOrder() + 0.02f);
        }
        
        // Draw cursor
        if (_isFocused && _cursorVisible && !string.IsNullOrEmpty(_text))
        {
            DrawCursor(spriteBatch, textBounds);
        }
        else if (_isFocused && _cursorVisible && string.IsNullOrEmpty(_text))
        {
            // Draw cursor at the beginning when text is empty
            var textSize = _font.MeasureString("I"); // Use a sample character for height
            var cursorPosition = new Vector2(
                textBounds.X,
                textBounds.Y + (textBounds.Height - textSize.Y) / 2
            );
            var cursorBounds = new Rectangle(
                (int)cursorPosition.X,
                (int)cursorPosition.Y,
                1,
                (int)textSize.Y
            );
            spriteBatch.Draw(_pixel, cursorBounds, null, _cursorColor, 0, Vector2.Zero, SpriteEffects.None, GetActualOrder() + 0.03f);
        }
    }

    private void DrawCursor(SpriteBatch spriteBatch, Rectangle textBounds)
    {
        string textToCursor = _text.Substring(0, _cursorPosition);
        var textSize = _font.MeasureString(textToCursor);
        var fullTextSize = _font.MeasureString("I"); // For cursor height
        
        var cursorPosition = new Vector2(
            textBounds.X + textSize.X,
            textBounds.Y + (textBounds.Height - fullTextSize.Y) / 2
        );
        
        var cursorBounds = new Rectangle(
            (int)cursorPosition.X,
            (int)cursorPosition.Y,
            1,
            (int)fullTextSize.Y
        );
        
        spriteBatch.Draw(_pixel, cursorBounds, null, _cursorColor, 0, Vector2.Zero, SpriteEffects.None, GetActualOrder() + 0.03f);
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
    public void Clear()
    {
        Text = string.Empty;
        _cursorPosition = 0;
    }

    public void SelectAll()
    {
        _selectionStart = 0;
        _selectionEnd = _text.Length;
    }

    public void SetCursorPosition(int position)
    {
        _cursorPosition = Math.Max(0, Math.Min(position, _text.Length));
        _cursorBlinkTimer = 0f;
        _cursorVisible = true;
    }

    // Clipboard and selection helper methods
    private string GetSelectedText()
    {
        if (!HasSelection())
            return string.Empty;
            
        int start = Math.Min(_selectionStart, _selectionEnd);
        int end = Math.Max(_selectionStart, _selectionEnd);
        int length = end - start;
        
        if (start < 0 || start >= _text.Length || length <= 0)
            return string.Empty;
            
        return _text.Substring(start, Math.Min(length, _text.Length - start));
    }
    
    private bool HasSelection()
    {
        return _selectionStart != _selectionEnd && 
               _selectionStart >= 0 && _selectionEnd >= 0 &&
               _selectionStart <= _text.Length && _selectionEnd <= _text.Length;
    }
    
    private void DeleteSelectedText()
    {
        if (!HasSelection())
            return;
            
        int start = Math.Min(_selectionStart, _selectionEnd);
        int end = Math.Max(_selectionStart, _selectionEnd);
        int length = end - start;
        
        if (start >= 0 && start < _text.Length && length > 0)
        {
            _text = _text.Remove(start, Math.Min(length, _text.Length - start));
            _cursorPosition = start;
            _selectionStart = _selectionEnd = _cursorPosition;
            OnTextChanged?.Invoke(_text);
        }
    }

    public void Dispose()
    {
        _pixel?.Dispose();
    }
}