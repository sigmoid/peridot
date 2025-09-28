using System;

namespace Peridot;

public class EchoCommandHandler : ConsoleCommandHandler
{
    public EchoCommandHandler()
    {
        CommandName = "echo";
    }

    public override void Execute(string[] args)
    {
        if (args.Length == 0)
        {
            throw new ArgumentException("No text provided to echo.");
        }

        string textToEcho = string.Join(" ", args);
        Console.PrintLine(textToEcho);
    }
}