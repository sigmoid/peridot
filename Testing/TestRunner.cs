using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Peridot.Testing.Input;
using Peridot.Testing.Assertion;

namespace Peridot.Testing;

/// <summary>
/// Manages execution of multiple tests and generates comprehensive reports
/// </summary>
public class TestRunner
{
    private bool _isRunning = false;
    private float _scenarioTimer = 0;
    private Scene _storedScene;
    private IInputManager _storedInputManager;
    
    // Assertion tracking fields
    private List<AssertionExecutionResult> _currentTestResults;
    private int _nextAssertionIndex = 0;
    private List<AssertionExecutionResult> _allTestResults;

    public TestRunner()
    {
        _currentTestResults = new List<AssertionExecutionResult>();
        _allTestResults = new List<AssertionExecutionResult>();
    }

    private List<TestScenario> _scenarios = new List<TestScenario>();

    private TestScenario _currentScenario
    {
        get { return _scenarios.Count > 0 ? _scenarios[0] : null; }
    }

    /// <summary>
    /// Gets whether tests are currently running
    /// </summary>
    public bool IsRunning => _isRunning;

    /// <summary>
    /// Gets the name of the currently running test, or null if no test is running
    /// </summary>
    public string CurrentTestName => _currentScenario?.TestName;

    public void Update(GameTime gameTime)
    {
        _scenarioTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (_currentScenario == null)
            return;

        // Process pending assertions
        ProcessPendingAssertions();

        if (_currentScenario.Duration <= _scenarioTimer)
        {
            IncrementCurrentTest();
        }
    }

