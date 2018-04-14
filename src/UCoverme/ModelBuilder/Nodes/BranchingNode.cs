using Mono.Cecil.Cil;

namespace UCoverme.ModelBuilder.Nodes
{
    public class BranchingNode : InstructionNode
    {
        public BranchingNode(Instruction instruction) : base(instruction)
        {
        }

        public override void ParseChild(NodeCache nodeCache)
        {
            if (Instruction.OpCode == OpCodes.Br || Instruction.OpCode == OpCodes.Br_S)
            {
                ParseUnconditionalBranch(nodeCache);
            }
            else if (Instruction.OpCode == OpCodes.Switch)
            {
                ParseSwitchBranch(nodeCache);
            }
            else
            {
                ParseConditionalBranch(nodeCache);
            }
        }

        private void ParseUnconditionalBranch(NodeCache nodeCache)
        {
            var alreadyVisitedBranch =
                nodeCache.Create(Instruction.Operand as Instruction, out var branchNode);
            branchNode.AddParent(this);
            ExitNodes.Add(branchNode);
            if (!alreadyVisitedBranch)
            {
                branchNode.ParseChild(nodeCache);
            }
        }

        private void ParseSwitchBranch(NodeCache nodeCache)
        {
            var alreadyVisitedDefault = nodeCache.Create(Instruction.Next, out var defaultBranchNode);
            defaultBranchNode.AddParent(this);
            ExitNodes.Add(defaultBranchNode);
            if (!alreadyVisitedDefault)
            {
                defaultBranchNode.ParseChild(nodeCache);
            }

            foreach (var branchInstruction in (Instruction[])Instruction.Operand)
            {
                var alreadyVisitedBranch = nodeCache.Create(branchInstruction, out var branchNode);
                branchNode.AddParent(this);
                ExitNodes.Add(branchNode);
                if (!alreadyVisitedBranch)
                {
                    branchNode.ParseChild(nodeCache);
                }
            }
        }

        private void ParseConditionalBranch(NodeCache nodeCache)
        {
            var alreadyVisitedElse = nodeCache.Create(Instruction.Next, out var elseBranchNode);
            elseBranchNode.AddParent(this);
            ExitNodes.Add(elseBranchNode);
            if (!alreadyVisitedElse)
            {
                elseBranchNode.ParseChild(nodeCache);
            }

            var alreadyVisitedThen = nodeCache.Create(Instruction.Operand as Instruction, out var thenBranchNode);
            thenBranchNode.AddParent(this);
            ExitNodes.Add(thenBranchNode);
            if (!alreadyVisitedThen)
            {
                thenBranchNode.ParseChild(nodeCache);
            }
        }
    }
}