using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;

namespace Peridot.EntityComponentScene.Serialization;

/// <summary>
/// Represents an entity definition that can be serialized to/from XML
/// </summary>
[XmlRoot("Entity")]
public class EntityDefinition
{
    [XmlAttribute("Name")]
    public string Name { get; set; } = string.Empty;

    [XmlElement("Position")]
    public Vector2Definition Position { get; set; } = new Vector2Definition();

    [XmlElement("Component")]
    public List<ComponentDefinition> Components { get; set; } = new List<ComponentDefinition>();

    [XmlArray("Children")]
    [XmlArrayItem("Entity", typeof(EntityDefinition))]
    [XmlArrayItem("EntityFromFile", typeof(EntityFromFileDefinition))]
    public List<object> Children { get; set; } = new List<object>();
}

/// <summary>
/// Represents a Vector2 that can be serialized to/from XML
/// </summary>
public class Vector2Definition
{
    [XmlElement("X")]
    public float X { get; set; } = 0f;

    [XmlElement("Y")]
    public float Y { get; set; } = 0f;

    public Vector2 ToVector2()
    {
        return new Vector2(X, Y);
    }

    public static Vector2Definition FromVector2(Vector2 vector)
    {
        return new Vector2Definition { X = vector.X, Y = vector.Y };
    }
}

/// <summary>
/// Represents a component definition that can be serialized to/from XML
/// </summary>
public class ComponentDefinition
{
    [XmlAttribute("Type")]
    public string Type { get; set; }

    [XmlElement("Property")]
    public List<PropertyDefinition> Properties { get; set; } = new List<PropertyDefinition>();
}

/// <summary>
/// Represents a property definition that can be serialized to/from XML
/// </summary>
public class PropertyDefinition
{
    [XmlAttribute("Name")]
    public string Name { get; set; }

    [XmlAttribute("Value")]
    public string Value { get; set; }

    [XmlAttribute("Type")]
    public string Type { get; set; } = "string";
}

/// <summary>
/// Represents a reference to an entity defined in another file
/// </summary>
public class EntityFromFileDefinition
{
    [XmlAttribute("Path")]
    public string Path { get; set; }

    [XmlAttribute("Name")]
    public string Name { get; set; } = string.Empty;

    [XmlElement("Position")]
    public Vector2Definition Position { get; set; } = new Vector2Definition();
}
