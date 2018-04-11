using System;

namespace UCoverme.DataCollector.Events
{
    public class TestCaseStartedEvent : ExecutionEvent
    {
        public Guid TestCaseId { get; }
        public string TestCaseName { get; }

        public TestCaseStartedEvent(Guid testCaseId, string testCaseName) : base(ExecutionEventType.TestCaseStarted)
        {
            TestCaseId = testCaseId;
            TestCaseName = testCaseName;
        }

        public override string ToString()
        {
            return $"Test case started. ({TestCaseId} - {TestCaseName})";
        }
    }
}