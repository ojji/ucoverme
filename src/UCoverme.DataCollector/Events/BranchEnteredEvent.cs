namespace UCoverme.DataCollector.Events
{
    public class BranchEnteredEvent : ExecutionEvent
    {
        public int MethodId { get; }
        public int BranchId { get; }

        public BranchEnteredEvent(int methodId, int branchId) : base(ExecutionEventType.BranchEntered)
        {
            MethodId = methodId;
            BranchId = branchId;
        }
    }
}