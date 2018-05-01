using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using UCoverme.Model;

namespace UCoverme.ModelBuilder
{
    public abstract class MethodBuilderBase
    {
        public Condition[] Conditions { get; protected set; }
        public Branch[] Branches { get; protected set; }
        public Instruction[] Instructions { get; }
        public InstrumentedSequencePoint[] SequencePoints { get; protected set; }
        public int? FileId { get; }

        protected NodeCache NodeCache { get; }

        protected MethodBuilderBase(MethodDefinition method, int? fileId)
        {
            FileId = fileId;
            Instructions = method.Body.Instructions.OrderBy(i => i.Offset).ToArray();
            NodeCache = new NodeCache();
            NodeCache.Create(Instructions[0], out var startingNode);
            startingNode.ParseChild(NodeCache);
        }

    }
}