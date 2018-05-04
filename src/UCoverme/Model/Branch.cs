using System.Threading;
using Newtonsoft.Json;

namespace UCoverme.Model
{
    public class Branch : CodeSection
    {
        public int Id { get; }
        
        [JsonIgnore] 
        public int VisitCount => _visitCount;
        [JsonIgnore]
        private int _visitCount;

        [JsonConstructor]
        public Branch(int id, int startOffset, int endOffset) : base(startOffset, endOffset)
        {
            Id = id;
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