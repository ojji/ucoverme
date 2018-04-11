using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace UCoverme.DataCollector.Events
{
    public class TestCaseEndedEvent : ExecutionEvent
    {
        public TestOutcome Result { get; }

        public TestCaseEndedEvent(TestOutcome outcome) : base(ExecutionEventType.TestCaseEnded)
        {
            Result = outcome;
        }

        public override string ToString()
        {
            return $"Test case ended. TestResult: {Result.ToString()}";
        }
    }
}