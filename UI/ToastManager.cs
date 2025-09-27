namespace Peridot.UI;

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

/// <summary>
/// Manages multiple toast notifications, handling positioning, stacking, and lifecycle.
/// Prevents toasts from overlapping and provides queue management.
/// 
/// Example usage:
/// 
/// var toastManager = new ToastManager(uiSystem, screenBounds);
/// 
/// // Show toasts through the manager
/// toastManager.ShowInfo("Operation completed!");
/// toastManager.ShowError("Connection failed!");
/// toastManager.ShowSuccess("File saved successfully!");
/// </summary>
public class ToastManager
{
    private readonly UISystem _uiSystem;
    private readonly SpriteFont _defaultFont;
    private Rectangle _screenBounds;
    private readonly List<Toast> _activeToasts;
    private readonly Dictionary<Toast.ToastPosition, List<Toast>> _toastsByPosition;
    private readonly List<Toast> _pendingRemovalToasts;
    private bool _isUpdating = false;
    private const int TOAST_SPACING = 10;

    /// <summary>
    /// Gets or sets the default font used for toasts.
    /// </summary>
    public SpriteFont DefaultFont { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of toasts that can be displayed at once.
    /// </summary>
    public int MaxToasts { get; set; } = 5;

    /// <summary>
    /// Gets or sets whether old toasts should be automatically dismissed when the maximum is reached.
    /// </summary>
    public bool AutoDismissOldToasts { get; set; } = true;

    public ToastManager(UISystem uiSystem, SpriteFont defaultFont, Rectangle screenBounds)
    {
        _uiSystem = uiSystem ?? throw new ArgumentNullException(nameof(uiSystem));
        _defaultFont = defaultFont ?? throw new ArgumentNullException(nameof(defaultFont));
        _screenBounds = screenBounds;
        _activeToasts = new List<Toast>();
        _toastsByPosition = new Dictionary<Toast.ToastPosition, List<Toast>>();
        _pendingRemovalToasts = new List<Toast>();

        // Initialize position lists
        foreach (Toast.ToastPosition position in Enum.GetValues<Toast.ToastPosition>())
        {
            _toastsByPosition[position] = new List<Toast>();
        }
    }

    /// <summary>
    /// Updates screen bounds (call when screen is resized).
    /// </summary>
    public void UpdateScreenBounds(Rectangle newScreenBounds)
    {
        _screenBounds = newScreenBounds;
        
        // Update all active toasts
        foreach (var toast in _activeToasts)
        {
            toast.UpdateScreenBounds(newScreenBounds);
        }
        
        // Recalculate positions
        RecalculateToastPositions();
    }

    /// <summary>
    /// Updates all managed toasts and cleans up finished ones.
    /// Note: Individual toasts are already updated by the UISystem, so we only need to handle cleanup here.
    /// </summary>
    public void Update(float deltaTime)
    {
        _isUpdating = true;
        
        // Process any pending removals from the previous frame
        ProcessPendingRemovals();
        
        // Check for finished toasts and mark them for removal
        // Use ToList() to avoid collection modification during iteration
        var finishedToasts = _activeToasts.Where(t => t.IsFinished).ToList();
        foreach (var toast in finishedToasts)
        {
            // Queue for removal instead of removing immediately
            if (!_pendingRemovalToasts.Contains(toast))
            {
                _pendingRemovalToasts.Add(toast);
            }
        }
        
        _isUpdating = false;
        
        // Process removals now that we're not iterating
        ProcessPendingRemovals();
    }

    private void ProcessPendingRemovals()
    {
        if (_pendingRemovalToasts.Count > 0)
        {
            var toastsToRemove = _pendingRemovalToasts.ToList();
            _pendingRemovalToasts.Clear();
            
            foreach (var toast in toastsToRemove)
            {
                if (_activeToasts.Contains(toast))
                {
                    RemoveToastInternal(toast);
                }
            }
        }
    }

    private void AddToast(Toast toast, Toast.ToastPosition position)
    {
        // Check if we need to dismiss old toasts
        if (_activeToasts.Count >= MaxToasts && AutoDismissOldToasts)
        {
            var oldestToast = _activeToasts.FirstOrDefault();
            if (oldestToast != null)
            {
                oldestToast.Dismiss();
            }
        }

        // Add to collections
        _activeToasts.Add(toast);
        _toastsByPosition[position].Add(toast);

        // Set up toast finished event
        toast.OnToastFinished += () => RemoveToast(toast);

        // Add to UI system
        _uiSystem.AddElement(toast);

        // Recalculate positions for this position group
        RecalculatePositionGroup(position);
    }

    private void RemoveToast(Toast toast)
    {
        if (_isUpdating)
        {
            // If we're currently updating, queue for removal instead
            if (!_pendingRemovalToasts.Contains(toast))
            {
                _pendingRemovalToasts.Add(toast);
            }
        }
        else
        {
            RemoveToastInternal(toast);
        }
    }

    private void RemoveToastInternal(Toast toast)
    {
        _activeToasts.Remove(toast);
        _uiSystem.RemoveElement(toast);

        // Remove from position-specific lists and recalculate
        foreach (var kvp in _toastsByPosition)
        {
            if (kvp.Value.Remove(toast))
            {
                RecalculatePositionGroup(kvp.Key);
                break;
            }
        }

        toast.Dispose();
    }

    private void RecalculateToastPositions()
    {
        foreach (Toast.ToastPosition position in Enum.GetValues<Toast.ToastPosition>())
        {
            RecalculatePositionGroup(position);
        }
    }

    private void RecalculatePositionGroup(Toast.ToastPosition position)
    {
        var toasts = _toastsByPosition[position];
        if (toasts.Count == 0) return;

        // Calculate stacking based on position
        bool stackVertically = IsVerticalPosition(position);
        bool stackFromTop = IsTopPosition(position);
        bool stackFromLeft = IsLeftPosition(position);

        for (int i = 0; i < toasts.Count; i++)
        {
            var toast = toasts[i];
            var bounds = toast.GetBoundingBox();
            
            if (stackVertically)
            {
                // Stack vertically
                int yOffset = 0;
                if (stackFromTop)
                {
                    // Stack downward from top
                    for (int j = 0; j < i; j++)
                    {
                        yOffset += toasts[j].GetBoundingBox().Height + TOAST_SPACING;
                    }
                }
                else
                {
                    // Stack upward from bottom
                    for (int j = i + 1; j < toasts.Count; j++)
                    {
                        yOffset -= toasts[j].GetBoundingBox().Height + TOAST_SPACING;
                    }
                }

                var baseX = GetBaseX(position, bounds.Width);
                var baseY = GetBaseY(position, bounds.Height);
                var newBounds = new Rectangle(baseX, baseY + yOffset, bounds.Width, bounds.Height);
                toast.SetBounds(newBounds);
            }
            else
            {
                // Stack horizontally
                int xOffset = 0;
                if (stackFromLeft)
                {
                    // Stack rightward from left
                    for (int j = 0; j < i; j++)
                    {
                        xOffset += toasts[j].GetBoundingBox().Width + TOAST_SPACING;
                    }
                }
                else
                {
                    // Stack leftward from right
                    for (int j = i + 1; j < toasts.Count; j++)
                    {
                        xOffset -= toasts[j].GetBoundingBox().Width + TOAST_SPACING;
                    }
                }

                var baseX = GetBaseX(position, bounds.Width);
                var baseY = GetBaseY(position, bounds.Height);
                var newBounds = new Rectangle(baseX + xOffset, baseY, bounds.Width, bounds.Height);
                toast.SetBounds(newBounds);
            }
        }
    }

    private bool IsVerticalPosition(Toast.ToastPosition position)
    {
        return position == Toast.ToastPosition.TopLeft || position == Toast.ToastPosition.TopCenter || 
               position == Toast.ToastPosition.TopRight || position == Toast.ToastPosition.BottomLeft || 
               position == Toast.ToastPosition.BottomCenter || position == Toast.ToastPosition.BottomRight;
    }

    private bool IsTopPosition(Toast.ToastPosition position)
    {
        return position == Toast.ToastPosition.TopLeft || position == Toast.ToastPosition.TopCenter || 
               position == Toast.ToastPosition.TopRight;
    }

    private bool IsLeftPosition(Toast.ToastPosition position)
    {
        return position == Toast.ToastPosition.TopLeft || position == Toast.ToastPosition.MiddleLeft || 
               position == Toast.ToastPosition.BottomLeft;
    }

    private int GetBaseX(Toast.ToastPosition position, int toastWidth = 0)
    {
        return position switch
        {
            Toast.ToastPosition.TopLeft or Toast.ToastPosition.MiddleLeft or Toast.ToastPosition.BottomLeft => 
                _screenBounds.X + 20,
            Toast.ToastPosition.TopRight or Toast.ToastPosition.MiddleRight or Toast.ToastPosition.BottomRight => 
                _screenBounds.X + _screenBounds.Width - toastWidth - 20,
            _ => _screenBounds.X + (_screenBounds.Width - toastWidth) / 2  // Center positions
        };
    }

    private int GetBaseY(Toast.ToastPosition position, int toastHeight = 0)
    {
        return position switch
        {
            Toast.ToastPosition.TopLeft or Toast.ToastPosition.TopCenter or Toast.ToastPosition.TopRight => 
                _screenBounds.Y + 20,
            Toast.ToastPosition.BottomLeft or Toast.ToastPosition.BottomCenter or Toast.ToastPosition.BottomRight => 
                _screenBounds.Y + _screenBounds.Height - toastHeight - 20,
            _ => _screenBounds.Y + (_screenBounds.Height - toastHeight) / 2  // Middle positions
        };
    }

    // Convenience methods for showing toasts

    /// <summary>
    /// Shows an info toast with default settings.
    /// </summary>
    public Toast ShowInfo(string message, float duration = 3f, Toast.ToastPosition position = Toast.ToastPosition.BottomRight)
    {
        var toast = Toast.ShowInfo(message, _defaultFont, _screenBounds, duration, position);
        AddToast(toast, position);
        return toast;
    }

    /// <summary>
    /// Shows a success toast with default settings.
    /// </summary>
    public Toast ShowSuccess(string message, float duration = 3f, Toast.ToastPosition position = Toast.ToastPosition.BottomRight)
    {
        var toast = Toast.ShowSuccess(message, _defaultFont, _screenBounds, duration, position);
        AddToast(toast, position);
        return toast;
    }

    /// <summary>
    /// Shows a warning toast with default settings.
    /// </summary>
    public Toast ShowWarning(string message, float duration = 4f, Toast.ToastPosition position = Toast.ToastPosition.BottomRight)
    {
        var toast = Toast.ShowWarning(message, _defaultFont, _screenBounds, duration, position);
        AddToast(toast, position);
        return toast;
    }

    /// <summary>
    /// Shows an error toast with default settings.
    /// </summary>
    public Toast ShowError(string message, float duration = 5f, Toast.ToastPosition position = Toast.ToastPosition.BottomRight)
    {
        var toast = Toast.ShowError(message, _defaultFont, _screenBounds, duration, position);
        AddToast(toast, position);
        return toast;
    }

    /// <summary>
    /// Shows a custom toast with full control over appearance.
    /// </summary>
    public Toast ShowCustom(string message, Toast.ToastType type, float duration = 3f, 
                          Toast.ToastPosition position = Toast.ToastPosition.BottomRight, Vector2? offset = null)
    {
        var toast = Toast.Show(message, _defaultFont, _screenBounds, type, duration, position, offset);
        AddToast(toast, position);
        return toast;
    }

    /// <summary>
    /// Dismisses all active toasts.
    /// </summary>
    public void DismissAll()
    {
        foreach (var toast in _activeToasts.ToList())
        {
            toast.Dismiss();
        }
    }

    /// <summary>
    /// Immediately hides all active toasts.
    /// </summary>
    public void HideAll()
    {
        foreach (var toast in _activeToasts.ToList())
        {
            toast.Hide();
        }
    }

    /// <summary>
    /// Gets the number of currently active toasts.
    /// </summary>
    public int ActiveToastCount => _activeToasts.Count;

    /// <summary>
    /// Gets all currently active toasts.
    /// </summary>
    public IReadOnlyList<Toast> ActiveToasts => _activeToasts.AsReadOnly();
}