using Mono.Cecil.Cil;

namespace UCoverme.ModelBuilder.Nodes
{
    public class SequentialNode : InstructionNode
    {
        public SequentialNode(Instruction instruction) : base(instruction)
        {
        }

        public override void ParseChild(NodeCache nodeCache)
        {
            var alreadyVisited = nodeCache.Create(Instruction.Next, out var nextNode);
            nextNode.AddParent(this);
            ExitNodes.Add(nextNode);
            if (!alreadyVisited)
            {
                nextNode.ParseChild(nodeCache);
            }
        }
    }
}