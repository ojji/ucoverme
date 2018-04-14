namespace UCoverme.DataCollector.Events
{
    public class SequencePointHitEvent : ExecutionEvent
    {
        public int MethodId { get; }
        public int SequencePointId { get; }

        public SequencePointHitEvent(int methodId, int sequencePointId) : base(ExecutionEventType.SequencePointEntered)
        {
            MethodId = methodId;
            SequencePointId = sequencePointId;
        }

        public override string ToString()
        {
            return $"Sequence point hit - #method: {MethodId} - #seq. point: {SequencePointId}";
        }
    }
}