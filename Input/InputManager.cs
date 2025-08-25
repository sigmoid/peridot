using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Peridot;
using Peridot.Testing.Input;

public class InputManager : IInputManager
{
    private Dictionary<string, IInputButton> _buttons;


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

    public void Update(GameTime gameTime)
    {
        if( _buttons == null || _buttons.Count == 0) return;
        foreach (var button in _buttons.Values)
        {
            button.Update(gameTime);
            RecordInput(button, gameTime);
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
}