    public void EnqueueAllTests()
    {
        _scenarios.Clear();
        
        // Clear previous test results when starting a new test run
        _allTestResults.Clear();
        _currentTestResults.Clear();
        _nextAssertionIndex = 0;

        _storedScene = Core.CurrentScene;
        _storedInputManager = Core.InputManager;


        // load all test scenarios from the "Content/tests" directory
        if (!System.IO.Directory.Exists("Content/tests"))
        {
            Logger.Error("Content/tests directory does not exist");
            return;
        }

        var testFiles = System.IO.Directory.GetFiles("Content/tests", "*.json");
        if (testFiles.Length == 0)
        {
            Logger.Error("No test scenarios found in 'Content/tests' directory");
            return;
        }

        Logger.Info($"Found {testFiles.Length} test files");

        foreach (var file in testFiles)
        {
            try
            {
                Logger.Info($"Loading test file: {file}");
                var scenario = TestSerializer.DeserializeScenario(file);
                if (scenario != null)
                {
                    _scenarios.Add(scenario);
                    Logger.Info($"Successfully loaded test: {System.IO.Path.GetFileName(file)}");
                }
                else
                {
                    Logger.Warning($"Failed to deserialize test file: {file}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error loading test file {file}: {ex.Message}");
            }
        }

        if (_scenarios.Count == 0)
        {
            Logger.Error("No valid test scenarios were loaded");
            return;
        }

        Logger.Info($"Successfully loaded {_scenarios.Count} test scenarios");
        StartCurrentTest();
    }

    public void EnqueueTest(string testName, float speedMultiplier = 1.0f)
    {
        if (_currentScenario == null)
        {
            _storedScene = Core.CurrentScene;
            _storedInputManager = Core.InputManager;
        }

        var scenario = TestSerializer.DeserializeScenario($"Content/tests/{testName}.json");

        _scenarios.Add(scenario);
    }

    private void IncrementCurrentTest()
    {
        if (_scenarios.Count == 0)
            return;

        // Store results for the completed test
        if (_currentScenario != null)
        {
            var testResult = new TestRunResult
            {
                TestName = _currentScenario.TestName,
                AssertionResults = new List<AssertionExecutionResult>(_currentTestResults),
                Status = DetermineTestStatus(_currentTestResults)
            };
            
            Logger.Info($"Test '{_currentScenario.TestName}' completed: {testResult.PassedAssertions} passed, {testResult.FailedAssertions} failed, {testResult.ErrorAssertions} errors");
        }

        _scenarios.RemoveAt(0);

        _isRunning = false;

        if (_currentScenario == null)
        {
            Core.CurrentScene = _storedScene;
            Core.InputManager = _storedInputManager;
            return;
        }

        StartCurrentTest();
    }

    private TestStatus DetermineTestStatus(List<AssertionExecutionResult> results)
    {
        if (results.Any(r => r.Result == AssertionResult.Error))
            return TestStatus.Error;
        
        if (results.Any(r => r.Result == AssertionResult.Fail))
            return TestStatus.Failed;
        
        return TestStatus.Completed;
    }

    /// <summary>
    /// Gets the results for the currently running test
    /// </summary>
    public List<AssertionExecutionResult> GetCurrentTestResults()
    {
        return new List<AssertionExecutionResult>(_currentTestResults);
    }

    /// <summary>
    /// Gets all assertion results from all tests that have been run
    /// </summary>
    public List<AssertionExecutionResult> GetAllTestResults()
    {
        return new List<AssertionExecutionResult>(_allTestResults);
    }

    /// <summary>
    /// Gets summary statistics for all completed tests
    /// </summary>
    public (int totalTests, int passedAssertions, int failedAssertions, int errorAssertions) GetTestSummary()
    {
        var totalAssertions = _allTestResults.Count;
        var passed = _allTestResults.Count(r => r.Result == AssertionResult.Pass);
        var failed = _allTestResults.Count(r => r.Result == AssertionResult.Fail);
        var errors = _allTestResults.Count(r => r.Result == AssertionResult.Error);
        
        return (totalAssertions, passed, failed, errors);
    }

    private void StartCurrentTest()
    {
        if (_isRunning)
            return;

        if (_currentScenario == null)
        {
            return;
        }

        _scenarioTimer = 0; // Reset timer for new test
        _isRunning = true;
        
        // Reset assertion tracking for new test
        _currentTestResults.Clear();
        _nextAssertionIndex = 0;

        var newScene = SceneSerializer.DeserializeScene(_currentScenario.SceneData);
        Core.CurrentScene = newScene;

        var mockInputManager = new MockInputManager(_currentScenario.InputMomentsData, _currentScenario.ButtonNames);
        Core.InputManager = mockInputManager;
    }

    private void ProcessPendingAssertions()
    {
        if (_currentScenario?.AssertionsData == null || !_isRunning)
            return;

        // Process all assertions that should have triggered by now
        while (_nextAssertionIndex < _currentScenario.AssertionsData.Count)
        {
            var assertion = _currentScenario.AssertionsData[_nextAssertionIndex];
            
            // Check if this assertion's timestamp has been reached
            if (assertion.Timestamp <= _scenarioTimer)
            {
                try
                {
                    Logger.Info($"Executing assertion at timestamp {assertion.Timestamp}: {assertion.Description ?? "No description"}");
                    var results = AssertionManager.ValidateAssertion(assertion);
                    
                    _currentTestResults.AddRange(results);
                    _allTestResults.AddRange(results);
                    
                    // Log assertion results
                    foreach (var result in results)
                    {
                        if (result.Result == AssertionResult.Pass)
                        {
                            Logger.Info($"  ✓ PASS: {result.PropertyPath}");
                        }
                        else if (result.Result == AssertionResult.Fail)
                        {
                            Logger.Warning($"  ✗ FAIL: {result.PropertyPath} - Expected: {result.ExpectedValue}, Actual: {result.ActualValue}");
                        }
                        else
                        {
                            Logger.Error($"  ⚠ ERROR: {result.PropertyPath} - {result.ErrorMessage}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"Error executing assertion at timestamp {assertion.Timestamp}: {ex.Message}");
                }
                
                _nextAssertionIndex++;
            }
            else
            {
                // No more assertions ready to execute
                break;
            }
        }
    }
}

public enum TestStatus
{
    Unknown,
    Completed,
    Failed,
    Error,
    LoadFailed,
    Timeout
}

public class TestRunResult
{
    public TestStatus Status { get; set; }
    public List<AssertionExecutionResult> AssertionResults { get; set; }
    public string TestName { get; set; }
    public int PassedAssertions => AssertionResults?.Count(r => r.Result == AssertionResult.Pass) ?? 0;
    public int FailedAssertions => AssertionResults?.Count(r => r.Result == AssertionResult.Fail) ?? 0;
    public int ErrorAssertions => AssertionResults?.Count(r => r.Result == AssertionResult.Error) ?? 0;
    public int TotalAssertions => AssertionResults?.Count ?? 0;

    public TestRunResult()
    {
        AssertionResults = new List<AssertionExecutionResult>();
    }
}
