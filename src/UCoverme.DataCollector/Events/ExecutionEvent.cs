using System;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace UCoverme.DataCollector.Events
{
    public class ExecutionEvent
    {
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

        public static ExecutionEvent BranchEntered(int methodId, int branchId)
        {
            return new BranchEnteredEvent(methodId, branchId);
        }

        public static ExecutionEvent SequencePointHit(int methodId, int sequencePointId)
        {
            return new SequencePointHitEvent(methodId, sequencePointId);
        }

        public static ExecutionEvent BranchExited(int methodId, int branchId)
        {
            return new BranchExitedEvent(methodId, branchId);
        }
    }

    public class BranchExitedEvent : ExecutionEvent
    {
        public int MethodId { get; }
        public int BranchId { get; }

        public BranchExitedEvent(int methodId, int branchId) : base(ExecutionEventType.BranchExited)
        {
            MethodId = methodId;
            BranchId = branchId;
        }
    }
}