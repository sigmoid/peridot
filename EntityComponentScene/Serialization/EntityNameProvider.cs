using System;
using System.Collections.Generic;
using System.Text;

namespace Peridot.EntityComponentScene.Serialization;
/// <summary>
/// Provides unique names for entities in the system
/// </summary>
public class EntityNameProvider
{
    private static readonly HashSet<string> _usedNames = new HashSet<string>();
    private static readonly Dictionary<string, int> _nameCounters = new Dictionary<string, int>();
    private static readonly object _lock = new object();

    /// <summary>
    /// Gets a unique name for an entity. If the provided name is already taken,
    /// it will append a number to make it unique.
    /// </summary>
    /// <param name="desiredName">The desired name for the entity</param>
    /// <returns>A unique name for the entity</returns>
    public static string GetUniqueName(string desiredName)
    {
        lock (_lock)
        {
            if (string.IsNullOrWhiteSpace(desiredName))
            {
                desiredName = "Entity";
            }

            // Clean the name to remove any invalid characters
            desiredName = CleanName(desiredName);

            // If the name is not taken, use it as is
            if (!_usedNames.Contains(desiredName))
            {
                _usedNames.Add(desiredName);
                return desiredName;
            }

            // If the name is taken, find a unique variation
            if (!_nameCounters.ContainsKey(desiredName))
            {
                _nameCounters[desiredName] = 1;
            }

            string uniqueName;
            do
            {
                _nameCounters[desiredName]++;
                uniqueName = $"{desiredName}_{_nameCounters[desiredName]}";
            } while (_usedNames.Contains(uniqueName));

            _usedNames.Add(uniqueName);
            return uniqueName;
        }
    }

    /// <summary>
    /// Generates a unique name based on a base name and type
    /// </summary>
    /// <param name="baseName">The base name to use</param>
    /// <param name="typeName">The type name to use if baseName is empty</param>
    /// <returns>A unique name for the entity</returns>
    public static string GenerateUniqueNameFromType(string baseName, string typeName)
    {
        if (string.IsNullOrWhiteSpace(baseName))
        {
            baseName = string.IsNullOrWhiteSpace(typeName) ? "Entity" : typeName;
        }

        return GetUniqueName(baseName);
    }

    /// <summary>
    /// Releases a name back to the pool, allowing it to be reused
    /// </summary>
    /// <param name="name">The name to release</param>
    public static void ReleaseName(string name)
    {
        lock (_lock)
        {
            _usedNames.Remove(name);
        }
    }

    /// <summary>
    /// Clears all tracked names. Use with caution - typically only needed for testing
    /// </summary>
    public static void ClearAllNames()
    {
        lock (_lock)
        {
            _usedNames.Clear();
            _nameCounters.Clear();
        }
    }

    /// <summary>
    /// Checks if a name is available
    /// </summary>
    /// <param name="name">The name to check</param>
    /// <returns>True if the name is available, false otherwise</returns>
    public static bool IsNameAvailable(string name)
    {
        lock (_lock)
        {
            return !_usedNames.Contains(name);
        }
    }

    /// <summary>
    /// Gets all currently used names
    /// </summary>
    /// <returns>A copy of all used names</returns>
    public static HashSet<string> GetUsedNames()
    {
        lock (_lock)
        {
            return new HashSet<string>(_usedNames);
        }
    }

    /// <summary>
    /// Cleans a name by removing invalid characters and ensuring it's suitable for use
    /// </summary>
    /// <param name="name">The name to clean</param>
    /// <returns>A cleaned name</returns>
    private static string CleanName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "Entity";
        }

        var sb = new StringBuilder();
        bool firstChar = true;

        foreach (char c in name)
        {
            if (char.IsLetterOrDigit(c) || c == '_')
            {
                // First character must be a letter or underscore
                if (firstChar && char.IsDigit(c))
                {
                    sb.Append('_');
                }
                sb.Append(c);
                firstChar = false;
            }
            else if (c == ' ' || c == '-')
            {
                // Replace spaces and dashes with underscores
                if (!firstChar && sb.Length > 0 && sb[sb.Length - 1] != '_')
                {
                    sb.Append('_');
                }
                firstChar = false;
            }
        }

        // Ensure we have a valid name
        string result = sb.ToString();
        if (string.IsNullOrEmpty(result))
        {
            return "Entity";
        }

        // Remove trailing underscores
        return result.TrimEnd('_');
    }
}
