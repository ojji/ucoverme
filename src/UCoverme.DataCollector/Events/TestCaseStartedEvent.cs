using System;
using ProtoBuf;

namespace UCoverme.DataCollector.Events
{
    [ProtoContract(SkipConstructor = true)]
    public class TestCaseStartedEvent : ExecutionEvent
    {
        [ProtoMember(1, IsRequired = true)]
        public Guid TestCaseId { get; }
        [ProtoMember(2, IsRequired = true)]
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