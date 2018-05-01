using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using ProtoBuf;

namespace UCoverme.DataCollector.Events
{
    [ProtoContract(SkipConstructor = true)]
    public class TestCaseEndedEvent : ExecutionEvent
    {
        [ProtoMember(1, IsRequired = true)]
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