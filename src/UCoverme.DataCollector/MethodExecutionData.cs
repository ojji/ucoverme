using System.Collections.Concurrent;
using UCoverme.DataCollector.Events;

namespace UCoverme.DataCollector
{
    public class MethodExecutionData
    {
        public int MethodId;
        public ConcurrentQueue<ExecutionEvent> ExecutionEvents { get; }

        public MethodExecutionData(int methodId)
        {
            MethodId = methodId;
            ExecutionEvents = new ConcurrentQueue<ExecutionEvent>();
        }

        public void BranchEntered(int branchId)
        {
            ExecutionEvents.Enqueue(ExecutionEvent.BranchEntered(MethodId, branchId));
        }

        public void BranchExited(int branchId)
        {
            ExecutionEvents.Enqueue(ExecutionEvent.BranchExited(MethodId, branchId));
        }

        public void SequencePointHit(int branchId)
        {
            ExecutionEvents.Enqueue(ExecutionEvent.SequencePointHit(MethodId, branchId));
        }
    }
}