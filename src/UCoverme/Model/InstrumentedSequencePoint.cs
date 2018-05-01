using System.Threading;
using Newtonsoft.Json;

namespace UCoverme.Model
{
    public class InstrumentedSequencePoint : ICodeSection
    {
        public int Id { get; }
        public int StartOffset { get; }
        public int EndOffset { get; }
        public int? FileId { get; }
        public int StartLine { get; }
        public int EndLine { get; }
        public int StartColumn { get; }
        public int EndColumn { get; }
        public bool IsHidden => StartLine == HiddenLine && EndLine == HiddenLine;

        [JsonIgnore] 
        public int VisitCount => _visitCount;

        private const int HiddenLine = 0xFEEFEE;

        [JsonIgnore]
        private int _visitCount;

        [JsonConstructor]
        public InstrumentedSequencePoint(int id, int? fileId, 
            int startOffset, int endOffset,
            int startLine, int endLine, 
            int startColumn, int endColumn)
        {
            Id = id;
            StartOffset = startOffset;
            EndOffset = endOffset;
            FileId = fileId;
            StartLine = startLine;
            EndLine = endLine;
            StartColumn = startColumn;
            EndColumn = endColumn;
            _visitCount = 0;
        }

        public void Visit()
        {
            Interlocked.Increment(ref _visitCount);
        }

        public override string ToString()
        {
            return IsHidden
                ? $"// [{Id}] [{StartOffset}-{EndOffset}] [hidden]"
                : $"// [{Id}] [{StartOffset}-{EndOffset}] [{StartLine} {StartColumn} - {EndLine} {EndColumn}]";
        }
    }
}