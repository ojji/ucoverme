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
        private CodeSection[] _codeSections;
        
        private static readonly OpCode[] CachedAnonymousDelegateInstructions = new[]
        {
            OpCodes.Ldsfld,
            OpCodes.Dup,
            OpCodes.Brtrue_S,
            OpCodes.Pop,
            OpCodes.Ldsfld,
            OpCodes.Ldftn,
            OpCodes.Newobj,
            OpCodes.Dup,
            OpCodes.Stsfld,
            OpCodes.Newobj
        };

        public NodeCache()
        {
            _generatedNodes = new Dictionary<int, InstructionNode>();
        }

        private List<CodeSection> FindSpecialCodeSections(Dictionary<int, InstructionNode> generatedNodes)
        {
            var specialSections = new List<CodeSection>();
            FindCachedAnonymousDelegates(generatedNodes.Values.OrderBy(instruction => instruction.Instruction.Offset).Select(instruction => instruction.Instruction).ToArray(), specialSections);

            return specialSections;
        }

        private void FindCachedAnonymousDelegates(Instruction[] instructions, List<CodeSection> specialSections)
        {
            for (int idx = 0; idx < instructions.Length - CachedAnonymousDelegateInstructions.Length + 1; idx++)
            {
                for (int innerIdx = 0; innerIdx < CachedAnonymousDelegateInstructions.Length; innerIdx++)
                {
                    if (instructions[idx + innerIdx].OpCode != CachedAnonymousDelegateInstructions[innerIdx])
                    {
                        break;
                    }

                    if (innerIdx == CachedAnonymousDelegateInstructions.Length - 1)
                    {
                        specialSections.Add(
                            new CodeSection(
                                instructions[idx].Offset, 
                                instructions[idx + innerIdx].Offset));
                    }
                }
            }
        }

        public CodeSection[] GetCodeSections()
        {
            if (_codeSections == null)
            {
                var speciallyTreatedCodeSections = FindSpecialCodeSections(_generatedNodes);
                var codeSections = new List<CodeSection>();
                var nodes = _generatedNodes.Values.OrderBy(node => node.Instruction.Offset).ToArray();

                var currentStart = nodes[0];
                var currentEnd = nodes[0];
                int i = 0;
                while (i < nodes.Length)
                {
                    if ((IsUnoptimizableBranchingNode(currentEnd, i, nodes) ||
                        currentEnd is ReturnNode ||
                        currentEnd is ThrowNode ||
                        NextNodeHasMultipleEnters(i, nodes)) &&
                        !speciallyTreatedCodeSections.Any(section => CodeSection.Intersects(section, currentEnd.Instruction.Offset)))
                    {
                        var codeSection = new CodeSection(currentStart.Instruction.Offset,
                            currentEnd.Instruction.Offset);
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

            if (instruction.OpCode.FlowControl == FlowControl.Cond_Branch ||
                instruction.OpCode.FlowControl == FlowControl.Branch)
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

        public bool HasMultipleExits(CodeSection section)
        {
            return _generatedNodes[section.EndOffset].ExitCount > 1;
        }

        public List<Condition> GetExitConditions(CodeSection section)
        {
            return _generatedNodes[section.EndOffset]
                .ExitNodes
                .Select(exitNode => new Condition(
                    section.EndOffset, 
                    exitNode.Instruction.Offset))
                .ToList();
        }

        public bool ExitsIntoGeneratedFinally(Condition condition, List<CodeSection> generatedFinallyHandlers)
        {
            return _generatedNodes[condition.StartOffset]
                .ExitNodes
                .Any(exit =>
                    generatedFinallyHandlers.Any(handler =>
                        CodeSection.Intersects(handler, exit.Instruction.Offset)
                    ));
        }

        public bool HasMultipleEnters(CodeSection section)
        {
            return _generatedNodes[section.StartOffset].EnterCount > 1;
        }

        public List<Condition> GetEnterConditions(CodeSection section)
        {
            return _generatedNodes[section.StartOffset]
                .NodesEntering
                .Select(enterNode => new Condition(
                    enterNode.Instruction.Offset,
                    section.StartOffset))
                .ToList();
        }

        public bool EnteredFromGeneratedFinally(Condition condition, List<CodeSection> generatedFinallyHandlers)
        {
            return _generatedNodes[condition.EndOffset]
                .NodesEntering
                .Any(enter =>
                    generatedFinallyHandlers.Any(handler =>
                        CodeSection.Intersects(handler, enter.Instruction.Offset)
                    ));
        }
    }
}