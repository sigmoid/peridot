using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Peridot;
using Peridot.UI;

namespace Peridot;

public class DeveloperConsole
{
    Canvas _rootElement;
    TextInput _textInput;
    TextArea _outputArea;

    private List<ConsoleCommandHandler> _commands = new List<ConsoleCommandHandler>();

    public void Initialize()
    {
        var width = Core.GraphicsDevice.Viewport.Width;
        var height = Core.GraphicsDevice.Viewport.Height * 0.2f;
        var x = 0;
        var y = 0;

        _rootElement = new Canvas(new Rectangle(0, 0, width, (int)(height)));

        var layout = new VerticalLayoutGroup(new Rectangle(x, y, width, (int)(height)), 2);
        _outputArea = new TextArea(new Rectangle(0, 0, width, (int)(height)), Core.DefaultFont, wordWrap: true, readOnly: true, backgroundColor: new Color(0, 0, 0, 200), textColor: Color.Lime, borderColor: Color.Black);

        HorizontalLayoutGroup inputLayout = new HorizontalLayoutGroup(new Rectangle(0, (int)(height) - 30, width, 30), 0);
        {
            var label = new Label(new Rectangle(0, 0, 40, 30), ">>", Core.DefaultFont, backgroundColor: new Color(0, 0, 0, 200), textColor: Color.Lime);
            _textInput = new TextInput(new Rectangle(0, 0, width - 40, 30), Core.DefaultFont, "", backgroundColor: new Color(0, 0, 0, 200), textColor: Color.Lime, borderColor: Color.Black);
            _textInput.OnEnterPressed += (command) => HandleCommand(command);
            inputLayout.AddChild(label);
            inputLayout.AddChild(_textInput);
        }

        layout.AddChild(_outputArea);
        layout.AddChild(inputLayout);

        _rootElement.AddChild(layout);
        _rootElement.Order = 0.7f;
        _rootElement.SetVisibility(false);
    }

    public UIElement GetRootElement()
    {
        return _rootElement;
    }

    public void PrintLine(string text)
    {
        _outputArea.Text += text + "\n";
    }

    public TextArea GetOutputArea()
    {
        return _outputArea;
    }

    public void RegisterCommandHandler(ConsoleCommandHandler command)
    {
        if (command != null && !_commands.Exists(c => c.CommandName.Equals(command.CommandName, StringComparison.OrdinalIgnoreCase)))
        {
            _commands.Add(command);
        }
    }

    private void HandleCommand(string command)
    {
        PrintLine($">> {command}");

        var commandResult = ConsoleCommandParser.Parse(command);

        if (commandResult.Success)
        {
            var foundCommand = _commands.Where(x => x.CommandName.Equals(commandResult.CommandName, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
            if (foundCommand != null)
            {
                try
                {
                    foundCommand.Execute(commandResult.Args);
                    PrintLine("Command executed successfully.");
                }
                catch (Exception ex)
                {
                    PrintLine($"Error executing command: {ex.Message}");
                }
            }
            else
            {
                PrintLine($"Unknown command: {commandResult.CommandName}");
            }
        }
        else
        {
            PrintLine($"Error: {commandResult.ErrorMessage}");
        }

        _textInput.Clear();

        _outputArea.ScrollToEnd();
    }

    public void Toggle()
    {
        _rootElement.SetVisibility(!_rootElement.IsVisible());
        _textInput.SetFocus(_rootElement.IsVisible());
        _textInput.Text = "";
    }

}
