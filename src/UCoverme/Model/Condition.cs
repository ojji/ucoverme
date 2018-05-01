using System;
using System.Threading;
using Newtonsoft.Json;

namespace UCoverme.Model
{
    public class Condition : ICodeSection, IEquatable<Condition>
    {
        public int StartOffset { get; }
        public int EndOffset { get; }

        public int StartBranch { get; private set; }
        public int TargetBranch { get; private set; }

        [JsonIgnore]
        public int VisitCount => _visitCount;
        [JsonIgnore] 
        private int _visitCount;

        [JsonConstructor]
        public Condition(int startOffset, int endOffset, int startBranch, int targetBranch)
        {
            StartOffset = startOffset;
            EndOffset = endOffset;
            StartBranch = startBranch;
            TargetBranch = targetBranch;
            _visitCount = 0;
        }

        public Condition(int startOffset, int endOffset)
        {
            StartOffset = startOffset;
            EndOffset = endOffset;
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

        public bool Equals(Condition other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(StartOffset, other.StartOffset) && Equals(EndOffset, other.EndOffset);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Condition) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (StartOffset.GetHashCode() * 397) ^ EndOffset.GetHashCode();
            }
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