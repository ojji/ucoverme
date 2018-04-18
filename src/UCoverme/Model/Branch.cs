using Newtonsoft.Json;

namespace UCoverme.Model
{
    public class Branch : ICodeSegment
    {
        public int Id { get; }
        public int StartOffset { get; }
        public int EndOffset { get; }

        [JsonConstructor]
        public Branch(int id, int startOffset, int endOffset)
        {
            Id = id;
            StartOffset = startOffset;
            EndOffset = endOffset;
        }
    }
}