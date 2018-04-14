namespace UCoverme.Model
{
    public interface ISkipable
    {
        bool IsSkipped { get; }
        SkipReason SkipReason { get; }
        void SkipFromInstrumentation(SkipReason reason);
    }
}