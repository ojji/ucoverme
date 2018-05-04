using System;
using System.Threading;
using Mono.Cecil.Cil;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UCoverme.ModelBuilder.Filters;

namespace UCoverme.Model
{
    public class InstrumentedMethod : ISkipable
    {
        public string Name { get; }
        public int MethodId { get; }
        public Condition[] Conditions { get; }
        public Branch[] Branches { get; }
        public InstrumentedSequencePoint[] SequencePoints { get; }

        [JsonIgnore] 
        public int VisitCount => _visitCount;
        private int _visitCount;

        [JsonIgnore]
        public Instruction[] Instructions { get; }

        public bool IsSkipped => SkipReason != SkipReason.NoSkip;
        [JsonConverter(typeof(StringEnumConverter))]
        public SkipReason SkipReason { get; private set; }

        [JsonConstructor]
        private InstrumentedMethod(string name, int methodId, Branch[] branches, Condition[] conditions, InstrumentedSequencePoint[] sequencePoints, SkipReason skipReason)
        {
            Name = name;
            MethodId = methodId;
            Conditions = conditions;
            Branches = branches;
            SequencePoints = sequencePoints;
            SkipReason = skipReason;
            _visitCount = 0;
        }

        public InstrumentedMethod(string name, int methodId, Branch[] branches, Condition[] conditions, InstrumentedSequencePoint[] sequencePoints, Instruction[] instructions)
        {
            Name = name;
            MethodId = methodId;
            Conditions = conditions;
            Branches = branches;
            SequencePoints = sequencePoints;
            Instructions = instructions;
            SkipReason = SkipReason.NoSkip;
            _visitCount = 0;
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

        public int CalculateCyclomaticComplexity()
        {
            // todo
            return 0;
        }

        public void Visit()
        {
            Interlocked.Increment(ref _visitCount);
        }
    }
}
