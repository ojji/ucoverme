using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace UCoverme.Model
{
    public class InstrumentedSequencePoint : ICodeSegment
    {
        public int Id { get; }
        public string FilePath { get; }
        public int StartOffset { get; }
        public int EndOffset { get; }
        public int StartLine { get; }
        public int EndLine { get; }
        public int StartColumn { get; }
        public int EndColumn { get; }
        public bool IsHidden => StartLine == HiddenLine && EndLine == HiddenLine;

        private const int HiddenLine = 0xFEEFEE;

        [JsonConstructor]
        public InstrumentedSequencePoint(int id, string filePath, int startOffset, int endOffset, int startLine, int endLine, int startColumn,
            int endColumn)
        {
            Id = id;
            FilePath = filePath;
            StartOffset = startOffset;
            EndOffset = endOffset;
            StartLine = startLine;
            EndLine = endLine;
            StartColumn = startColumn;
            EndColumn = endColumn;
        }

        public string GetTextFromSource()
        {
            StringBuilder builder = new StringBuilder();
            int lineNumber = 1;
            using (var reader = new StreamReader(File.OpenRead(FilePath)))
            {
                string srcLine;
                while ((srcLine = reader.ReadLine()) != null && lineNumber <= EndLine)
                {
                    if (lineNumber >= StartLine && lineNumber <= EndLine)
                    {
                        if (lineNumber == StartLine && lineNumber == EndLine)
                        {
                            srcLine = srcLine.Substring(StartColumn - 1, EndColumn - StartColumn);
                        }
                        else if (lineNumber == StartLine)
                        {
                            srcLine = srcLine.Substring(StartColumn - 1);
                        }
                        else if (lineNumber == EndLine)
                        {
                            srcLine = srcLine.Substring(0, EndColumn - 1);
                        }

                        builder.AppendLine(srcLine);
                    }

                    lineNumber++;
                }
            }

            return builder.ToString();
        }

        public override string ToString()
        {
            return IsHidden ? $"// [{Id}] [{StartOffset}-{EndOffset}] [hidden]" : $"// [{Id}] [{StartOffset}-{EndOffset}] [{StartLine} {StartColumn} - {EndLine} {EndColumn}]";
        }
    }
}