using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public interface IUIElement
{
    void Update(float deltaTime);
    void Draw(SpriteBatch spriteBatch);
    Rectangle GetBoundingBox();
    void SetBounds(Rectangle bounds);
}