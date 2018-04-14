namespace UCoverme.Model
{
    public class Branch
    {
        public int Id { get; }
        public int StartOffset { get; }
        public int EndOffset { get; }

        public Branch(int id, int startOffset, int endOffset)
        {
            Id = id;
            StartOffset = startOffset;
            EndOffset = endOffset;
        }
    }
}