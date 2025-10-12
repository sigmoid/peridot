using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Peridot;
using Peridot.Input;
using Peridot.Testing.Input;

public class InputManager : IInputManager
{
    private Dictionary<string, IInputButton> _buttons;
    private UISystem _uiSystem;


    public Vector2 GetMousePosition()
    { 
        var mouseState = Mouse.GetState();
        return new Vector2(mouseState.X / (float)Core.ScreenWidth, mouseState.Y / (float)Core.ScreenHeight);
    }

    public void AddButton(string name, Keys key)
    {
        var button = new InputButton(name, key);
        if (_buttons == null)
        {
            _buttons = new Dictionary<string, IInputButton>();
        }
        _buttons[name] = button;
    }

    /// <summary>
    /// Sets the UI system reference for input blocking functionality.
    /// Call this during initialization to enable UI input blocking.
    /// </summary>
    public void SetUISystem(UISystem uiSystem)
    {
        _uiSystem = uiSystem;
    }

    public void Update(GameTime gameTime)
    {
        if( _buttons == null || _buttons.Count == 0) return;

        // Check if UI should block input
        bool shouldBlockKeyboard = _uiSystem?.ShouldBlockKeyboardInput() ?? false;
        bool shouldBlockMouse = _uiSystem?.ShouldBlockMouseInput() ?? false;

        foreach (var button in _buttons.Values)
        {
            // Check if this button should be blocked by UI
            bool shouldBlockThisButton = ShouldBlockButton(button, shouldBlockKeyboard, shouldBlockMouse);
            
            if (!shouldBlockThisButton)
            {
                button.Update(gameTime);
                RecordInput(button, gameTime);
            }
            else
            {
                // For blocked buttons, we still need to update their state
                // but we don't want to trigger pressed/released events
                UpdateButtonStateOnly(button, gameTime);
            }
        }
    }

    public IInputButton GetButton(string name)
    {
        if (_buttons.TryGetValue(name, out var button))
        {
            return button;
        }
        return null;
    }

    public List<IInputButton> GetAllButtons()
    {
        return new List<IInputButton>(_buttons.Values);
    }

    private void RecordInput(IInputButton button, GameTime gameTime)
    {
        if (button.IsPressed || button.IsReleased)
        {
            Core.TestRecorder.RecordMoment(new InputMoment
            {
                ButtonName = button.Name,
                ActionType = button.IsPressed ? InputActionType.Pressed : InputActionType.Released,
                Timestamp = gameTime.TotalGameTime.TotalMilliseconds
            });
        }
    }

    public void AddButton(string name, MouseButton mouseButton)
    {
        var button = new MouseInputButton(name, mouseButton);
        if (_buttons == null)
        {
            _buttons = new Dictionary<string, IInputButton>();
        }
        _buttons[name] = button;
    }

    /// <summary>
    /// Determines if a specific button should be blocked based on UI state.
    /// </summary>
    private bool ShouldBlockButton(IInputButton button, bool shouldBlockKeyboard, bool shouldBlockMouse)
    {
        // Check if this is a mouse button
        if (button is MouseInputButton)
        {
            return shouldBlockMouse;
        }
        
        // For keyboard buttons, block if UI is consuming keyboard input
        return shouldBlockKeyboard;
    }

    /// <summary>
    /// Updates button state without triggering input events.
    /// This prevents blocked buttons from firing pressed/released events while maintaining state consistency.
    /// </summary>
    private void UpdateButtonStateOnly(IInputButton button, GameTime gameTime)
    {
        // We need to update the internal state but prevent events
        // This is a bit tricky since we don't have access to the internal state of InputButton
        // For now, we'll just skip the update entirely for blocked buttons
        // In a more sophisticated implementation, you might want to modify InputButton 
        // to support "silent" updates that maintain state without firing events
    }
}