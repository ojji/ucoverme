using ProtoBuf;

namespace UCoverme.DataCollector.Events
{
    [ProtoContract(SkipConstructor = true)]
    public class MethodEnteredEvent : ExecutionEvent
    {
        [ProtoMember(1, IsRequired = true)]
        public int AssemblyId { get; }
        [ProtoMember(2, IsRequired = true)]
        public int MethodId { get; }

        public MethodEnteredEvent(int assemblyId, int methodId) : base(ExecutionEventType.MethodEntered)
        {
            AssemblyId = assemblyId;
            MethodId = methodId;
        }

        public override string ToString()
        {
            return $"Method entered - #assembly: {AssemblyId} - #method: {MethodId}";
        }
    }
}