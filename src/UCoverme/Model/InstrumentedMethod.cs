using System;
using Mono.Cecil.Cil;

namespace UCoverme.Model
{
    public class InstrumentedMethod : ISkipable
    {
        public string Name { get; }
        public int MethodId { get; }
        public Condition[] Conditions { get; }
        public Branch[] Branches { get; }
        public InstrumentedSequencePoint[] SequencePoints { get; }
        public Instruction[] Instructions { get; }

        public InstrumentedMethod(string name, int methodId, Branch[] branches, Condition[] conditions, InstrumentedSequencePoint[] sequencePoints, Instruction[] instructions)
        {
            Name = name;
            MethodId = methodId;
            Conditions = conditions;
            Branches = branches;
            SequencePoints = sequencePoints;
            Instructions = instructions;
        }

        public override string ToString()
        {
            return $"{MethodId} - {Name}";
        }

        public bool IsSkipped => SkipReason != SkipReason.NoSkip;
        public SkipReason SkipReason { get; private set; }

        public void SkipFromInstrumentation(SkipReason reason)
        {
            throw new NotImplementedException();
        }
    }
}
