using System.Threading;
using Newtonsoft.Json;

namespace UCoverme.Model
{
    public class Condition : CodeSection
    {
        public int StartBranch { get; private set; }
        public int TargetBranch { get; private set; }

        [JsonIgnore]
        public int VisitCount => _visitCount;
        [JsonIgnore] 
        private int _visitCount;

        [JsonConstructor]
        public Condition(int startOffset, int endOffset, int startBranch, int targetBranch) : base(startOffset, endOffset)
        {
            StartBranch = startBranch;
            TargetBranch = targetBranch;
            _visitCount = 0;
        }

        public Condition(int startOffset, int endOffset) : base(startOffset, endOffset)
        {
            _visitCount = 0;
        }

        public void Visit()
        {
            Interlocked.Increment(ref _visitCount);
        }

        public override string ToString()
        {
            return $"{StartOffset} --> {EndOffset}";
        }
        
        public void SetStartBranchId(int id)
        {
            StartBranch = id;
        }

        public void SetTargetBranchId(int id)
        {
            TargetBranch = id;
        }
    }
}