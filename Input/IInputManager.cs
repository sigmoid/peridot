using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Peridot;
using Peridot.Testing;

public enum MouseButton
{
    Left,
    Right,
    Middle
}

public interface IInputManager
{

    void AddButton(string name, Keys key);

    void AddButton(string name, MouseButton mouseButton);

    void Update(GameTime gameTime);

    IInputButton GetButton(string name);

    List<IInputButton> GetAllButtons();
    Vector2 GetMousePosition();
}