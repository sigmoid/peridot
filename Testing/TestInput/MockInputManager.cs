using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Peridot.Testing.Input;

/// <summary>
/// Mock input manager that replays recorded input during test playback
/// </summary>
public class MockInputManager : IInputManager
{
    private readonly List<InputMoment> _inputMoments;
    private readonly Dictionary<string, IMockInputButton> _mockButtons;
    private double _currentTime;
    private int _nextInputIndex;
    private bool _isActive;
    private float _startTime;

    public MockInputManager(List<InputMoment> inputMoments, List<string> buttons)
    {
        _inputMoments = inputMoments ?? new List<InputMoment>();
        _mockButtons = new Dictionary<string, IMockInputButton>();
        _currentTime = 0;
        _nextInputIndex = 0;
        _isActive = true;


        // Pre-create buttons for all input moments
        foreach (var buttonName in buttons)
        {
            if (!_mockButtons.ContainsKey(buttonName))
            {
                _mockButtons[buttonName] = new MockInputButton(buttonName);
            }
        }
    }

    public void Start(float startTime)
    { 
        _startTime = startTime;
        _currentTime = startTime;
        _nextInputIndex = 0;
        _isActive = true;

    }

    public Vector2 GetMousePosition()
    {
        return Vector2.Zero;
    }

    public void AddButton(string name, Keys key)
    {
        if (!_mockButtons.ContainsKey(name))
        {
            _mockButtons[name] = new MockInputButton(name, key);
        }
    }
    
    public void AddButton(string name, MouseButton mouseButton)
    {
        if (!_mockButtons.ContainsKey(name))
        {
            _mockButtons[name] = new MockInputMouseButton(name, mouseButton);
        }
    }

    public void Update(GameTime gameTime)
    {
        _currentTime += gameTime.ElapsedGameTime.TotalSeconds;

        // Process all input moments that should have occurred by now
        while (_nextInputIndex < _inputMoments.Count &&
               _inputMoments[_nextInputIndex].Timestamp <= _currentTime)
        {
            var moment = _inputMoments[_nextInputIndex];


            if (_mockButtons.ContainsKey(moment.ButtonName))
            {
                var button = _mockButtons[moment.ButtonName];
                if (moment.ActionType == InputActionType.Pressed)
                {
                    button.SetPressed();
                }
                else if (moment.ActionType == InputActionType.Released)
                {
                    button.SetReleased();
                }
            }

            _nextInputIndex++;
        }

        // Update all buttons
        foreach (var button in _mockButtons.Values)
        {
            button.Update(gameTime);
        }
    }

    public IInputButton GetButton(string name)
    {
        if (_mockButtons.ContainsKey(name))
        {
            return _mockButtons[name];
        }
        throw new Exception("Button not found: " + name);
    }

    public List<IInputButton> GetAllButtons()
    {
        return _mockButtons.Values.ToList<IInputButton>();
    }

    public bool IsActive => _isActive;
    public double CurrentTime => _currentTime;
    public bool HasMoreInput => _nextInputIndex < _inputMoments.Count;
}

public interface IMockInputButton : IInputButton
{
    void SetPressed();
    void SetReleased();
}

/// <summary>
/// Mock implementation of InputButton for test playback
/// </summary>
public class MockInputButton : InputButton, IMockInputButton
{
    private bool _currentState;
    private bool _previousState;

    public MockInputButton(string name, Keys key = Keys.None) : base(name, key)
    {
        _currentState = false;
        _previousState = false;
    }

    public override bool IsPressed => _currentState && !_previousState;
    public override bool IsReleased => !_currentState && _previousState;
    public override bool IsHeld => _currentState;

    public void SetPressed()
    {
        _currentState = true;
    }

    public void SetReleased()
    {
        _currentState = false;
    }

    public override void Update(GameTime gameTime)
    {
        _previousState = _currentState;
    }
}

/// <summary>
/// Mock implementation of InputButton for test playback
/// </summary>
public class MockInputMouseButton : MouseInputButton, IMockInputButton
{
    private bool _currentState;
    private bool _previousState;

    public MockInputMouseButton(string name, MouseButton button) : base(name, button)
    {
        _currentState = false;
        _previousState = false;
    }

    public override bool IsPressed => _currentState && !_previousState;
    public override bool IsReleased => !_currentState && _previousState;
    public override bool IsHeld => _currentState;

    public void SetPressed()
    {
        _currentState = true;
    }

    public void SetReleased()
    {
        _currentState = false;
    }

    public override void Update(GameTime gameTime)
    {
        _previousState = _currentState;
    }
}
