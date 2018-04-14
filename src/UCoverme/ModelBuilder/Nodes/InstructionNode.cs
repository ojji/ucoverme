using System;
using System.Collections.Generic;
using Mono.Cecil.Cil;

namespace UCoverme.ModelBuilder.Nodes
{
    public abstract class InstructionNode : IEquatable<InstructionNode>
    {
        protected InstructionNode(Instruction instruction)
        {
            Instruction = instruction;
            NodesEntering = new HashSet<InstructionNode>();
            ExitNodes = new HashSet<InstructionNode>();
        }

        public Instruction Instruction { get; }

        public int EnterCount => NodesEntering.Count;
        public HashSet<InstructionNode> NodesEntering { get; }

        public HashSet<InstructionNode> ExitNodes { get; }

        public int ExitCount => ExitNodes.Count;

        public abstract void ParseChild(NodeCache nodeCache);

        public void AddParent(InstructionNode nodeEntering)
        {
            if (!NodesEntering.Contains(nodeEntering))
            {
                NodesEntering.Add(nodeEntering);
            }
        }

        public bool Equals(InstructionNode other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Instruction.Offset == other.Instruction.Offset;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((InstructionNode) obj);
        }

        public override int GetHashCode()
        {
            return Instruction.Offset.GetHashCode();
        }

        public override string ToString()
        {
            return $"offset: {Instruction.Offset}, cmd: {Instruction}, #nodesEntering: {EnterCount}, #nodeExits: {ExitCount}";
        }
    }
}