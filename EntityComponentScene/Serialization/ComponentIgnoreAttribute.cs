using System;

namespace Peridot.EntityComponentScene.Serialization;

/// <summary>
/// Attribute to mark properties that should NOT be serialized
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class ComponentIgnoreAttribute : Attribute
{
}
