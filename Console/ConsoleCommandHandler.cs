using System;

namespace Peridot;

public class ConsoleCommandHandler
{
    public string CommandName;
    public DeveloperConsole Console => Core.DeveloperConsole;

    public ConsoleCommandHandler()
    {
    }

    public virtual void Execute(string[] args)
    {
        throw new NotImplementedException("Execute method must be implemented by subclasses.");
    }
}