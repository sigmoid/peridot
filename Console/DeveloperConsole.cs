using System.Numerics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Peridot;
using Peridot.UI;

public class DeveloperConsole
{
    Canvas _rootElement;

    public void Initialize()
    {
        var width = Core.GraphicsDevice.Viewport.Width;
        var height = Core.GraphicsDevice.Viewport.Height * 0.2f;
        var x = 0;
        var y = 0;

        _rootElement = new Canvas(new Rectangle(0, 0, width, (int)(height)));

        var layout = new VerticalLayoutGroup(new Rectangle(x, y, width, (int)(height)), 5);
        var textArea = new TextArea(new Rectangle(0, 0, width, (int)(height)), Core.DefaultFont, "", readOnly: true, backgroundColor: new Color(0, 0, 0, 200), textColor: Color.Lime, borderColor: Color.Black);

        HorizontalLayoutGroup inputLayout = new HorizontalLayoutGroup(new Rectangle(0, (int)(height) - 30, width, 30), 5);
        {
            var label = new Label(new Rectangle(0, 0, 80, 30), ">>>:", Core.DefaultFont, backgroundColor: new Color(0, 0, 0, 200), textColor: Color.Lime);
            var textInput = new TextInput(new Rectangle(0, 0, width - 80, 30), Core.DefaultFont, "", backgroundColor: new Color(0, 0, 0, 200), textColor: Color.Lime, borderColor: Color.Black);
            inputLayout.AddChild(label);
            inputLayout.AddChild(textInput);
        }

        layout.AddChild(textArea);
        layout.AddChild(inputLayout);

        _rootElement.AddChild(layout);
        _rootElement.Order = 0.9f;
        _rootElement.SetVisibility(false);
    }
    
    public UIElement GetRootElement()
    {
        return _rootElement;
    }
}