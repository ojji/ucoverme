using System;
using System.Linq;
using UCoverme.Model;

namespace UCoverme.Report
{
    public class OpenCoverMethod
    {
        public Summary Summary { get; }
        public string Name { get; }
        public int MethodId { get; }
        public int VisitCount { get; }
        public OpenCoverSequencePoint MethodPoint { get; }
        public bool HasVisibleSequencePoint => _sequencePoints.Any(sp => !sp.IsHidden);
        public int NPathComplexity { get; }
        public OpenCoverSequencePoint[] SequencePoints { get; }
        public OpenCoverBranchingPoint[] BranchingPoints { get; }
        public bool IsConstructor => 
            Name.EndsWith("::.ctor()") || 
            Name.EndsWith("::.cctor()");
        public bool IsStatic => false; // todo
        public bool IsGetter => false; // todo
        public bool IsSetter => false; // todo

        public OpenCoverMethod(OpenCoverReport report, InstrumentedMethod method)
        {
            Summary = report.GetSummaryForMethod(method);
            _sequencePoints = method.SequencePoints;
            Name = method.Name;
            MethodId = method.MethodId;
            VisitCount = method.VisitCount;
            var methodPoint = method.SequencePoints.FirstOrDefault();
            MethodPoint = methodPoint != null
                ? method.SequencePoints.Select(sp => new OpenCoverSequencePoint(sp)).First()
                : null;
            NPathComplexity = CalculateNPathComplexity(method);
            SequencePoints = method.SequencePoints.Where(sp => !sp.IsHidden).OrderBy(sp => sp.StartOffset).Select(sp => new OpenCoverSequencePoint(sp)).ToArray();
            BranchingPoints = GetBranchingPoints(method)
                .Where(bp => bp.HasVisibleSequencePointAssociated)
                .ToArray();
            AddBranchPointsToSequencePoints(BranchingPoints, SequencePoints);
        }

        private void AddBranchPointsToSequencePoints(OpenCoverBranchingPoint[] branchingPoints, OpenCoverSequencePoint[] sequencePoints)
        {
            foreach (var branchingPoint in branchingPoints)
            {
                if (branchingPoint.HasVisibleSequencePointAssociated)
                {
                    sequencePoints.First(sp => sp.StartOffset == branchingPoint.ClosestVisibleSequencePoint.StartOffset).AddBranchingPoint(branchingPoint);
                }
            }
        }

        private OpenCoverBranchingPoint[] GetBranchingPoints(InstrumentedMethod method)
        {
            return method
                .Conditions
                .Where(condition =>
                    method
                        .Conditions
                        .Any(otherCondition =>
                            otherCondition.StartOffset == condition.StartOffset &&
                            otherCondition.EndOffset != condition.EndOffset))
                .Select(condition =>
                    new OpenCoverBranchingPoint(
                        condition.StartOffset,
                        condition.EndOffset,
                        condition.VisitCount,
                        method.SequencePoints))
                .ToArray();
        }
        
        private int CalculateNPathComplexity(InstrumentedMethod method)
        {
            // todo
            return 0;
        }

        public override string ToString()
        {
            return $"{Name}";
        }

        private readonly InstrumentedSequencePoint[] _sequencePoints;
    }
}