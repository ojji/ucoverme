using Newtonsoft.Json;

namespace UCoverme.Model
{
    public class CodeSection : ICodeSection
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
    }
}