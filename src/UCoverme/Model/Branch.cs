using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UCoverme.ModelBuilder.Nodes;

namespace UCoverme.Model
{
    public class Branch : ICodeSegment
    {
        public int Id { get; }
        public int StartOffset { get; }
        public int EndOffset { get; }

        [JsonIgnore]
        public bool HasMultipleEnters => _start.EnterCount > 1;
        [JsonIgnore]
        public bool HasMultipleExits => _end.ExitCount > 1;
        [JsonIgnore]
        private readonly InstructionNode _start;
        [JsonIgnore]
        private readonly InstructionNode _end;

        [JsonConstructor]
        public Branch(int id, int startOffset, int endOffset)
        {
            Id = id;
            StartOffset = startOffset;
            EndOffset = endOffset;
        }

        public Branch(int id, InstructionNode start, InstructionNode end)
        {
            Id = id;
            _start = start;
            _end = end;
            StartOffset = _start.Instruction.Offset;
            EndOffset = _end.Instruction.Offset;
        }

        public List<Condition> GetEnterConditions()
        {
            return _start.NodesEntering.Select(enterNode => new Condition(enterNode, _start)).ToList();
        }

        public List<Condition> GetExitConditions()
        {
            return _end.ExitNodes.Select(exit => new Condition(_end, exit)).ToList();
        }

        public override string ToString()
        {
            return $"[{StartOffset}, {_start.Instruction}] - [{EndOffset}, {_end.Instruction}]";
        }
    }
}