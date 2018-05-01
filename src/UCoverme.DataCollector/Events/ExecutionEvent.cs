using System;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using ProtoBuf;

namespace UCoverme.DataCollector.Events
{
    [ProtoContract(SkipConstructor = true)]
    [ProtoInclude(2, typeof(TestCaseStartedEvent))]
    [ProtoInclude(3, typeof(TestCaseEndedEvent))]
    [ProtoInclude(4, typeof(BranchEnteredEvent))]
    [ProtoInclude(5, typeof(SequencePointHitEvent))]
    [ProtoInclude(6, typeof(BranchExitedEvent))]
    [ProtoInclude(7, typeof(MethodEnteredEvent))]
    public class ExecutionEvent
    {
        [ProtoMember(1, IsRequired = true)]
        public ExecutionEventType ExecutionEventType { get; }

        protected ExecutionEvent(ExecutionEventType executionEventType)
        {
            ExecutionEventType = executionEventType;
        }

        public static ExecutionEvent TestCaseStarted(Guid testCaseId, string testCaseName)
        {
            return new TestCaseStartedEvent(testCaseId, testCaseName);
        }

        public static ExecutionEvent TestCaseEnded(TestOutcome result)
        {
            return new TestCaseEndedEvent(result);
        }

        public static ExecutionEvent BranchEntered(int assemblyId, int methodId, int branchId)
        {
            return new BranchEnteredEvent(assemblyId, methodId, branchId);
        }

        public static ExecutionEvent SequencePointHit(int assemblyId, int methodId, int sequencePointId)
        {
            return new SequencePointHitEvent(assemblyId, methodId, sequencePointId);
        }

        public static ExecutionEvent BranchExited(int assemblyId, int methodId, int branchId)
        {
            return new BranchExitedEvent(assemblyId, methodId, branchId);
        }

        public static ExecutionEvent MethodEntered(int assemblyId, int methodId)
        {
            return new MethodEnteredEvent(assemblyId, methodId);
        }
    }
}