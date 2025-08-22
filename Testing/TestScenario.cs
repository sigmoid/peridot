using System.Collections.Generic;
using Peridot.Testing.Assertion;
using Peridot.Testing.Input;

namespace Peridot.Testing
{
    [System.Serializable]
    public class TestScenario
    {
        public double Duration { get; set; }
        public List<InputMoment> InputMomentsData { get; set; }
        public List<SceneAssertion> AssertionsData { get; set; }
        public List<string> ButtonNames { get; set; }
        public string SceneData { get; set; }
        public string TestName { get; set; }
        public long RecordedTimestamp { get; set; }

        public TestScenario()
        {
            RecordedTimestamp = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }
    }
}
