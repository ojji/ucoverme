using Mono.Cecil.Cil;

namespace UCoverme.ModelBuilder.Nodes
{
    public class ReturnNode : InstructionNode
    {
        public ReturnNode(Instruction instruction) : base(instruction)
        {
        }

        public override void ParseChild(NodeCache nodeCache)
        {
        }
    }
}