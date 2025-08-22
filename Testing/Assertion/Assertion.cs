using System.Collections.Generic;

namespace Peridot.Testing.Assertion;

[System.Serializable]
public class SceneAssertion
{
    public double Timestamp { get; set; }
    public List<PropertyAssertion> PropertyAssertions { get; set; }
    public string Description { get; set; }

    public SceneAssertion()
    {
        PropertyAssertions = new List<PropertyAssertion>();
    }

    public SceneAssertion(double timestamp, string description = null)
    {
        Timestamp = timestamp;
        Description = description;
        PropertyAssertions = new List<PropertyAssertion>();
    }

    public void AddAssertion(string propertyPath, object expectedValue, string propertyType, float tolerance = 0.001f)
    {
        PropertyAssertions.Add(new PropertyAssertion(propertyPath, expectedValue, propertyType, tolerance));
    }
}

public enum AssertionResult
{
    Pass,
    Fail,
    Error
}

[System.Serializable]
public class AssertionExecutionResult
{
    public string PropertyPath { get; set; }
    public object ExpectedValue { get; set; }
    public object ActualValue { get; set; }
    public AssertionResult Result { get; set; }
    public string ErrorMessage { get; set; }
    public double Timestamp { get; set; }

    public AssertionExecutionResult()
    {
    }

    public AssertionExecutionResult(string propertyPath, object expectedValue, object actualValue,
        AssertionResult result, double timestamp, string errorMessage = null)
    {
        PropertyPath = propertyPath;
        ExpectedValue = expectedValue;
        ActualValue = actualValue;
        Result = result;
        Timestamp = timestamp;
        ErrorMessage = errorMessage;
    }
}

    [System.Serializable]
    public class PropertyAssertion
    {
        public string PropertyPath { get; set; }
        public object ExpectedValue { get; set; }
        public string PropertyType { get; set; }
        public float Tolerance { get; set; } = 0.001f; // For floating point comparisons

        public PropertyAssertion()
        {
            // Parameterless constructor for JSON deserialization
        }

        public PropertyAssertion(string propertyPath, object expectedValue, string propertyType, float tolerance = 0.001f)
        {
            PropertyPath = propertyPath;
            ExpectedValue = expectedValue;
            PropertyType = propertyType;
            Tolerance = tolerance;
        }
    }