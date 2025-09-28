using System;
using System.Linq;

namespace Peridot;

public class ConsoleCommandParseResult
{
    public bool Success { get; set; }
    public string ErrorMessage { get; set; }
    public string CommandName { get; set; }
    public string[] Args { get; set; }

    public ConsoleCommandParseResult(bool success, string errorMessage = null, string commandName = null, string[] args = null)
    {
        Success = success;
        ErrorMessage = errorMessage;
        CommandName = commandName;
        Args = args;
    }
}

public class ConsoleCommandParser
{
    public static ConsoleCommandParseResult Parse(string command)
    {
        try
        {
            var parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
                return new ConsoleCommandParseResult(false, "No command entered.");

            var cmd = parts[0].ToLower();
            var args = parts.Skip(1).ToArray();
            return new ConsoleCommandParseResult(true, commandName: cmd, args: args);
        }
        catch (Exception ex)
        {
            return new ConsoleCommandParseResult(false, $"Error parsing command: {ex.Message}");
        }
    }
}