using System.Collections.Generic;
using System.Linq;
using Mono.Cecil.Cil;
using UCoverme.ModelBuilder.Nodes;

namespace UCoverme.ModelBuilder
{
    public class NodeCache
    {
        private readonly Dictionary<int, InstructionNode> _generatedNodes;

        public NodeCache()
        {
            _generatedNodes = new Dictionary<int, InstructionNode>();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="instruction"></param>
        /// <param name="nodeReturned"></param>
        /// <returns>If the node was already created then returns true.</returns>
        public bool Create(Instruction instruction, out InstructionNode nodeReturned)
        {
            if (_generatedNodes.ContainsKey(instruction.Offset))
            {
                nodeReturned = _generatedNodes[instruction.Offset];
                return true;
            }

            if (instruction.OpCode.FlowControl == FlowControl.Cond_Branch || instruction.OpCode.FlowControl == FlowControl.Branch)
            {
                nodeReturned = new BranchingNode(instruction);
            }
            else if (instruction.OpCode.FlowControl == FlowControl.Return)
            {
                nodeReturned = new ReturnNode(instruction);
            }
            else if (instruction.OpCode.FlowControl == FlowControl.Throw)
            {
                nodeReturned = new ThrowNode(instruction);
            }
            else
            {
                nodeReturned = new SequentialNode(instruction);
            }

            _generatedNodes.Add(nodeReturned.Instruction.Offset, nodeReturned);
            return false;
        }

        public IEnumerable<InstructionNode> GetNodesWithMultipleEnters()
        {
            return _generatedNodes.Values.Where(node => node.EnterCount > 1);
        }

        public IEnumerable<InstructionNode> GetNodesWithMultipleExits()
        {
            return _generatedNodes.Values.Where(node => node.ExitCount > 1);
        }
    }
}