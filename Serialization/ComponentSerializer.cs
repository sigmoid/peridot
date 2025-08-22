using System;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using System.Globalization;
using Microsoft.Xna.Framework;

namespace Peridot.Components
{
    /// <summary>
    /// Base class for automatic component serialization using reflection
    /// </summary>
    public static class ComponentSerializer
    {
        /// <summary>
        /// Automatically serialize a component using reflection to find properties
        /// </summary>
        /// <param name="component">The component to serialize</param>
        /// <returns>XElement with Component Type and Property elements</returns>
        public static XElement AutoSerialize(Component component)
        {
            var componentType = component.GetType();
            var element = new XElement("Component", 
                new XAttribute("Type", componentType.Name));

            // Get all public properties that can be read and are basic serializable types
            var properties = componentType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(prop => prop.CanRead && ShouldSerializeProperty(prop));

            foreach (var property in properties)
            {
                try
                {
                    var value = property.GetValue(component);
                    if (value != null)
                    {
                        var stringValue = ConvertValueToString(value);
                        var typeName = GetPropertyTypeName(property.PropertyType);
                        
                        element.Add(new XElement("Property",
                            new XAttribute("Name", property.Name),
                            new XAttribute("Value", stringValue),
                            new XAttribute("Type", typeName)));
                    }
                }
                catch (Exception ex)
                {
                    // Log warning but continue with other properties
                    Console.WriteLine($"Warning: Could not serialize property {property.Name} on {componentType.Name}: {ex.Message}");
                }
            }

            return element;
        }

        /// <summary>
        /// Determine if a property should be serialized
        /// </summary>
        private static bool ShouldSerializeProperty(PropertyInfo property)
        {
            // Skip Entity property (reference to parent entity)
            if (property.Name == "Entity")
                return false;

            // Include basic serializable types
            var type = property.PropertyType;
            return IsBasicSerializableType(type);
        }

        /// <summary>
        /// Check if a type is a basic serializable type
        /// </summary>
        private static bool IsBasicSerializableType(Type type)
        {
            return type == typeof(string) ||
                   type == typeof(int) ||
                   type == typeof(float) ||
                   type == typeof(double) ||
                   type == typeof(bool) ||
                   type == typeof(Vector2) ||
                   type.IsEnum;
        }

        /// <summary>
        /// Convert a value to its string representation for XML
        /// </summary>
        private static string ConvertValueToString(object value)
        {
            switch (value)
            {
                case string s:
                    return s;
                case int i:
                    return i.ToString(CultureInfo.InvariantCulture);
                case float f:
                    return f.ToString(CultureInfo.InvariantCulture);
                case double d:
                    return d.ToString(CultureInfo.InvariantCulture);
                case bool b:
                    return b.ToString().ToLowerInvariant();
                case Vector2 v2:
                    return $"{v2.X},{v2.Y}";
                case Enum e:
                    return e.ToString();
                default:
                    return value.ToString();
            }
        }

        /// <summary>
        /// Get the type name for the Property Type attribute
        /// </summary>
        private static string GetPropertyTypeName(Type type)
        {
            if (type == typeof(string)) return "string";
            if (type == typeof(int)) return "int";
            if (type == typeof(float)) return "float";
            if (type == typeof(double)) return "double";
            if (type == typeof(bool)) return "bool";
            if (type == typeof(Vector2)) return "Vector2";
            if (type.IsEnum) return "enum";
            
            return type.Name.ToLowerInvariant();
        }
    }
}
