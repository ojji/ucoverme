using System;
using Mono.Cecil.Cil;
using UCoverme.ModelBuilder.Filters;

namespace UCoverme.Model
{
    public class InstrumentedMethod : ISkipable
    {
        public InstrumentedClass ContainingClass { get; private set; }
        public string Name { get; }
        public int MethodId { get; }
        public Condition[] Conditions { get; }
        public Branch[] Branches { get; }
        public InstrumentedSequencePoint[] SequencePoints { get; }
        public Instruction[] Instructions { get; }

        public bool IsSkipped => SkipReason != SkipReason.NoSkip;
        public SkipReason SkipReason { get; private set; }

        public InstrumentedMethod(string name, int methodId, Branch[] branches, Condition[] conditions, InstrumentedSequencePoint[] sequencePoints, Instruction[] instructions)
        {
            Name = name;
            MethodId = methodId;
            Conditions = conditions;
            Branches = branches;
            SequencePoints = sequencePoints;
            Instructions = instructions;
            SkipReason = SkipReason.NoSkip;
        }
        
        public void SetContainingClass(InstrumentedClass instrumentedClass)
        {
            ContainingClass = instrumentedClass;
        }

        public override string ToString()
        {
            return $"{MethodId} - {Name}";
        }

        public void SkipFromInstrumentation(SkipReason reason)
        {
            SkipReason = reason;
        }

        public void Unskip()
        {
            SkipReason = SkipReason.NoSkip;
        }

        public void ApplyFilter(IFilter filter)
        {
            throw new NotImplementedException();
        }
    }
}
