using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Peridot;

public class UISystem
{
    private List<IUIElement> _elements;

    public UISystem()
    {
        _elements = new List<IUIElement>();
    }

    public void AddElement(IUIElement element)
    {
        _elements.Add(element);
    }

    public void RemoveElement(IUIElement element)
    {
        _elements.Remove(element);
    }

    public void Update(float deltaTime)
    {
        foreach (var element in _elements)
        {
            element.Update(deltaTime);
        }
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        foreach (var element in _elements)
        {
            element.Draw(spriteBatch);
        }
    }
}