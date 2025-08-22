using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Peridot.Components;

namespace Peridot.EntityComponentScene.Serialization;

/// <summary>
/// Factory class for creating entities from XML definitions using reflection
/// </summary>
public static class EntityFactory
{
    private static readonly Dictionary<string, Type> _componentTypes = new Dictionary<string, Type>();
    private static bool _typesInitialized = false;
    private static readonly object _initializationLock = new object();

    /// <summary>
    /// Initialize the component type cache by scanning assemblies
    /// </summary>
    private static void InitializeComponentTypes()
    {
        if (_typesInitialized) return;

        lock (_initializationLock)
        {
            // Double-check pattern to avoid race conditions
            if (_typesInitialized) return;

        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        
        foreach (var assembly in assemblies)
        {
            try
            {
                var componentTypes = assembly.GetTypes()
                    .Where(t => typeof(Component).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

                foreach (var type in componentTypes)
                {
                    _componentTypes[type.Name] = type;
                    // Also register without "Component" suffix for convenience
                    if (type.Name.EndsWith("Component"))
                    {
                        var shortName = type.Name.Substring(0, type.Name.Length - "Component".Length);
                        _componentTypes[shortName] = type;
                    }
                }
            }
            catch (ReflectionTypeLoadException)
            {
                // Skip assemblies that can't be loaded
            }
        }

        _typesInitialized = true;
        }
    }

    /// <summary>
    /// Load an entity from an XML file through the content manager
    /// </summary>
    /// <param name="content">The content manager to use for loading</param>
    /// <param name="assetName">The name of the XML asset to load</param>
    /// <returns>A new entity instance</returns>
    public static Entity FromFile(ContentManager content, string assetName)
    {
        // For now, we'll load directly from the file system
        // In a full implementation, you'd want to use the content pipeline
        var contentPath = Path.Combine(content.RootDirectory, assetName + ".xml");
        return FromXmlFile(contentPath);
    }

    /// <summary>
    /// Load an entity from an XML file through the content manager using full path
    /// </summary>
    /// <param name="content">The content manager to use for loading</param>
    /// <param name="assetPath">The full path to the XML asset</param>
    /// <returns>A new entity instance</returns>
    public static Entity FromContentFile(ContentManager content, string assetPath)
    {
        var fullPath = Path.Combine(content.RootDirectory, assetPath);
        return FromXmlFile(fullPath);
    }

    /// <summary>
    /// Load an entity from an XML file path
    /// </summary>
    /// <param name="xmlFilePath">The path to the XML file</param>
    /// <returns>A new entity instance</returns>
    public static Entity FromXmlFile(string xmlFilePath)
    {
        using (var reader = new FileStream(xmlFilePath, FileMode.Open))
        {
            var doc = XDocument.Load(reader);
            return FromXElement(doc.Root, Path.GetDirectoryName(xmlFilePath));
        }
    }

    public static Entity FromString(string xmlContent)
    {
        var doc = XDocument.Parse(xmlContent);
        return FromXElement(doc.Root);
    }

    /// <summary>
    /// Create an entity from an XElement (supports both new and old XML formats)
    /// </summary>
    /// <param name="element">The XML element representing the entity</param>
    /// <param name="baseDirectory">The base directory for resolving relative file paths</param>
    /// <returns>A new entity instance</returns>
    public static Entity FromXElement(XElement element, string baseDirectory = null)
    {
        InitializeComponentTypes();

        var entity = new Entity();

        // Set name - use Name attribute if present
        string entityName = element.Attribute("Name")?.Value ?? element.Attribute("name")?.Value;
        if (!string.IsNullOrEmpty(entityName))
        {
            // Check if this is a file reference (entities/player_entity.xml format)
            if (entityName.Contains('/') || entityName.Contains('\\') || entityName.EndsWith(".xml"))
            {
                // This is a file reference, load from file
                var filePath = entityName;
                if (baseDirectory != null && !Path.IsPathRooted(filePath))
                {
                    filePath = Path.Combine(baseDirectory, filePath);
                }

                var loadedEntity = FromXmlFile(filePath);

                // Override position if specified in the scene
                var positionElement = element.Element("Position");
                if (positionElement != null)
                {
                    loadedEntity.LocalPosition = new Vector2(
                        float.Parse(positionElement.Element("X").Value),
                        float.Parse(positionElement.Element("Y").Value)
                    );
                }

                return loadedEntity;
            }
            else
            {
                // This is an inline entity definition
                entity.Name = entityName;
            }
        }
        else
        {
            entity.Name = EntityNameProvider.GetUniqueName("Entity");
        }

        // Set position
        var position = element.Element("Position");
        if (position != null)
        {
            var x = float.Parse(position.Element("X").Value);
            var y = float.Parse(position.Element("Y").Value);
            // Always set LocalPosition to ensure correct positioning relative to parent
            entity.LocalPosition = new Vector2(x, y);
        }

        // Load components - handle both new format (Component elements) and old format
        var componentElements = element.Elements("Component");
        foreach (var componentElement in componentElements)
        {
            var component = CreateComponentFromXElement(componentElement);
            if (component != null)
            {
                entity.AddComponent(component);
            }
        }

        // Load children
        var childrenElement = element.Element("Children");
        if (childrenElement != null)
        {
            foreach (var childElement in childrenElement.Elements("Entity"))
            {
                var child = FromXElement(childElement, baseDirectory);
                if (child != null)
                {
                    entity.AddChild(child);
                }
            }
        }

        return entity;
    }

    /// <summary>
    /// Create an entity from an EntityDefinition
    /// </summary>
    /// <param name="definition">The entity definition</param>
    /// <returns>A new entity instance</returns>
    public static Entity FromDefinition(EntityDefinition definition)
    {
        return FromDefinition(definition, null);
    }

    /// <summary>
    /// Create an entity from an EntityDefinition
    /// </summary>
    /// <param name="definition">The entity definition</param>
    /// <param name="baseDirectory">The base directory for resolving relative file paths</param>
    /// <returns>A new entity instance</returns>
    private static Entity FromDefinition(EntityDefinition definition, string baseDirectory)
    {
        InitializeComponentTypes();

        var entity = new Entity();
        
        // Set name using the EntityNameProvider to ensure uniqueness
        string desiredName = !string.IsNullOrEmpty(definition.Name) ? definition.Name : "Entity";
        entity.Name = EntityNameProvider.GetUniqueName(desiredName);
        
        // Set position (this will be local position for children)
        if (definition.Position != null)
        {
            entity.LocalPosition = definition.Position.ToVector2();
        }

        // Create and add components
        foreach (var componentDef in definition.Components)
        {
            var component = CreateComponent(componentDef);
            if (component != null)
            {
                entity.AddComponent(component);
            }
        }

        // Create and add children
        foreach (var childDef in definition.Children)
        {
            Entity child = null;
            
            if (childDef is EntityDefinition entityDef)
            {
                child = FromDefinition(entityDef, baseDirectory);
            }
            else if (childDef is EntityFromFileDefinition fileDef)
            {
                var childFilePath = fileDef.Path;
                
                // Resolve relative paths relative to the base directory
                if (baseDirectory != null && !Path.IsPathRooted(childFilePath))
                {
                    childFilePath = Path.Combine(baseDirectory, childFilePath);
                }
                
                child = FromXmlFile(childFilePath);
                // Override local position if specified in the file reference
                if (fileDef.Position != null)
                {
                    child.LocalPosition = fileDef.Position.ToVector2();
                }
                // Override name if specified in the file reference
                if (!string.IsNullOrEmpty(fileDef.Name))
                {
                    child.Name = EntityNameProvider.GetUniqueName(fileDef.Name);
                }
            }
            
            if (child != null)
            {
                entity.AddChild(child);
            }
        }

        return entity;
    }

    /// <summary>
    /// Create a component from an XElement using the new Component XML format
    /// </summary>
    /// <param name="element">The XML element representing the component</param>
    /// <returns>A new component instance</returns>
    private static Component CreateComponentFromXElement(XElement element)
    {
        var componentType = element.Attribute("Type")?.Value;
        if (string.IsNullOrEmpty(componentType))
        {
            return null;
        }

        if (!_componentTypes.TryGetValue(componentType, out var type))
        {
            throw new ArgumentException($"Unknown component type: {componentType}");
        }

        // Extract properties from Property elements
        var properties = new Dictionary<string, string>();
        foreach (var propertyElement in element.Elements("Property"))
        {
            var name = propertyElement.Attribute("Name")?.Value;
            var value = propertyElement.Attribute("Value")?.Value;
            if (!string.IsNullOrEmpty(name) && value != null)
            {
                properties[name] = value;
            }
        }

        // Find the best constructor
        var constructors = type.GetConstructors()
            .OrderByDescending(c => c.GetParameters().Length);

        foreach (var constructor in constructors)
        {
            var parameters = constructor.GetParameters();
            var args = new object[parameters.Length];
            bool canUseConstructor = true;

            for (int i = 0; i < parameters.Length; i++)
            {
                var param = parameters[i];
                var paramName = param.Name;
                
                // Try to find a property with the same name (case-insensitive)
                var propKey = properties.Keys.FirstOrDefault(k => 
                    string.Equals(k, paramName, StringComparison.OrdinalIgnoreCase));

                if (propKey != null)
                {
                    try
                    {
                        args[i] = ConvertValue(properties[propKey], param.ParameterType);
                    }
                    catch
                    {
                        canUseConstructor = false;
                        break;
                    }
                }
                else if (param.HasDefaultValue)
                {
                    args[i] = param.DefaultValue;
                }
                else
                {
                    canUseConstructor = false;
                    break;
                }
            }

            if (canUseConstructor)
            {
                var component = (Component)Activator.CreateInstance(type, args);
                
                // Set any remaining properties that weren't used in the constructor
                SetRemainingPropertiesFromDictionary(component, properties, parameters);
                
                return component;
            }
        }

        // If no constructor matched, try default constructor and set properties
        try
        {
            var component = (Component)Activator.CreateInstance(type);
            SetRemainingPropertiesFromDictionary(component, properties, Array.Empty<ParameterInfo>());
            return component;
        }
        catch
        {
            throw new ArgumentException($"Could not create component of type {componentType}");
        }
    }

    /// <summary>
    /// Set properties on a component from a dictionary
    /// </summary>
    private static void SetRemainingPropertiesFromDictionary(Component component, Dictionary<string, string> properties, ParameterInfo[] constructorParams)
    {
        var componentType = component.GetType();
        var usedNames = new HashSet<string>(constructorParams.Select(p => p.Name), StringComparer.OrdinalIgnoreCase);

        foreach (var prop in properties)
        {
            if (usedNames.Contains(prop.Key)) continue;

            var propertyInfo = componentType.GetProperty(prop.Key, BindingFlags.Public | BindingFlags.Instance);
            if (propertyInfo != null && propertyInfo.CanWrite)
            {
                try
                {
                    var value = ConvertValue(prop.Value, propertyInfo.PropertyType);
                    propertyInfo.SetValue(component, value);
                }
                catch
                {
                    // Ignore properties that can't be set
                }
            }
        }
    }

    /// <summary>
    /// Create a component from a ComponentDefinition using reflection
    /// </summary>
    /// <param name="definition">The component definition</param>
    /// <returns>A new component instance</returns>
    private static Component CreateComponent(ComponentDefinition definition)
    {
        if (!_componentTypes.TryGetValue(definition.Type, out var componentType))
        {
            throw new ArgumentException($"Unknown component type: {definition.Type}");
        }

        // Convert properties to a dictionary for easier lookup
        var properties = definition.Properties.ToDictionary(p => p.Name, p => p);

        // Find the best constructor
        var constructors = componentType.GetConstructors()
            .OrderByDescending(c => c.GetParameters().Length);

        foreach (var constructor in constructors)
        {
            var parameters = constructor.GetParameters();
            var args = new object[parameters.Length];
            bool canUseConstructor = true;

            for (int i = 0; i < parameters.Length; i++)
            {
                var param = parameters[i];
                var paramName = param.Name;
                
                // Try to find a property with the same name (case-insensitive)
                var propKey = properties.Keys.FirstOrDefault(k => 
                    string.Equals(k, paramName, StringComparison.OrdinalIgnoreCase));

                if (propKey != null)
                {
                    try
                    {
                        args[i] = ConvertValue(properties[propKey].Value, param.ParameterType);
                    }
                    catch
                    {
                        canUseConstructor = false;
                        break;
                    }
                }
                else if (param.HasDefaultValue)
                {
                    args[i] = param.DefaultValue;
                }
                else
                {
                    canUseConstructor = false;
                    break;
                }
            }

            if (canUseConstructor)
            {
                var component = (Component)Activator.CreateInstance(componentType, args);
                
                // Set any remaining properties that weren't used in the constructor
                SetRemainingProperties(component, properties, parameters);
                
                return component;
            }
        }

        // If no constructor matched, try default constructor and set properties
        try
        {
            var component = (Component)Activator.CreateInstance(componentType);
            SetRemainingProperties(component, properties, Array.Empty<ParameterInfo>());
            return component;
        }
        catch
        {
            throw new ArgumentException($"Could not create component of type {definition.Type}");
        }
    }

    /// <summary>
    /// Set properties on a component that weren't used in the constructor
    /// </summary>
    private static void SetRemainingProperties(Component component, Dictionary<string, PropertyDefinition> properties, ParameterInfo[] constructorParams)
    {
        var componentType = component.GetType();
        var usedNames = new HashSet<string>(constructorParams.Select(p => p.Name), StringComparer.OrdinalIgnoreCase);

        foreach (var prop in properties)
        {
            if (usedNames.Contains(prop.Key)) continue;

            var propertyInfo = componentType.GetProperty(prop.Key, BindingFlags.Public | BindingFlags.Instance);
            if (propertyInfo != null && propertyInfo.CanWrite)
            {
                try
                {
                    var value = ConvertValue(prop.Value.Value, propertyInfo.PropertyType);
                    propertyInfo.SetValue(component, value);
                }
                catch
                {
                    // Ignore properties that can't be set
                }
            }
        }
    }

    /// <summary>
    /// Convert a string value to the specified type
    /// </summary>
    private static object ConvertValue(string value, Type targetType)
    {
        if (targetType == typeof(string))
                return value;

        if (targetType == typeof(int))
            return int.Parse(value);

        if (targetType == typeof(float))
            return float.Parse(value);

        if (targetType == typeof(bool))
            return bool.Parse(value);

        if (targetType == typeof(Vector2))
        {
            // format: {X:0.0, Y:0.0} or "X, Y"
            if (value.StartsWith("{") && value.EndsWith("}"))
            {
                value = value[1..^1]; // remove { and }
                // remove X: and Y: prefixes
                value = value.Replace("X:", "").Replace("Y:", "").Trim();

                var parts = value.Split(' ');
                if (parts.Length == 2)
                {
                    return new Vector2(float.Parse(parts[0]), float.Parse(parts[1]));
                }
            }
            else
            {
                var parts = value.Split(',');
                if (parts.Length == 2)
                {
                    return new Vector2(float.Parse(parts[0].Trim()), float.Parse(parts[1].Trim()));
                }
            }
        }

        if (targetType.IsEnum)
        {
            return Enum.Parse(targetType, value);
        }

        // Try using the type's converter
        var converter = System.ComponentModel.TypeDescriptor.GetConverter(targetType);
        if (converter.CanConvertFrom(typeof(string)))
        {
            return converter.ConvertFromString(value);
        }

        throw new ArgumentException($"Cannot convert '{value}' to type {targetType.Name}");
    }
}
