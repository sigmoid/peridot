using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

public interface IInputButton
{ 
    string Name { get; set; }

    bool IsPressed { get; }
    bool IsReleased { get; }
    bool IsHeld { get; }    

    void Update(GameTime gameTime);
}