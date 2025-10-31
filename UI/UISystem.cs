using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Peridot;
using Peridot.UI;
using System;
using System.Runtime.InteropServices;

public class UISystem
{
    private List<UIElement> _elements;
    private string _fallbackClipboard = string.Empty;

    // Windows Clipboard API
    [DllImport("user32.dll")]
    private static extern bool OpenClipboard(IntPtr hWndNewOwner);

    [DllImport("user32.dll")]
    private static extern bool CloseClipboard();

    [DllImport("user32.dll")]
    private static extern bool EmptyClipboard();

    [DllImport("user32.dll")]
    private static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

    [DllImport("user32.dll")]
    private static extern IntPtr GetClipboardData(uint uFormat);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GlobalLock(IntPtr hMem);

    [DllImport("kernel32.dll")]
    private static extern bool GlobalUnlock(IntPtr hMem);

    [DllImport("kernel32.dll")]
    private static extern UIntPtr GlobalSize(IntPtr hMem);

    private const uint CF_UNICODETEXT = 13;
    private const uint GMEM_MOVEABLE = 0x0002;

    public UISystem()
    {
        _elements = new List<UIElement>();
    }

    // Clipboard functionality with Windows API
    public void SetClipboardText(string text)
    {
        try
        {
            if (string.IsNullOrEmpty(text))
                text = string.Empty;

            if (!OpenClipboard(IntPtr.Zero))
            {
                _fallbackClipboard = text;
                return;
            }

            EmptyClipboard();

            // Allocate memory for the text
            var textBytes = System.Text.Encoding.Unicode.GetBytes(text + '\0');
            var hMem = GlobalAlloc(GMEM_MOVEABLE, (UIntPtr)textBytes.Length);
            
            if (hMem == IntPtr.Zero)
            {
                CloseClipboard();
                _fallbackClipboard = text;
                return;
            }

            var pMem = GlobalLock(hMem);
            if (pMem != IntPtr.Zero)
            {
                Marshal.Copy(textBytes, 0, pMem, textBytes.Length);
                GlobalUnlock(hMem);
                SetClipboardData(CF_UNICODETEXT, hMem);
            }

            CloseClipboard();
        }
        catch
        {
            // Fallback to in-memory storage if clipboard access fails
            _fallbackClipboard = text;
        }
    }

    public string GetClipboardText()
    {
        try
        {
            if (!OpenClipboard(IntPtr.Zero))
            {
                return _fallbackClipboard;
            }

            var hData = GetClipboardData(CF_UNICODETEXT);
            if (hData == IntPtr.Zero)
            {
                CloseClipboard();
                return _fallbackClipboard;
            }

            var pData = GlobalLock(hData);
            if (pData == IntPtr.Zero)
            {
                CloseClipboard();
                return _fallbackClipboard;
            }

            var dataSize = GlobalSize(hData);
            var buffer = new byte[dataSize.ToUInt32()];
            Marshal.Copy(pData, buffer, 0, buffer.Length);
            GlobalUnlock(hData);
            CloseClipboard();

            // Convert from Unicode and remove null terminator
            var text = System.Text.Encoding.Unicode.GetString(buffer);
            var nullIndex = text.IndexOf('\0');
            if (nullIndex >= 0)
                text = text.Substring(0, nullIndex);

            return text;
        }
        catch
        {
            // Fallback to in-memory storage if clipboard access fails
            return _fallbackClipboard;
        }
    }

    public bool IsCtrlPressed()
    {
        var keyboardState = Keyboard.GetState();
        return keyboardState.IsKeyDown(Keys.LeftControl) || keyboardState.IsKeyDown(Keys.RightControl);
    }

    public bool IsSystemClipboardAvailable()
    {
        try
        {
            return OpenClipboard(IntPtr.Zero) && CloseClipboard();
        }
        catch
        {
            return false;
        }
    }

    public void AddElement(UIElement element)
    {
        _elements.Add(element);
    }

    public void RemoveElement(UIElement element)
    {
        _elements.Remove(element);
    }

