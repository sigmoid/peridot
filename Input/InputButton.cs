using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

public class InputButton : IInputButton
{
    public string Name { get; set; }
    public Keys Key { get; set; }

    public virtual bool IsPressed => _currentState && !_previousState;
    public virtual bool IsReleased => !_currentState && _previousState;
    public virtual bool IsHeld => _currentState;

    private bool _previousState;
    private bool _currentState;

    public InputButton(string name, Keys key)
    {
        Name = name;
        Key = key;
    }

    public virtual void Update(GameTime gameTime)
    {
        _previousState = _currentState;
        _currentState = Keyboard.GetState().IsKeyDown(Key);
    }
}

public class MouseInputButton : IInputButton
{
    public string Name { get; set; }
    public MouseButton Button { get; set; }

    public virtual bool IsPressed => _currentState && !_previousState;
    public virtual bool IsReleased => !_currentState && _previousState;
    public virtual bool IsHeld => _currentState;

    private bool _previousState;
    private bool _currentState;

    public MouseInputButton(string name, MouseButton button)
    {
        Name = name;
        Button = button;
    }

    public virtual void Update(GameTime gameTime)
    {
        _previousState = _currentState;
        var mouseState = Mouse.GetState();
        _currentState = Button switch
        {
            MouseButton.Left => mouseState.LeftButton == ButtonState.Pressed,
            MouseButton.Right => mouseState.RightButton == ButtonState.Pressed,
            MouseButton.Middle => mouseState.MiddleButton == ButtonState.Pressed,
            _ => false
        };
    }
}