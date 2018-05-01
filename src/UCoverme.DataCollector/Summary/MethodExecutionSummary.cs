using ProtoBuf;
using UCoverme.DataCollector.Events;

namespace UCoverme.DataCollector.Summary
{
    [ProtoContract(SkipConstructor = true)]
    public class MethodExecutionSummary
    {
        [ProtoMember(1, IsRequired = true)]
        public int AssemblyId { get; set; }
        [ProtoMember(2, IsRequired = true)]
        public int MethodId { get; set; }
        [ProtoMember(3, IsRequired = true)]
        public ExecutionEvent[] MethodEvents { get; set; }
    }
}