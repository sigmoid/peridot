using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;
using System.Linq;
using System.Text.Json;

namespace Peridot.Testing.Assertion;

public class AssertionManager
{
    private const float DEFAULT_TOLERANCE = 0.001f;

    /// <summary>
    /// Creates a scene assertion by capturing the current state of specified properties
    /// </summary>
    /// <param name="timestamp">The timestamp when the assertion is taken</param>
    /// <param name="description">Optional description for the assertion</param>
    /// <param name="propertyPaths">Specific property paths to capture, if null captures common properties</param>
    /// <returns>A SceneAssertion containing the captured state</returns>
    public static SceneAssertion CaptureSceneAssertion(double timestamp, string description = null, params string[] propertyPaths)
    {
        var assertion = new SceneAssertion(timestamp, description);

        if (propertyPaths == null || propertyPaths.Length == 0)
        {
            // Capture default common properties
            CaptureDefaultProperties(assertion);
        }
        else
        {
            // Capture specific properties
            foreach (var propertyPath in propertyPaths)
            {
                CaptureProperty(assertion, propertyPath);
            }
        }

        return assertion;
    }

    /// <summary>
    /// Validates a scene assertion against the current scene state
    /// </summary>
    /// <param name="assertion">The assertion to validate</param>
    /// <returns>A list of assertion execution results</returns>
    public static List<AssertionExecutionResult> ValidateAssertion(SceneAssertion assertion)
    {
        var results = new List<AssertionExecutionResult>();

        foreach (var propertyAssertion in assertion.PropertyAssertions)
        {
            try
            {
                var actualValue = GetPropertyValue(propertyAssertion.PropertyPath);
                var result = CompareValues(propertyAssertion.ExpectedValue, actualValue, 
                    propertyAssertion.PropertyType, propertyAssertion.Tolerance);

                results.Add(new AssertionExecutionResult(
                    propertyAssertion.PropertyPath,
                    propertyAssertion.ExpectedValue,
                    actualValue,
                    result ? AssertionResult.Pass : AssertionResult.Fail,
                    assertion.Timestamp
                ));
            }
            catch (Exception ex)
            {
                results.Add(new AssertionExecutionResult(
                    propertyAssertion.PropertyPath,
                    propertyAssertion.ExpectedValue,
                    null,
                    AssertionResult.Error,
                    assertion.Timestamp,
                    ex.Message
                ));
            }
        }

        return results;
    }

    private static void CaptureDefaultProperties(SceneAssertion assertion)
    {
        // Capture entity count
        CaptureProperty(assertion, "Scene.EntityCount");

        // Capture positions of all named entities
        var entities = Core.CurrentScene.GetEntities();
        foreach (var entity in entities)
        {
            if (!string.IsNullOrEmpty(entity.Name))
            {
                CaptureProperty(assertion, $"Entity[{entity.Name}].Position.X");
                CaptureProperty(assertion, $"Entity[{entity.Name}].Position.Y");
                
                // Capture component states if they exist
                var boxCollider = entity.GetComponent<BoxColliderComponent>();
                if (boxCollider != null)
                {
                    var bounds = boxCollider.GetBoundingBox();
                    CaptureProperty(assertion, $"Entity[{entity.Name}].BoxCollider.Min.X", bounds.Min.X);
                    CaptureProperty(assertion, $"Entity[{entity.Name}].BoxCollider.Min.Y", bounds.Min.Y);
                    CaptureProperty(assertion, $"Entity[{entity.Name}].BoxCollider.Max.X", bounds.Max.X);
                    CaptureProperty(assertion, $"Entity[{entity.Name}].BoxCollider.Max.Y", bounds.Max.Y);
                }

                var rigidbody = entity.GetComponent<RigidbodyComponent>();
                if (rigidbody != null)
                {
                    CaptureProperty(assertion, $"Entity[{entity.Name}].Rigidbody.IsStatic", rigidbody.IsStatic);
                    CaptureProperty(assertion, $"Entity[{entity.Name}].Rigidbody.Position.X", rigidbody.Position.X);
                    CaptureProperty(assertion, $"Entity[{entity.Name}].Rigidbody.Position.Y", rigidbody.Position.Y);
                }
            }
        }
    }

    private static void CaptureProperty(SceneAssertion assertion, string propertyPath, object customValue = null)
    {
        try
        {
            var value = customValue ?? GetPropertyValue(propertyPath);
            var valueType = value?.GetType()?.Name ?? "null";
            
            assertion.AddAssertion(propertyPath, value, valueType, DEFAULT_TOLERANCE);
        }
        catch (Exception ex)
        {
            Logger.Warning($"Failed to capture property {propertyPath}: {ex.Message}");
        }
    }

