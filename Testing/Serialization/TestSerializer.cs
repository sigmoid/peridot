using System.IO;
using System.Text.Json;

namespace Peridot.Testing;

public class TestSerializer
{
    public static TestScenario DeserializeScenario(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Test scenario file not found: {filePath}");
        }
        string json = File.ReadAllText(filePath);
        var scenario = JsonSerializer.Deserialize<TestScenario>(json);

        return scenario ?? throw new InvalidDataException("Failed to deserialize TestScenario from JSON.");
    }

    public static void SerializeScenario(TestScenario scenario, string filepath)
    {
        string json = JsonSerializer.Serialize(scenario, new JsonSerializerOptions { WriteIndented = true });

        if (!Directory.Exists(Path.GetDirectoryName(filepath)))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filepath));
        }

        File.WriteAllText(filepath, json);
    }
}