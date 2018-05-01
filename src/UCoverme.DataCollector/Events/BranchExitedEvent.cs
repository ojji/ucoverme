using ProtoBuf;

namespace UCoverme.DataCollector.Events
{
    [ProtoContract(SkipConstructor = true)]
    public class BranchExitedEvent : ExecutionEvent
    {
        [ProtoMember(1, IsRequired = true)]
        public int AssemblyId { get; }
        [ProtoMember(2, IsRequired = true)]
        public int MethodId { get; }
        [ProtoMember(3, IsRequired = true)]
        public int BranchId { get; }

        public BranchExitedEvent(int assemblyId, int methodId, int branchId) : base(ExecutionEventType.BranchExited)
        {
            AssemblyId = assemblyId;
            MethodId = methodId;
            BranchId = branchId;
        }

        public override string ToString()
        {
            return $"Branch exited - #assembly: {AssemblyId} - #method: {MethodId} - #branch: {BranchId}";
        }
    }
}