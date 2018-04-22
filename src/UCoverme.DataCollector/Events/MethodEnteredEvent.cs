namespace UCoverme.DataCollector.Events
{
    public class MethodEnteredEvent : ExecutionEvent
    {
        public int MethodId { get; }

        public MethodEnteredEvent(int methodId) : base(ExecutionEventType.MethodEntered)
        {
            MethodId = methodId;
        }

        public override string ToString()
        {
            return $"Method entered - #method: {MethodId}";
        }
    }
}