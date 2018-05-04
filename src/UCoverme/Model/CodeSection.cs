using System;
using Newtonsoft.Json;

namespace UCoverme.Model
{
    public class CodeSection : ICodeSection, IEquatable<CodeSection>
    {
        public int StartOffset { get; }
        public int EndOffset { get; }

        [JsonConstructor]
        public CodeSection(int startOffset, int endOffset)
        {
            StartOffset = startOffset;
            EndOffset = endOffset;
        }

        public static bool Intersects(ICodeSection first, ICodeSection second)
        {
            if (!(first.EndOffset < second.StartOffset) &&
                !(first.StartOffset > second.EndOffset))
            {
                return true;
            }

            return false;
        }

        public static bool Intersects(ICodeSection codeSection, int instructionOffset)
        {
            return instructionOffset >= codeSection.StartOffset && 
                   instructionOffset <= codeSection.EndOffset;
        }
        
        public bool Equals(CodeSection other)
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
            return Equals((CodeSection)obj);
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