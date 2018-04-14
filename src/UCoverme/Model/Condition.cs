using System;
using UCoverme.ModelBuilder.Nodes;

namespace UCoverme.Model
{
    public class Condition : IEquatable<Condition>
    {
        public InstructionNode Start { get; }
        public InstructionNode Target { get; }

        public Condition(InstructionNode start, InstructionNode target)
        {
            Start = start;
            Target = target;
        }

        public override string ToString()
        {
            string start = Start == null ? "no start node" : Start.ToString();
            return $"{start}  -->  {Target}";
        }

        public bool Equals(Condition other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(Start, other.Start) && Equals(Target, other.Target);
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
                return ((Start != null ? Start.GetHashCode() : 0) * 397) ^ (Target != null ? Target.GetHashCode() : 0);
            }
        }
    }
}