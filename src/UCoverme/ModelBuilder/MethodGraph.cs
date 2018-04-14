using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using UCoverme.Model;
using UCoverme.ModelBuilder.Nodes;

namespace UCoverme.ModelBuilder
{
    public class MethodGraph
    {
        public Condition[] Conditions { get; }
        public Branch[] Branches { get; }
        public Instruction[] Instructions { get; }
        public SequencePoint[] SequencePoints { get; }

        private readonly List<Branch> _generatedFinallyHandlers;

        private MethodGraph(MethodDefinition method)
        {
            Instructions = method.Body.Instructions.OrderBy(i => i.Offset).ToArray();
            var nodeCache = new NodeCache();
            nodeCache.Create(Instructions[0], out var startingNode);
            startingNode.ParseChild(nodeCache);

            SequencePoints = GetSequencePoints(method);
            _generatedFinallyHandlers = GetGeneratedFinallyHandlers(method);

            Conditions = GetConditions(nodeCache);
            Branches = GetBranches();
        }

        private SequencePoint[] GetSequencePoints(MethodDefinition method)
        {
            return method.DebugInformation.SequencePoints.ToArray();
        }


        private List<Branch> GetGeneratedFinallyHandlers(MethodDefinition method)
        {
            int generatedBranchId = 0; // this is whatever, the offsets are the key
            var generatedFinallyHandlers = method.Body.ExceptionHandlers
                .Where(handler => handler.HandlerType == ExceptionHandlerType.Finally &&
                                  !SequencePoints.Any(sp =>
                                      sp.Offset >= handler.HandlerStart.Offset &&
                                      sp.Offset < handler.HandlerEnd.Offset &&
                                      !sp.IsHidden))
                .Select(handler => new Branch(
                    generatedBranchId++,
                    handler.HandlerStart.Offset,
                    GetOffsetOfPreviousEndFinally(handler.HandlerEnd)
                ));

            return generatedFinallyHandlers.ToList();
        }

        private int GetOffsetOfPreviousEndFinally(Instruction handlerEndInstruction)
        {
            // the HandlerEnd points to the next instruction following an endfinally
            var indexOfNextInstruction = Array.IndexOf(Instructions, handlerEndInstruction);
            var endFinally = Instructions[indexOfNextInstruction - 1];
            if (endFinally.OpCode != OpCodes.Endfinally)
            {
                throw new InvalidOperationException("The previous instruction is not an endfinally");
            }

            return endFinally.Offset;
        }

        public static MethodGraph Build(MethodDefinition method)
        {
            return new MethodGraph(method);
        }

        private Branch[] GetBranches()
        {
            var branchStartOffsets = Conditions
                .Select(lp => lp.Target.Instruction.Offset)
                .Prepend(Instructions[0].Offset)
                .OrderBy(offset => offset)
                .Distinct()
                .ToArray();

            var retInstructions = Instructions.Where(i => i.OpCode.FlowControl == FlowControl.Return && !InGeneratedFinally(i.Offset)).Select(i => i.Offset);
            var throwInstructions = Instructions.Where(i => i.OpCode.FlowControl == FlowControl.Throw && !InGeneratedFinally(i.Offset)).Select(i => i.Offset);

            var branchExitOffsets = Conditions
                .Select(lp => lp.Start.Instruction.Offset)
                .Union(retInstructions)
                .Union(throwInstructions)
                .OrderBy(offset => offset)
                .Distinct()
                .ToArray();

            if (branchStartOffsets.Length != branchExitOffsets.Length)
            {
                throw new InvalidOperationException("The start and the end offset count is not equal.");
            }

            var branches = new Branch[branchStartOffsets.Length];
            for (int i = 0; i < branchStartOffsets.Length; i++)
            {
                branches[i] = new Branch(i, branchStartOffsets[i], branchExitOffsets[i]);
            }

            return branches;
        }

        private bool InGeneratedFinally(int instructionOffset)
        {
            return _generatedFinallyHandlers.Any(handler =>
                instructionOffset >= handler.StartOffset &&
                instructionOffset <= handler.EndOffset);
        }

        private Condition[] GetConditions(NodeCache nodeCache)
        {
            var conditions = new List<Condition>();

            var nodesWithMultipleExits = nodeCache
                .GetNodesWithMultipleExits()
                .Where(node => !ExitsIntoGeneratedFinally(node));

            conditions.AddRange(
                nodesWithMultipleExits
                    .SelectMany(
                        node => node.ExitNodes,
                        (start, target) => new Condition(start, target)));

            var nodesWithMultipleEnters = nodeCache
                .GetNodesWithMultipleEnters()
                .Where(node => !EnteredFromGeneratedFinally(node));

            conditions.AddRange(
                nodesWithMultipleEnters
                    .SelectMany(
                        node => node.NodesEntering,
                        (target, start) => new Condition(start, target)));

            return conditions.Distinct().ToArray();
        }

        private bool EnteredFromGeneratedFinally(InstructionNode node)
        {
            return node
                .NodesEntering
                .Any(entry =>
                    _generatedFinallyHandlers
                        .Any(branch => 
                            entry.Instruction.Offset >= branch.StartOffset &&
                            entry.Instruction.Offset <= branch.EndOffset));
        }

        private bool ExitsIntoGeneratedFinally(InstructionNode node)
        {
            return node
                .ExitNodes
                .Any(exit => 
                    _generatedFinallyHandlers
                        .Any(branch => 
                            exit.Instruction.Offset >= branch.StartOffset &&
                            exit.Instruction.Offset <= branch.EndOffset));
        }
    }
}