using System.Threading;
using Newtonsoft.Json;

namespace UCoverme.Model
{
    public class Branch : ICodeSection
    {
        public int Id { get; }
        public int StartOffset { get; }
        public int EndOffset { get; }

        [JsonIgnore] 
        public int VisitCount => _visitCount;
        [JsonIgnore]
        private int _visitCount;

        [JsonConstructor]
        public Branch(int id, int startOffset, int endOffset)
        {
            Id = id;
            StartOffset = startOffset;
            EndOffset = endOffset;
            _visitCount = 0;
        }

        public override string ToString()
        {
            return $"[{Id}] [{StartOffset} - {EndOffset}]";
        }

        public void Visit()
        {
            Interlocked.Increment(ref _visitCount);
        }
    }
}