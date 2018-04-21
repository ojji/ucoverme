using System.Collections.Generic;
using System.Linq;
using Mono.Cecil.Cil;
using UCoverme.Model;
using UCoverme.ModelBuilder.Nodes;

namespace UCoverme.ModelBuilder
{
    public class NodeCache
    {
        private readonly Dictionary<int, InstructionNode> _generatedNodes;
        private Branch[] _codeSections;

        public NodeCache()
        {
            _generatedNodes = new Dictionary<int, InstructionNode>();
        }

        public Branch[] GetCodeSections()
        {
            if (_codeSections == null)
            {
                var codeSections = new List<Branch>();
                var nodes = _generatedNodes.Values.OrderBy(node => node.Instruction.Offset).ToArray();

                var currentId = 0;
                var currentStart = nodes[0];
                var currentEnd = nodes[0];
                int i = 0;
                while (i < nodes.Length)
                {
                    if (IsUnoptimizableBranchingNode(currentEnd, i, nodes) ||
                        currentEnd is ReturnNode ||
                        currentEnd is ThrowNode ||
                        NextNodeHasMultipleEnters(i, nodes))
                    {
                        var codeSection = new Branch(
                            currentId++,
                            currentStart,
                            currentEnd);

                        codeSections.Add(codeSection);

                        if (i + 1 >= nodes.Length)
                        {
                            break;
                        }

                        currentStart = nodes[i + 1];
                    }

                    currentEnd = nodes[i + 1];
                    i++;
                }

                _codeSections = codeSections.OrderBy(s => s.StartOffset).ToArray();
            }

            return _codeSections;
        }

        private bool NextNodeHasMultipleEnters(int currentIndex, InstructionNode[] nodes)
        {
            return currentIndex + 1 < nodes.Length &&
                   nodes[currentIndex + 1].EnterCount > 1;
        }

        private bool IsUnoptimizableBranchingNode(InstructionNode currentNode, int currentIndex,
            InstructionNode[] nodes)
        {
            if (!(currentNode is BranchingNode))
            {
                return false;
            }

            // 1) this node can be a real branching node with multiple exits
            if (currentNode.ExitCount > 1)
            {
                return true;
            }

            // 2) or if it is an unconditional branch we have to check 
            // if the exit points to the next code section, and whether 
            // it can be merged with the current section or not.
            var nextNode = currentIndex + 1 < nodes.Length ? nodes[currentIndex + 1] : null;
            if (nextNode != null &&
                currentNode.ExitNodes.Single().Instruction.Offset == nextNode.Instruction.Offset &&
                nextNode.EnterCount == 1)
            {
                return false;
            }

            return true;
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