    private static object GetPropertyValue(string propertyPath)
    {
        var parts = propertyPath.Split('.');
        object current = null;

        if (parts[0] == "Scene")
        {
            current = Core.CurrentScene;
            parts = parts.Skip(1).ToArray();
        }
        else if (parts[0].StartsWith("Entity[") && parts[0].EndsWith("]"))
        {
            var entityName = parts[0].Substring(7, parts[0].Length - 8); // Extract name from Entity[name]
            current = Core.CurrentScene.FindEntityByName(entityName);
            parts = parts.Skip(1).ToArray();
            
            if (current == null)
            {
                throw new ArgumentException($"Entity '{entityName}' not found in scene");
            }
        }
        else
        {
            throw new ArgumentException($"Invalid property path root: {parts[0]}");
        }

        foreach (var part in parts)
        {
            if (current == null)
                return null;

            current = GetPropertyValueFromObject(current, part);
        }

        return current;
    }

    private static object GetPropertyValueFromObject(object obj, string propertyName)
    {
        if (obj == null) return null;

        var type = obj.GetType();

        // Handle special cases
        switch (propertyName)
        {
            case "EntityCount" when obj is Scene scene:
                return scene.GetEntities().Count;
            
            case "BoxCollider" when obj is Entity entity:
                return entity.GetComponent<BoxColliderComponent>();
                
            case "Rigidbody" when obj is Entity entity:
                return entity.GetComponent<RigidbodyComponent>();
        }

        // Try to get property using reflection
        var property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        if (property != null)
        {
            return property.GetValue(obj);
        }

        // Try to get field using reflection
        var field = type.GetField(propertyName, BindingFlags.Public | BindingFlags.Instance);
        if (field != null)
        {
            return field.GetValue(obj);
        }

        throw new ArgumentException($"Property or field '{propertyName}' not found on type '{type.Name}'");
    }

    private static bool CompareValues(object expected, object actual, string propertyType, float tolerance)
    {
        if (expected == null && actual == null) return true;
        if (expected == null || actual == null) return false;

        // Debug logging to help diagnose comparison issues
        Logger.Info($"Comparing values - Expected: {expected} (Type: {expected.GetType().Name}), Actual: {actual} (Type: {actual.GetType().Name}), Tolerance: {tolerance}");

        // Handle JsonElement conversion for values that came from JSON deserialization
        var expectedValue = ExtractValueFromJsonElement(expected);
        var actualValue = ExtractValueFromJsonElement(actual);

        // Always try numeric comparison first for any numeric types
        if (IsNumericType(expectedValue) && IsNumericType(actualValue))
        {
            var expectedAsDouble = Convert.ToDouble(expectedValue);
            var actualAsDouble = Convert.ToDouble(actualValue);
            var diff = System.Math.Abs(expectedAsDouble - actualAsDouble);
            var result = diff <= tolerance;
            Logger.Info($"Numeric conversion comparison: {expectedAsDouble} vs {actualAsDouble}, diff: {diff}, tolerance: {tolerance}, result: {result}");
            return result;
        }

        // Handle Vector2 comparisons
        if (expectedValue is Vector2 expectedVector && actualValue is Vector2 actualVector)
        {
            var result = System.Math.Abs(expectedVector.X - actualVector.X) <= tolerance && 
                        System.Math.Abs(expectedVector.Y - actualVector.Y) <= tolerance;
            Logger.Info($"Vector2 comparison: {expectedVector} vs {actualVector}, tolerance: {tolerance}, result: {result}");
            return result;
        }

        // Handle other types with standard equality
        var equalityResult = expectedValue.Equals(actualValue);
        Logger.Info($"Equality comparison: {expectedValue} vs {actualValue}, result: {equalityResult}");
        return equalityResult;
    }

    private static object ExtractValueFromJsonElement(object value)
    {
        if (value is JsonElement jsonElement)
        {
            switch (jsonElement.ValueKind)
            {
                case JsonValueKind.Number:
                    // Try to get the most appropriate numeric type
                    if (jsonElement.TryGetDouble(out double doubleValue))
                    {
                        // If it's a whole number, return as double to match typical JSON behavior
                        return doubleValue;
                    }
                    break;
                case JsonValueKind.String:
                    return jsonElement.GetString();
                case JsonValueKind.True:
                    return true;
                case JsonValueKind.False:
                    return false;
                case JsonValueKind.Null:
                    return null;
            }
        }
        
        return value; // Return original value if not a JsonElement
    }

    private static bool IsNumericType(object value)
    {
        return value is sbyte || value is byte || value is short || value is ushort ||
               value is int || value is uint || value is long || value is ulong ||
               value is float || value is double || value is decimal;
    }
}
