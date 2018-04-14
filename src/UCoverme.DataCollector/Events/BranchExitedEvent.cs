namespace UCoverme.DataCollector.Events
{
    public class BranchExitedEvent : ExecutionEvent
    {
        public int MethodId { get; }
        public int BranchId { get; }

        public BranchExitedEvent(int methodId, int branchId) : base(ExecutionEventType.BranchExited)
        {
            MethodId = methodId;
            BranchId = branchId;
        }

        public override string ToString()
        {
            return $"Branch exited - #method: {MethodId} - #branch: {BranchId}";
        }
    }
}