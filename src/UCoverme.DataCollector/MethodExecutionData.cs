using System.Collections.Concurrent;
using UCoverme.DataCollector.Events;

namespace UCoverme.DataCollector
{
    public class MethodExecutionData
    {
        public int AssemblyId { get; }
        public int MethodId { get; }
        public ConcurrentQueue<ExecutionEvent> ExecutionEvents { get; }

        public MethodExecutionData(int assemblyId, int methodId)
        {
            AssemblyId = assemblyId;
            MethodId = methodId;
            ExecutionEvents = new ConcurrentQueue<ExecutionEvent>();
        }

        public void BranchEntered(int branchId)
        {
            ExecutionEvents.Enqueue(ExecutionEvent.BranchEntered(AssemblyId, MethodId, branchId));
        }

        public void BranchExited(int branchId)
        {
            ExecutionEvents.Enqueue(ExecutionEvent.BranchExited(AssemblyId, MethodId, branchId));
        }

        public void SequencePointHit(int branchId)
        {
            ExecutionEvents.Enqueue(ExecutionEvent.SequencePointHit(AssemblyId, MethodId, branchId));
        }
    }
}