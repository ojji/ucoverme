using System.Collections.Generic;
using System.Linq;
using UCoverme.Model;

namespace UCoverme.Report
{
    public class OpenCoverSequencePoint
    {
        private readonly List<OpenCoverBranchingPoint> _branchingPoints;
        public int VisitCount { get; }
        public int StartOffset { get; }
        public int EndOffset { get; }
        public int StartLine { get; }
        public int StartColumn { get; }
        public int EndLine { get; }
        public int EndColumn { get; }
        public int BranchExitCount => _branchingPoints.Count;
        public int BranchExitVisited => _branchingPoints.Count(bp => bp.VisitCount > 0);
        public int? FileId { get; }
        public bool IsHidden { get; }

        public OpenCoverSequencePoint(InstrumentedSequencePoint sequencePoint)
        {
            VisitCount = sequencePoint.VisitCount;
            StartOffset = sequencePoint.StartOffset;
            EndOffset = sequencePoint.EndOffset;
            StartLine = sequencePoint.StartLine;
            StartColumn = sequencePoint.StartColumn;
            EndLine = sequencePoint.EndLine;
            EndColumn = sequencePoint.EndColumn;
            _branchingPoints = new List<OpenCoverBranchingPoint>();
            FileId = sequencePoint.FileId;
            IsHidden = sequencePoint.IsHidden;
        }

        public void AddBranchingPoint(OpenCoverBranchingPoint branchingPoint)
        {
            _branchingPoints.Add(branchingPoint);
        }
    }
}