    public void Update(float deltaTime)
    {
        var elementsToUpdate = _elements.ToList();
        foreach (var element in elementsToUpdate)
        {
            if (_elements.Contains(element) && element.IsVisible())
            {
                element.Update(deltaTime);
            }
        }
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        var elementsToDraw = _elements.ToList();
        
        // Filter visible elements and sort them manually by their order
        var visibleElements = elementsToDraw
            .Where(element => _elements.Contains(element) && element.IsVisible())
            .OrderBy(element => element.GetActualOrder())
            .ToList();
        
        // Draw elements in sorted order (back to front)
        foreach (var element in visibleElements)
        {
            element.Draw(spriteBatch);
        }
    }

    /// <summary>
    /// Checks if the mouse is currently over any visible UI element.
    /// This is used by the input manager to determine if UI should block mouse inputs.
    /// </summary>
    public bool IsMouseOverUI()
    {
        var mouseState = Mouse.GetState();
        var mousePosition = new Vector2(mouseState.X, mouseState.Y);
        return IsPositionOverUI(mousePosition);
    }

    /// <summary>
    /// Checks if a specific position is over any visible UI element.
    /// </summary>
    public bool IsPositionOverUI(Vector2 position)
    {
        var elementsToCheck = _elements.ToList();
        
        // Check elements in reverse order (front to back) since the topmost element should take priority
        var visibleElements = elementsToCheck
            .Where(element => _elements.Contains(element) && element.IsVisible())
            .OrderByDescending(element => element.GetActualOrder())
            .ToList();

        foreach (var element in visibleElements)
        {
            if (IsElementAtPosition(element, position))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if any UI element is currently consuming keyboard input.
    /// This includes focused text inputs and other elements that need exclusive keyboard access.
    /// </summary>
    public bool IsUIConsumingKeyboard()
    {
        var elementsToCheck = _elements.ToList();

        foreach (var element in elementsToCheck)
        {
            if (!_elements.Contains(element) || !element.IsVisible()) continue;

            // Check if this element is consuming keyboard input
            if (IsElementConsumingKeyboard(element))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Gets the UI element at the specified position, if any.
    /// Returns the topmost (highest order) element at that position.
    /// </summary>
    public UIElement GetElementAtPosition(Vector2 position)
    {
        var elementsToCheck = _elements.ToList();
        
        // Check elements in reverse order (front to back) since the topmost element should take priority
        var visibleElements = elementsToCheck
            .Where(element => _elements.Contains(element) && element.IsVisible())
            .OrderByDescending(element => element.GetActualOrder())
            .ToList();

        foreach (var element in visibleElements)
        {
            if (IsElementAtPosition(element, position))
            {
                return element;
            }
        }

        return null;
    }

    /// <summary>
    /// Recursively checks if an element or any of its children contain the specified position.
    /// </summary>
    private bool IsElementAtPosition(UIElement element, Vector2 position)
    {
        // Check if the position is within this element's bounds
        if (element.GetBoundingBox().Contains(position))
        {
            return true;
        }

        // If this element is a container, check its children as well
        if (element is UIContainer container)
        {
            foreach (var child in container.Children)
            {
                if (child.IsVisible() && IsElementAtPosition(child, position))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Recursively checks if an element or any of its children are consuming keyboard input.
    /// </summary>
    private bool IsElementConsumingKeyboard(UIElement element)
    {
        // Check specific element types that consume keyboard input
        switch (element)
        {
            case TextInput textInput:
                return textInput.IsFocused;
            
            case TextArea textArea:
                return textArea.IsFocused;
            
            // Add other keyboard-consuming elements here as needed
            // For example:
            // case SearchBox searchBox:
            //     return searchBox.IsFocused;
        }

        // If this element is a container, check its children recursively
        if (element is UIContainer container)
        {
            foreach (var child in container.Children)
            {
                if (child.IsVisible() && IsElementConsumingKeyboard(child))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if the UI should block all input (both mouse and keyboard).
    /// This is a convenience method that combines mouse and keyboard checks.
    /// </summary>
    public bool ShouldBlockInput()
    {
        return IsMouseOverUI() || IsUIConsumingKeyboard();
    }

    /// <summary>
    /// Checks if the UI should block mouse input specifically.
    /// </summary>
    public bool ShouldBlockMouseInput()
    {
        return IsMouseOverUI();
    }

    /// <summary>
    /// Checks if the UI should block keyboard input specifically.
    /// </summary>
    public bool ShouldBlockKeyboardInput()
    {
        return IsUIConsumingKeyboard();
    }
}