using ProtoBuf;

namespace UCoverme.DataCollector.Events
{
    [ProtoContract(SkipConstructor = true)]
    public class SequencePointHitEvent : ExecutionEvent
    {
        [ProtoMember(1, IsRequired = true)]
        public int AssemblyId { get; }
        [ProtoMember(2, IsRequired = true)]
        public int MethodId { get; }
        [ProtoMember(3, IsRequired = true)]
        public int SequencePointId { get; }

        public SequencePointHitEvent(int assemblyId, int methodId, int sequencePointId) : base(ExecutionEventType.SequencePointHit)
        {
            AssemblyId = assemblyId;
            MethodId = methodId;
            SequencePointId = sequencePointId;
        }

        public override string ToString()
        {
            return $"Sequence point hit - #assemblyId: {AssemblyId} - #method: {MethodId} - #seq. point: {SequencePointId}";
        }
    }
}