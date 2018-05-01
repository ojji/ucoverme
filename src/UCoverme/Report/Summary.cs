namespace UCoverme.Report
{
    public class Summary
    {
        public int NumSequencePoints { get; set; }
        public int VisitedSequencePoints { get; set; }
        public int NumBranchPoints { get; set; }
        public int VisitedBranchPoints { get; set; }
        public decimal SequenceCoverage { get; set; }
        public decimal BranchCoverage { get; set; }
        public int MinCyclomaticComplexity { get; set; }
        public int MaxCyclomaticComplexity { get; set; }
        public int NumClasses { get; set; }
        public int VisitedClasses { get; set; }
        public int NumMethods { get; set; }
        public int VisitedMethods { get; set; }
    }
}