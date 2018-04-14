using Mono.Cecil.Cil;

namespace UCoverme.ModelBuilder.Nodes
{
    public class ThrowNode : InstructionNode
    {
        public ThrowNode(Instruction instruction) : base(instruction)
        {
        }

        public override void ParseChild(NodeCache nodeCache)
        {
        }
    }
}