using System;
using System.Linq;
using UCoverme.Model;

namespace UCoverme.Report
{
    public class OpenCoverBranchingPoint
    {
        public int StartOffset { get; }
        public int EndOffset { get; }
        public int VisitCount { get; }
        public int StartLine
        {
            get
            {
                if (!HasVisibleSequencePointAssociated)
                {
                    throw new InvalidOperationException($"The branching point does not belong to any visible sequence point");
                }
                return ClosestVisibleSequencePoint.StartLine;
            }
        }

        public int FileId
        {
            get
            {
                if (!HasVisibleSequencePointAssociated)
                {
                    throw new InvalidOperationException($"The branching point does not belong to any visible sequence point");
                }
                return (int) ClosestVisibleSequencePoint.FileId;
            }
        }

        public bool HasVisibleSequencePointAssociated => ClosestVisibleSequencePoint != null;

        public int Path => 0; // todo

        public InstrumentedSequencePoint ClosestVisibleSequencePoint { get; }

        public OpenCoverBranchingPoint(int startOffset, int endOffset, int visitCount, InstrumentedSequencePoint[] sequencePoints)
        {
            StartOffset = startOffset;
            EndOffset = endOffset;
            VisitCount = visitCount;
            ClosestVisibleSequencePoint = FindClosestVisibleSequencePoint(sequencePoints, startOffset);
        }

        private InstrumentedSequencePoint FindClosestVisibleSequencePoint(InstrumentedSequencePoint[] sequencePoints, int startOffset)
        {
            InstrumentedSequencePoint visibleSequencePoint = null;

            if (sequencePoints.Length != 0)
            {
                var idx = Array.IndexOf(sequencePoints, sequencePoints.First(sp => CodeSection.Intersects(sp, startOffset)));
                while (idx >= 0)
                {
                    if (!sequencePoints[idx].IsHidden)
                    {
                        visibleSequencePoint = sequencePoints[idx];
                        break;
                    }
                    idx--;
                }
            }

            return visibleSequencePoint;
        }
    }
}