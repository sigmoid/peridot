using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Peridot;
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
}