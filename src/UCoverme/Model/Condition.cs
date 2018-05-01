using System;
using Newtonsoft.Json;

namespace UCoverme.Model
{
    public class Condition : ICodeSection, IEquatable<Condition>
    {
        public int StartOffset { get; }
        public int EndOffset { get; }

        [JsonConstructor]
        public Condition(int startOffset, int endOffset)
        {
            StartOffset = startOffset;
            EndOffset = endOffset;
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
    }
}