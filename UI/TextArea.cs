namespace Peridot.UI;

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Peridot;

/// <summary>
/// A multi-line text input control with word wrapping, scrolling, and editing capabilities.
/// 
/// Example usage:
/// 
/// // Editable TextArea
/// var editableTextArea = new TextArea(bounds, font, "Type here...", wordWrap: true, readOnly: false);
/// 
/// // Read-only TextArea for displaying logs
/// var logTextArea = new TextArea(bounds, font, "", wordWrap: true, readOnly: true);
/// logTextArea.Text = "This is read-only content\nUsers cannot edit this text";
/// 
/// // Toggle read-only state
/// textArea.ReadOnly = true;  // Disable editing
/// textArea.ReadOnly = false; // Enable editing
/// </summary>

public class TextArea : UIElement
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
    private Color _selectionColor;
    private Texture2D _pixel;
    
    // Multi-line text handling
    private List<string> _lines;
    private List<string> _wrappedLines;
    private int _cursorLine;
    private int _cursorColumn;
    private float _lineHeight;
    private bool _wordWrap;
    
    // Scrolling
    private int _scrollOffsetY;
    private int _maxVisibleLines;
    
    // Input state
    private bool _isFocused;
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
    
    // Selection
    private int _selectionStartLine;
    private int _selectionStartColumn;
    private int _selectionEndLine;
    private int _selectionEndColumn;
    private bool _hasSelection;
    
    // Styling
    private int _padding;
    private int _borderWidth;
    
    // Events
    public event Action<string> OnTextChanged;
    public event Action OnFocusGained;
    public event Action OnFocusLost;
    
    // Properties
    public string Text 
    { 
        get => _text; 
        set 
        { 
            string filteredValue = FilterUnsupportedCharacters(value ?? string.Empty);
            if (_text != filteredValue)
            {
                _text = filteredValue;
                UpdateLines();
                ValidateCursorPosition();
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
    public bool WordWrap 
    { 
        get => _wordWrap; 
        set 
        { 
            if (_wordWrap != value)
            {
                _wordWrap = value;
                UpdateWrappedLines();
            }
        } 
    }
    
    public int MaxLength { get; set; } = int.MaxValue;
    /// <summary>
    /// Gets or sets whether the TextArea is read-only. When true:
    /// - Text cannot be edited through user input
    /// - Cursor is not displayed
    /// - Mouse clicks won't position cursor
    /// - Keyboard input is ignored
    /// - Clear() method has no effect
    /// - Visual appearance is slightly dimmed
    /// - Text can still be set programmatically via the Text property
    /// </summary>
    public bool ReadOnly { get; set; } = false;

    /// <summary>
    /// Creates a new TextArea with the specified parameters.
    /// </summary>
    /// <param name="bounds">The bounds of the TextArea</param>
    /// <param name="font">The font to use for text rendering</param>
    /// <param name="placeholder">Placeholder text shown when empty</param>
    /// <param name="wordWrap">Whether to enable word wrapping</param>
    /// <param name="readOnly">Whether the TextArea should be read-only (no user editing)</param>
    /// <param name="backgroundColor">Background color (default: White)</param>
    /// <param name="textColor">Text color (default: Black)</param>
    /// <param name="borderColor">Border color (default: DarkGray)</param>
    /// <param name="focusedBorderColor">Border color when focused (default: CornflowerBlue)</param>
    /// <param name="padding">Internal padding (default: 8)</param>
    /// <param name="borderWidth">Border width (default: 2)</param>
    public TextArea(Rectangle bounds, SpriteFont font, string placeholder = "", 
        bool wordWrap = true, bool readOnly = false, Color? backgroundColor = null, Color? textColor = null, 
        Color? borderColor = null, Color? focusedBorderColor = null,
        int padding = 8, int borderWidth = 2)
    {
        _bounds = bounds;
        _font = font ?? throw new ArgumentNullException(nameof(font));
        _text = string.Empty;
        _placeholder = placeholder ?? string.Empty;
        _wordWrap = wordWrap;
        ReadOnly = readOnly;
        _backgroundColor = backgroundColor ?? Color.White;
        _textColor = textColor ?? Color.Black;
        _placeholderColor = Color.Gray;
        _borderColor = borderColor ?? Color.DarkGray;
        _focusedBorderColor = focusedBorderColor ?? Color.CornflowerBlue;
        _cursorColor = Color.Black;
        _selectionColor = Color.CornflowerBlue * 0.3f;
        _padding = padding;
        _borderWidth = borderWidth;
        
        _lines = new List<string>();
        _wrappedLines = new List<string>();
        _cursorLine = 0;
        _cursorColumn = 0;
        _scrollOffsetY = 0;
        
        _lineHeight = _font.LineSpacing;
        CalculateMaxVisibleLines();
        
        _cursorBlinkTimer = 0f;
        _cursorVisible = true;
        
        // Create pixel texture for drawing
        _pixel = new Texture2D(Core.GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
        
        _previousKeyboardState = Keyboard.GetState();
        _previousMouseState = Mouse.GetState();
        
        UpdateLines();
    }

    /// <summary>
    /// Checks if a character is supported by the current font.
    /// This prevents runtime errors when the font doesn't contain certain Unicode characters.
    /// </summary>
    private bool IsCharacterSupported(char character)
    {
        // Common characters that are usually safe
        if (character >= 32 && character <= 126) // Basic ASCII printable characters
            return true;
        
        if (character == '\t' || character == '\n' || character == '\r') // Control characters we handle
            return true;
        
        // For other characters, we need to check if the font contains them
        // This is a simplified check - in a real implementation you might want to cache this
        try
        {
            var glyphs = _font.Characters;
            return glyphs.Contains(character);
        }
        catch
        {
            // If we can't check, assume it's not supported to be safe
            return false;
        }
    }

    /// <summary>
    /// Filters text to remove or replace characters that aren't supported by the current font.
    /// This prevents rendering errors and exceptions.
    /// </summary>
    private string FilterUnsupportedCharacters(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var filtered = new System.Text.StringBuilder(input.Length);
        
        foreach (char c in input)
        {
            if (IsCharacterSupported(c))
            {
                filtered.Append(c);
            }
            else
            {
                // Replace unsupported characters with safe alternatives
                switch (c)
                {
                    case '\u2022': // Bullet point •
                        filtered.Append('-');
                        break;
                    case '\u2013': // En dash –
                    case '\u2014': // Em dash —
                        filtered.Append('-');
                        break;
                    case '\u2018': // Left single quotation mark '
                    case '\u2019': // Right single quotation mark '
                        filtered.Append('\'');
                        break;
                    case '\u201C': // Left double quotation mark "
                    case '\u201D': // Right double quotation mark "
                        filtered.Append('"');
                        break;
                    case '\u2026': // Horizontal ellipsis …
                        filtered.Append("...");
                        break;
                    default:
                        // For other unsupported characters, we can either:
                        // 1. Skip them (do nothing)
                        // 2. Replace with '?' 
                        // 3. Replace with space
                        // Here we'll skip them to avoid visual clutter
                        break;
                }
            }
        }
        
        return filtered.ToString();
    }

    private void UpdateLines()
    {
        _lines.Clear();
        if (string.IsNullOrEmpty(_text))
        {
            _lines.Add(string.Empty);
        }
        else
        {
            string[] lines = _text.Split(new[] { '\r', '\n' }, StringSplitOptions.None);
            // Handle different line ending types
            var normalizedLines = new List<string>();
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i] != string.Empty || i == lines.Length - 1)
                {
                    normalizedLines.Add(lines[i]);
                }
            }
            _lines.AddRange(normalizedLines);
        }
        
        if (_lines.Count == 0)
        {
            _lines.Add(string.Empty);
        }
        
        UpdateWrappedLines();
    }

    private void UpdateWrappedLines()
    {
        _wrappedLines.Clear();
        
        if (!_wordWrap)
        {
            _wrappedLines.AddRange(_lines);
            return;
        }
        
        var textBounds = GetTextBounds();
        float maxWidth = textBounds.Width;
        
        foreach (string line in _lines)
        {
            if (string.IsNullOrEmpty(line))
            {
                _wrappedLines.Add(string.Empty);
                continue;
            }
            
            var wrappedLinesForThisLine = WrapLine(line, maxWidth);
            _wrappedLines.AddRange(wrappedLinesForThisLine);
        }
    }

    private List<string> WrapLine(string line, float maxWidth)
    {
        var result = new List<string>();
        
        if (_font.MeasureString(line).X <= maxWidth)
        {
            result.Add(line);
            return result;
        }
        
        var words = line.Split(' ');
        var currentLine = string.Empty;
        
        foreach (string word in words)
        {
            string testLine = string.IsNullOrEmpty(currentLine) ? word : currentLine + " " + word;
            
            if (_font.MeasureString(testLine).X <= maxWidth)
            {
                currentLine = testLine;
            }
            else
            {
                if (!string.IsNullOrEmpty(currentLine))
                {
                    result.Add(currentLine);
                    currentLine = word;
                }
                else
                {
                    // Word is too long for a single line, break it
                    result.AddRange(BreakLongWord(word, maxWidth));
                    currentLine = string.Empty;
                }
            }
        }
        
        if (!string.IsNullOrEmpty(currentLine))
        {
            result.Add(currentLine);
        }
        
        return result;
    }

    private List<string> BreakLongWord(string word, float maxWidth)
    {
        var result = new List<string>();
        var currentPart = string.Empty;
        
        foreach (char c in word)
        {
            string testPart = currentPart + c;
            if (_font.MeasureString(testPart).X <= maxWidth)
            {
                currentPart = testPart;
            }
            else
            {
                if (!string.IsNullOrEmpty(currentPart))
                {
                    result.Add(currentPart);
                }
                currentPart = c.ToString();
            }
        }
        
        if (!string.IsNullOrEmpty(currentPart))
        {
            result.Add(currentPart);
        }
        
        return result;
    }

    private void CalculateMaxVisibleLines()
    {
        var textBounds = GetTextBounds();
        _maxVisibleLines = (int)Math.Floor(textBounds.Height / _lineHeight);
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

    public override void Update(float deltaTime)
    {
        if (!IsVisible()) return;

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
                if (!ReadOnly)
                {
                    SetCursorFromMouse(mousePosition);
                }
            }
            else if (!wasClicked && _isFocused)
            {
                SetFocus(false);
            }
            else if (wasClicked && _isFocused && !ReadOnly)
            {
                SetCursorFromMouse(mousePosition);
            }
        }
        
        if (_isFocused && !ReadOnly)
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

    private void SetCursorFromMouse(Vector2 mousePosition)
    {
        var textBounds = GetTextBounds();
        float relativeY = mousePosition.Y - textBounds.Y;
        int displayLineIndex = (int)(relativeY / _lineHeight) + _scrollOffsetY;
        
        // Convert display line back to original line and column
        var originalPos = ConvertDisplayToOriginal(displayLineIndex, mousePosition.X - textBounds.X);
        _cursorLine = Math.Max(0, Math.Min(originalPos.Line, _lines.Count - 1));
        _cursorColumn = Math.Max(0, Math.Min(originalPos.Column, _lines[_cursorLine].Length));
        
        EnsureCursorVisible();
    }

    private (int Line, int Column) ConvertDisplayToOriginal(int displayLine, float x)
    {
        if (!_wordWrap)
        {
            // No wrapping - simple case
            displayLine = Math.Max(0, Math.Min(displayLine, _lines.Count - 1));
            if (displayLine < _lines.Count)
            {
                int column = GetColumnFromX(_lines[displayLine], x);
                return (displayLine, column);
            }
            return (0, 0);
        }
        
        // With word wrapping - find which original line this display line belongs to
        int currentDisplayLine = 0;
        
        for (int originalLine = 0; originalLine < _lines.Count; originalLine++)
        {
            var wrappedSegments = WrapLine(_lines[originalLine], GetTextBounds().Width);
            
            // Check if target display line is within this original line's wrapped segments
            if (displayLine >= currentDisplayLine && displayLine < currentDisplayLine + wrappedSegments.Count)
            {
                int segmentIndex = displayLine - currentDisplayLine;
                if (segmentIndex < wrappedSegments.Count)
                {
                    string segment = wrappedSegments[segmentIndex];
                    int columnInSegment = GetColumnFromX(segment, x);
                    
                    // Use character mapping to convert back to original position
                    var segmentMap = BuildCharacterToSegmentMap(_lines[originalLine], wrappedSegments);
                    if (segmentIndex < segmentMap.Count)
                    {
                        int originalColumn = segmentMap[segmentIndex].StartChar + columnInSegment;
                        return (originalLine, Math.Min(originalColumn, _lines[originalLine].Length));
                    }
                }
            }
            
            currentDisplayLine += wrappedSegments.Count;
        }
        
        // Fallback to end of text
        if (_lines.Count > 0)
        {
            return (_lines.Count - 1, _lines[_lines.Count - 1].Length);
        }
        
        return (0, 0);
    }



    private int GetColumnFromX(string line, float x)
    {
        if (x <= 0 || string.IsNullOrEmpty(line)) return 0;
        
        for (int i = 0; i <= line.Length; i++)
        {
            string substring = line.Substring(0, i);
            float width = _font.MeasureString(substring).X;
            
            if (width > x)
            {
                if (i > 0)
                {
                    float prevWidth = _font.MeasureString(line.Substring(0, i - 1)).X;
                    return Math.Abs(width - x) < Math.Abs(prevWidth - x) ? i : i - 1;
                }
                return i;
            }
        }
        
        return line.Length;
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
            
            if (IsRepeatableKey(key))
            {
                _repeatingKey = key;
                _keyRepeatTimer = -KEY_REPEAT_DELAY;
            }
        }
    }

    private bool IsRepeatableKey(Keys key)
    {
        return key == Keys.Left || key == Keys.Right || key == Keys.Up || key == Keys.Down ||
               key == Keys.Back || key == Keys.Delete || key == Keys.Home || key == Keys.End ||
               key == Keys.PageUp || key == Keys.PageDown;
    }

    private void ProcessKey(Keys key, KeyboardState keyboardState)
    {
        _cursorBlinkTimer = 0f;
        _cursorVisible = true;
        
        bool shift = keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift);
        bool ctrl = keyboardState.IsKeyDown(Keys.LeftControl) || keyboardState.IsKeyDown(Keys.RightControl);
        
        switch (key)
        {
            case Keys.Back:
                HandleBackspace();
                break;
                
            case Keys.Delete:
                HandleDelete();
                break;
                
            case Keys.Left:
                MoveCursorLeft(ctrl);
                break;
                
            case Keys.Right:
                MoveCursorRight(ctrl);
                break;
                
            case Keys.Up:
                MoveCursorUp();
                break;
                
            case Keys.Down:
                MoveCursorDown();
                break;
                
            case Keys.Home:
                if (ctrl)
                    MoveCursorToStart();
                else
                    MoveCursorToLineStart();
                break;
                
            case Keys.End:
                if (ctrl)
                    MoveCursorToEnd();
                else
                    MoveCursorToLineEnd();
                break;
                
            case Keys.PageUp:
                MoveCursorPageUp();
                break;
                
            case Keys.PageDown:
                MoveCursorPageDown();
                break;
                
            case Keys.Enter:
                InsertNewLine();
                break;
                
            case Keys.Tab:
                InsertCharacter('\t');
                break;
                
            case Keys.A:
                if (ctrl)
                {
                    SelectAll();
                }
                else
                {
                    InsertCharacter(shift ? 'A' : 'a');
                }
                break;
                
            case Keys.C:
                if (ctrl)
                {
                    CopyToClipboard();
                }
                else
                {
                    InsertCharacter(shift ? 'C' : 'c');
                }
                break;
                
            case Keys.V:
                if (ctrl)
                {
                    PasteFromClipboard();
                }
                else
                {
                    InsertCharacter(shift ? 'V' : 'v');
                }
                break;
                
            case Keys.X:
                if (ctrl)
                {
                    CutToClipboard();
                }
                else
                {
                    InsertCharacter(shift ? 'X' : 'x');
                }
                break;
                
            default:
                char character = GetCharacterFromKey(key, shift);
                if (character != '\0')
                {
                    InsertCharacter(character);
                }
                break;
        }
        
        EnsureCursorVisible();
    }

    private void HandleBackspace()
    {
        if (_hasSelection)
        {
            DeleteSelection();
            return;
        }
        
        if (_cursorColumn > 0)
        {
            _lines[_cursorLine] = _lines[_cursorLine].Remove(_cursorColumn - 1, 1);
            _cursorColumn--;
        }
        else if (_cursorLine > 0)
        {
            // Join with previous line
            _cursorColumn = _lines[_cursorLine - 1].Length;
            _lines[_cursorLine - 1] += _lines[_cursorLine];
            _lines.RemoveAt(_cursorLine);
            _cursorLine--;
        }
        
        UpdateTextFromLines();
    }

    private void HandleDelete()
    {
        if (_hasSelection)
        {
            DeleteSelection();
            return;
        }
        
        if (_cursorColumn < _lines[_cursorLine].Length)
        {
            _lines[_cursorLine] = _lines[_cursorLine].Remove(_cursorColumn, 1);
        }
        else if (_cursorLine < _lines.Count - 1)
        {
            // Join with next line
            _lines[_cursorLine] += _lines[_cursorLine + 1];
            _lines.RemoveAt(_cursorLine + 1);
        }
        
        UpdateTextFromLines();
    }

    private void MoveCursorLeft(bool wordJump = false)
    {
        if (wordJump)
        {
            MoveCursorToPreviousWord();
        }
        else
        {
            if (_cursorColumn > 0)
            {
                _cursorColumn--;
            }
            else if (_cursorLine > 0)
            {
                _cursorLine--;
                _cursorColumn = _lines[_cursorLine].Length;
            }
        }
    }

    private void MoveCursorRight(bool wordJump = false)
    {
        if (wordJump)
        {
            MoveCursorToNextWord();
        }
        else
        {
            if (_cursorColumn < _lines[_cursorLine].Length)
            {
                _cursorColumn++;
            }
            else if (_cursorLine < _lines.Count - 1)
            {
                _cursorLine++;
                _cursorColumn = 0;
            }
        }
    }

    private void MoveCursorUp()
    {
        if (_cursorLine > 0)
        {
            _cursorLine--;
            _cursorColumn = Math.Min(_cursorColumn, _lines[_cursorLine].Length);
        }
    }

    private void MoveCursorDown()
    {
        if (_cursorLine < _lines.Count - 1)
        {
            _cursorLine++;
            _cursorColumn = Math.Min(_cursorColumn, _lines[_cursorLine].Length);
        }
    }

    private void MoveCursorToLineStart()
    {
        _cursorColumn = 0;
    }

    private void MoveCursorToLineEnd()
    {
        _cursorColumn = _lines[_cursorLine].Length;
    }

    private void MoveCursorToStart()
    {
        _cursorLine = 0;
        _cursorColumn = 0;
    }

    private void MoveCursorToEnd()
    {
        _cursorLine = _lines.Count - 1;
        _cursorColumn = _lines[_cursorLine].Length;
    }

    private void MoveCursorPageUp()
    {
        _cursorLine = Math.Max(0, _cursorLine - _maxVisibleLines);
        _cursorColumn = Math.Min(_cursorColumn, _lines[_cursorLine].Length);
    }

    private void MoveCursorPageDown()
    {
        _cursorLine = Math.Min(_lines.Count - 1, _cursorLine + _maxVisibleLines);
        _cursorColumn = Math.Min(_cursorColumn, _lines[_cursorLine].Length);
    }

    private void MoveCursorToPreviousWord()
    {
        // Move to the beginning of the current word or previous word
        string line = _lines[_cursorLine];
        int pos = _cursorColumn - 1;
        
        // Skip whitespace
        while (pos >= 0 && char.IsWhiteSpace(line[pos]))
            pos--;
        
        // Skip word characters
        while (pos >= 0 && !char.IsWhiteSpace(line[pos]))
            pos--;
        
        _cursorColumn = pos + 1;
        
        if (_cursorColumn < 0)
        {
            if (_cursorLine > 0)
            {
                _cursorLine--;
                _cursorColumn = _lines[_cursorLine].Length;
            }
            else
            {
                _cursorColumn = 0;
            }
        }
    }

    private void MoveCursorToNextWord()
    {
        // Move to the beginning of the next word
        string line = _lines[_cursorLine];
        int pos = _cursorColumn;
        
        // Skip current word characters
        while (pos < line.Length && !char.IsWhiteSpace(line[pos]))
            pos++;
        
        // Skip whitespace
        while (pos < line.Length && char.IsWhiteSpace(line[pos]))
            pos++;
        
        if (pos >= line.Length && _cursorLine < _lines.Count - 1)
        {
            _cursorLine++;
            _cursorColumn = 0;
        }
        else
        {
            _cursorColumn = pos;
        }
    }

    private void InsertNewLine()
    {
        if (_hasSelection)
        {
            DeleteSelection();
        }
        
        string currentLine = _lines[_cursorLine];
        string beforeCursor = currentLine.Substring(0, _cursorColumn);
        string afterCursor = currentLine.Substring(_cursorColumn);
        
        _lines[_cursorLine] = beforeCursor;
        _lines.Insert(_cursorLine + 1, afterCursor);
        
        _cursorLine++;
        _cursorColumn = 0;
        
        UpdateTextFromLines();
    }

    private void InsertCharacter(char character)
    {
        if (_text.Length >= MaxLength) return;
        
        // Check if the character is supported by the font
        if (!IsCharacterSupported(character))
        {
            // Try to find a suitable replacement
            string replacement = character switch
            {
                '\u2022' => "-", // Bullet point •
                '\u2013' or '\u2014' => "-", // En dash – or Em dash —
                '\u2018' or '\u2019' => "'", // Single quotes ' '
                '\u201C' or '\u201D' => "\"", // Double quotes " "
                '\u2026' => "...", // Ellipsis …
                _ => null // No replacement, skip the character
            };
            
            if (replacement != null)
            {
                // Insert the replacement string instead
                foreach (char c in replacement)
                {
                    if (_text.Length >= MaxLength) break;
                    if (IsCharacterSupported(c))
                    {
                        if (_hasSelection)
                        {
                            DeleteSelection();
                        }
                        _lines[_cursorLine] = _lines[_cursorLine].Insert(_cursorColumn, c.ToString());
                        _cursorColumn++;
                    }
                }
                UpdateTextFromLines();
            }
            // If no replacement, silently ignore the unsupported character
            return;
        }
        
        if (_hasSelection)
        {
            DeleteSelection();
        }
        
        _lines[_cursorLine] = _lines[_cursorLine].Insert(_cursorColumn, character.ToString());
        _cursorColumn++;
        
        UpdateTextFromLines();
    }

    private void UpdateTextFromLines()
    {
        _text = string.Join("\n", _lines);
        UpdateWrappedLines();
        OnTextChanged?.Invoke(_text);
    }

    private void ValidateCursorPosition()
    {
        if (_lines.Count == 0)
        {
            _lines.Add(string.Empty);
        }
        
        _cursorLine = Math.Max(0, Math.Min(_cursorLine, _lines.Count - 1));
        _cursorColumn = Math.Max(0, Math.Min(_cursorColumn, _lines[_cursorLine].Length));
    }

    private void EnsureCursorVisible()
    {
        var displayPosition = GetCursorDisplayPosition();
        int cursorDisplayLine = displayPosition.Line;
        
        if (cursorDisplayLine < _scrollOffsetY)
        {
            _scrollOffsetY = cursorDisplayLine;
        }
        else if (cursorDisplayLine >= _scrollOffsetY + _maxVisibleLines)
        {
            _scrollOffsetY = cursorDisplayLine - _maxVisibleLines + 1;
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
            Keys.Decimal => '.',
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

    // Clipboard operations
    private void CopyToClipboard()
    {
        string textToCopy = _hasSelection ? GetSelectedText() : _text;
        Core.UISystem.SetClipboardText(textToCopy);
    }

    private void CutToClipboard()
    {
        string textToCut = _hasSelection ? GetSelectedText() : _text;
        Core.UISystem.SetClipboardText(textToCut);
        
        if (_hasSelection)
        {
            DeleteSelection();
        }
        else
        {
            Clear();
        }
    }

    private void PasteFromClipboard()
    {
        string clipboardText = Core.UISystem.GetClipboardText();
        if (!string.IsNullOrEmpty(clipboardText))
        {
            if (_hasSelection)
            {
                DeleteSelection();
            }
            
            // Insert the text character by character to handle newlines properly
            foreach (char c in clipboardText)
            {
                if (_text.Length >= MaxLength) break;
                
                if (c == '\n' || c == '\r')
                {
                    if (c == '\r' && clipboardText.IndexOf(c) < clipboardText.Length - 1 && 
                        clipboardText[clipboardText.IndexOf(c) + 1] == '\n')
                    {
                        continue; // Skip \r in \r\n
                    }
                    InsertNewLine();
                }
                else if (!char.IsControl(c))
                {
                    InsertCharacter(c);
                }
            }
        }
    }

    // Selection methods
    private void SelectAll()
    {
        _selectionStartLine = 0;
        _selectionStartColumn = 0;
        _selectionEndLine = _lines.Count - 1;
        _selectionEndColumn = _lines[_selectionEndLine].Length;
        _hasSelection = true;
    }

    private string GetSelectedText()
    {
        if (!_hasSelection) return string.Empty;
        
        int startLine = Math.Min(_selectionStartLine, _selectionEndLine);
        int endLine = Math.Max(_selectionStartLine, _selectionEndLine);
        int startCol = _selectionStartLine < _selectionEndLine ? _selectionStartColumn : _selectionEndColumn;
        int endCol = _selectionStartLine < _selectionEndLine ? _selectionEndColumn : _selectionStartColumn;
        
        if (startLine == endLine)
        {
            return _lines[startLine].Substring(startCol, endCol - startCol);
        }
        
        var selectedText = new List<string>();
        
        // First line
        selectedText.Add(_lines[startLine].Substring(startCol));
        
        // Middle lines
        for (int i = startLine + 1; i < endLine; i++)
        {
            selectedText.Add(_lines[i]);
        }
        
        // Last line
        if (endLine < _lines.Count)
        {
            selectedText.Add(_lines[endLine].Substring(0, endCol));
        }
        
        return string.Join("\n", selectedText);
    }

    private void DeleteSelection()
    {
        if (!_hasSelection) return;
        
        int startLine = Math.Min(_selectionStartLine, _selectionEndLine);
        int endLine = Math.Max(_selectionStartLine, _selectionEndLine);
        int startCol = _selectionStartLine < _selectionEndLine ? _selectionStartColumn : _selectionEndColumn;
        int endCol = _selectionStartLine < _selectionEndLine ? _selectionEndColumn : _selectionStartColumn;
        
        if (startLine == endLine)
        {
            _lines[startLine] = _lines[startLine].Remove(startCol, endCol - startCol);
        }
        else
        {
            // Combine first and last line
            string newLine = _lines[startLine].Substring(0, startCol) + 
                           (endLine < _lines.Count ? _lines[endLine].Substring(endCol) : "");
            
            // Remove lines in between
            for (int i = endLine; i >= startLine; i--)
            {
                _lines.RemoveAt(i);
            }
            
            _lines.Insert(startLine, newLine);
        }
        
        _cursorLine = startLine;
        _cursorColumn = startCol;
        _hasSelection = false;
        
        UpdateTextFromLines();
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
        if (!IsVisible()) return;

        // Draw border (slightly dimmed if read-only)
        Color currentBorderColor = _isFocused ? _focusedBorderColor : _borderColor;
        if (ReadOnly)
        {
            currentBorderColor = Color.Lerp(currentBorderColor, Color.Gray, 0.3f);
        }
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
        
        // Draw text
        if (_wrappedLines.Count > 0)
        {
            DrawText(spriteBatch, textBounds);
        }
        else if (!string.IsNullOrEmpty(_placeholder))
        {
            DrawPlaceholder(spriteBatch, textBounds);
        }
        
        // Draw cursor (only if not read-only)
        if (_isFocused && _cursorVisible && !ReadOnly)
        {
            DrawCursor(spriteBatch, textBounds);
        }
    }

    private void DrawText(SpriteBatch spriteBatch, Rectangle textBounds)
    {
        int visibleLines = Math.Min(_maxVisibleLines, _wrappedLines.Count - _scrollOffsetY);
        
        for (int i = 0; i < visibleLines; i++)
        {
            int lineIndex = i + _scrollOffsetY;
            if (lineIndex >= _wrappedLines.Count) break;
            
            string line = _wrappedLines[lineIndex];
            var position = new Vector2(
                textBounds.X,
                textBounds.Y + (i * _lineHeight)
            );
            
            if (!string.IsNullOrEmpty(line))
            {
                Color displayColor = ReadOnly ? Color.Lerp(_textColor, Color.Gray, 0.2f) : _textColor;
                spriteBatch.DrawString(_font, line, position, displayColor, 0, Vector2.Zero, 1f, SpriteEffects.None, GetActualOrder() + 0.02f);
            }
        }
    }

    private void DrawPlaceholder(SpriteBatch spriteBatch, Rectangle textBounds)
    {
        var position = new Vector2(textBounds.X, textBounds.Y);
        spriteBatch.DrawString(_font, _placeholder, position, _placeholderColor, 0, Vector2.Zero, 1f, SpriteEffects.None, GetActualOrder() + 0.02f);
    }

    private void DrawCursor(SpriteBatch spriteBatch, Rectangle textBounds)
    {
        // Get cursor position in display coordinates (accounting for word wrapping)
        var displayPosition = GetCursorDisplayPosition();
        int displayLine = displayPosition.Line - _scrollOffsetY;
        
        if (displayLine >= 0 && displayLine < _maxVisibleLines)
        {
            var cursorPosition = new Vector2(
                textBounds.X + displayPosition.X,
                textBounds.Y + (displayLine * _lineHeight)
            );
            
            var cursorBounds = new Rectangle(
                (int)cursorPosition.X,
                (int)cursorPosition.Y,
                1,
                (int)_lineHeight
            );
            
            spriteBatch.Draw(_pixel, cursorBounds, null, _cursorColor, 0, Vector2.Zero, SpriteEffects.None, GetActualOrder() + 0.03f);
        }
    }

    private (int Line, float X) GetCursorDisplayPosition()
    {
        if (!_wordWrap)
        {
            // No wrapping - simple case
            string lineText = _lines[_cursorLine].Substring(0, _cursorColumn);
            float x = _font.MeasureString(lineText).X;
            return (_cursorLine, x);
        }
        
        // With word wrapping - need to find which wrapped line the cursor is on
        int wrappedLineIndex = 0;
        
        // Count wrapped lines for all lines before the cursor line
        for (int i = 0; i < _cursorLine; i++)
        {
            var wrappedLinesForThisLine = WrapLine(_lines[i], GetTextBounds().Width);
            wrappedLineIndex += wrappedLinesForThisLine.Count;
        }
        
        // Now find the cursor position within the current line's wrapped segments
        var currentLineWrapped = WrapLine(_lines[_cursorLine], GetTextBounds().Width);
        
        // Build a map of original character positions to wrapped segments
        var charToSegmentMap = BuildCharacterToSegmentMap(_lines[_cursorLine], currentLineWrapped);
        
        // Find which segment contains the cursor
        for (int i = 0; i < charToSegmentMap.Count; i++)
        {
            var segmentInfo = charToSegmentMap[i];
            if (_cursorColumn >= segmentInfo.StartChar && _cursorColumn <= segmentInfo.EndChar)
            {
                // Cursor is in this segment
                int cursorInSegment = _cursorColumn - segmentInfo.StartChar;
                string textToCursor = segmentInfo.Text.Substring(0, Math.Min(cursorInSegment, segmentInfo.Text.Length));
                float x = _font.MeasureString(textToCursor).X;
                return (wrappedLineIndex + i, x);
            }
        }
        
        // Cursor is at the end of the line
        if (currentLineWrapped.Count > 0)
        {
            string lastSegment = currentLineWrapped[currentLineWrapped.Count - 1];
            float x = _font.MeasureString(lastSegment).X;
            return (wrappedLineIndex + currentLineWrapped.Count - 1, x);
        }
        
        // Fallback
        return (wrappedLineIndex, 0f);
    }

    private struct SegmentInfo
    {
        public string Text;
        public int StartChar;
        public int EndChar;
    }

    private List<SegmentInfo> BuildCharacterToSegmentMap(string originalLine, List<string> wrappedSegments)
    {
        var segmentMap = new List<SegmentInfo>();
        int currentPos = 0;
        
        foreach (string segment in wrappedSegments)
        {
            int segmentStart = currentPos;
            int segmentEnd = currentPos + segment.Length;
            
            segmentMap.Add(new SegmentInfo
            {
                Text = segment,
                StartChar = segmentStart,
                EndChar = segmentEnd
            });
            
            // Move position forward by segment length plus one space (if not the last segment)
            currentPos = segmentEnd;
            if (currentPos < originalLine.Length && originalLine[currentPos] == ' ')
            {
                currentPos++; // Skip the space that caused the wrap
            }
        }
        
        return segmentMap;
    }

    public override Rectangle GetBoundingBox()
    {
        return _bounds;
    }

    public override void SetBounds(Rectangle bounds)
    {
        _bounds = bounds;
        CalculateMaxVisibleLines();
        UpdateWrappedLines();
    }

    // Public utility methods
    public void Clear()
    {
        if (ReadOnly) return;
        
        Text = string.Empty;
        _cursorLine = 0;
        _cursorColumn = 0;
        _scrollOffsetY = 0;
        _hasSelection = false;
    }

    public void SetCursorPosition(int line, int column)
    {
        _cursorLine = Math.Max(0, Math.Min(line, _lines.Count - 1));
        _cursorColumn = Math.Max(0, Math.Min(column, _lines[_cursorLine].Length));
        EnsureCursorVisible();
        _cursorBlinkTimer = 0f;
        _cursorVisible = true;
    }

    public (int Line, int Column) GetCursorPosition()
    {
        return (_cursorLine, _cursorColumn);
    }

    public void ScrollToLine(int lineNumber)
    {
        lineNumber = Math.Max(0, Math.Min(lineNumber, _lines.Count - 1));
        _scrollOffsetY = Math.Max(0, Math.Min(lineNumber, _lines.Count - _maxVisibleLines));
    }

    public int GetLineCount()
    {
        return _lines.Count;
    }

    public string GetLine(int lineNumber)
    {
        if (lineNumber >= 0 && lineNumber < _lines.Count)
        {
            return _lines[lineNumber];
        }
        return string.Empty;
    }

    /// <summary>
    /// Tests if the given text would be properly handled by the character filtering.
    /// Returns the filtered version of the text that would actually be displayed.
    /// Useful for debugging font compatibility issues.
    /// </summary>
    public string TestCharacterFiltering(string input)
    {
        return FilterUnsupportedCharacters(input);
    }

    /// <summary>
    /// Gets information about which characters in the input text are not supported by the current font.
    /// Returns a list of unsupported characters and their positions.
    /// </summary>
    public List<(char Character, int Position, string Suggestion)> GetUnsupportedCharacters(string input)
    {
        var unsupported = new List<(char, int, string)>();
        
        if (string.IsNullOrEmpty(input))
            return unsupported;
            
        for (int i = 0; i < input.Length; i++)
        {
            char c = input[i];
            if (!IsCharacterSupported(c))
            {
                string suggestion = c switch
                {
                    '\u2022' => "- (hyphen)",
                    '\u2013' or '\u2014' => "- (hyphen)",
                    '\u2018' or '\u2019' => "' (straight apostrophe)",
                    '\u201C' or '\u201D' => "\" (straight quotes)",
                    '\u2026' => "... (three dots)",
                    _ => "(character will be skipped)"
                };
                unsupported.Add((c, i, suggestion));
            }
        }
        
        return unsupported;
    }

    public void Dispose()
    {
        _pixel?.Dispose();
    }
}