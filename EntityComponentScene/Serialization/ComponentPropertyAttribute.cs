using System;

namespace Peridot.EntityComponentScene.Serialization;
/// <summary>
/// Attribute to mark properties that should be serialized/deserialized from XML
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class ComponentPropertyAttribute : Attribute
{
    public string Name { get; }
    public object DefaultValue { get; }

    public ComponentPropertyAttribute(string name = null, object defaultValue = null)
    {
        Name = name;
        DefaultValue = defaultValue;
    }
}
