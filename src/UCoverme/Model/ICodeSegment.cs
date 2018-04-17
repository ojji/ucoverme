namespace UCoverme.Model
{
    public interface ICodeSegment
    {
        int StartOffset { get; }
        int EndOffset { get; }
    }
}