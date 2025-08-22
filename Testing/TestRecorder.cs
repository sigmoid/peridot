using Microsoft.Xna.Framework;
using Peridot.Testing.Assertion;
using Peridot.Testing.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Peridot.Testing;

public class TestRecorder
{
    private List<InputMoment> _inputMoments;
    private List<InputMoment> _currentTestMoments;
    private List<SceneAssertion> _capturedAssertions;
    private string _currentTestName;
    private string _currentSceneData;
    private float _startTime;

    public TestRecorder()
    {
    }

    public void BeginRecordingTest(string testName, GameTime gameTime)
    {
        _currentTestMoments = new List<InputMoment>();
        _capturedAssertions = new List<SceneAssertion>();
        _currentTestName = testName;

        _currentSceneData = SceneSerializer.SerializeScene(Core.CurrentScene);

        //get current time span
        _startTime = (float)gameTime.TotalGameTime.TotalMilliseconds;
    }

    public void EndRecordingTest(GameTime gameTime)
    {
        var buttons = Core.InputManager.GetAllButtons();


        TestScenario scenario = new TestScenario
        {
            InputMomentsData = _inputMoments,
            SceneData = _currentSceneData,
            Duration = (float)(gameTime.TotalGameTime.TotalMilliseconds - _startTime) * 0.001f,
            TestName = _currentTestName ?? "Unnamed Test",
            RecordedTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            ButtonNames = buttons.Select(x => x.Name).ToList(),
            AssertionsData = _capturedAssertions ?? new List<SceneAssertion>()
        };

        SaveScenarioToFile(scenario);
    }

    public void RecordMoment(InputMoment moment)
    {
        _inputMoments ??= new List<InputMoment>();
        moment.Timestamp = (moment.Timestamp - _startTime) * 0.001f; 
        _inputMoments.Add(moment);
    }

    public void RecordMousePosition(Vector2 position)
    {
        throw new NotImplementedException();
    }

    public void CaptureAssertion(GameTime gameTime)
    {
        if (string.IsNullOrEmpty(_currentTestName))
        {
            Logger.Warning("Cannot capture assertion - no test recording in progress");
            return;
        }

        // Calculate the timestamp relative to test start
        double timestamp = ((float)gameTime.TotalGameTime.TotalMilliseconds - _startTime) * 0.001f;
        
        // Find the player entity
        var playerEntity = Core.CurrentScene.FindEntityByName("Player");
        if (playerEntity == null)
        {
            Logger.Warning("Cannot capture player position assertion - Player entity not found in scene");
            return;
        }

        // Create assertion for player position
        var assertion = new SceneAssertion(timestamp, $"Player position at {timestamp:F2}s");
        
        // Capture player position with some tolerance for movement
        assertion.AddAssertion("Entity[Player].Position.X", playerEntity.Position.X, "Single", 5.0f);
        assertion.AddAssertion("Entity[Player].Position.Y", playerEntity.Position.Y, "Single", 5.0f);

        _capturedAssertions.Add(assertion);
        
        Logger.Info($"Captured player position assertion at {timestamp:F2}s: ({playerEntity.Position.X:F1}, {playerEntity.Position.Y:F1}) with tolerance 5.0");
        Logger.Info($"Position types - X: {playerEntity.Position.X.GetType().Name}, Y: {playerEntity.Position.Y.GetType().Name}");
    }

    private void SaveScenarioToFile(TestScenario scenario)
    {
        TestSerializer.SerializeScenario(scenario, $"Tests/{scenario.TestName}.json");
    }
}