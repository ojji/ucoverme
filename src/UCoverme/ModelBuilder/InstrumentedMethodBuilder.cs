using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using UCoverme.Model;
using UCoverme.ModelBuilder.Nodes;

namespace UCoverme.ModelBuilder
{
    public class InstrumentedMethodBuilder
    {
        public Condition[] Conditions { get; }
        public Branch[] Branches { get; }
        public Instruction[] Instructions { get; }
        public InstrumentedSequencePoint[] SequencePoints { get; }

        private readonly List<Branch> _generatedFinallyHandlers;

        private InstrumentedMethodBuilder(MethodDefinition method)
        {
            Instructions = method.Body.Instructions.OrderBy(i => i.Offset).ToArray();
            var nodeCache = new NodeCache();
            nodeCache.Create(Instructions[0], out var startingNode);
            startingNode.ParseChild(nodeCache);

            var sequencePoints = method.DebugInformation.SequencePoints.OrderBy(sp => sp.Offset).ToArray();
            SequencePoints = GetSequencePoints(sequencePoints);
            _generatedFinallyHandlers = GetGeneratedFinallyHandlers(method, sequencePoints);

            
            Conditions = GetConditions(nodeCache);
            Branches = MergeGeneratedCodeSections(nodeCache);
        }

        private Branch[] MergeGeneratedCodeSections(NodeCache nodeCache)
        {
            var codeSections = nodeCache.GetCodeSections();
            List<Branch> sections = new List<Branch>();
            int id = 0;

            var inGeneratedSection = false;
            var startIndex = -1;
            for (int i = 0; i < codeSections.Length - 1; i++)
            {
                if (!inGeneratedSection &&
                    InGeneratedFinally(codeSections[i + 1].StartOffset))
                {
                    inGeneratedSection = true;
                    startIndex = i;
                }
                else if (!inGeneratedSection &&
                         !InGeneratedFinally(codeSections[i + 1].StartOffset))
                {
                    sections.Add(new Branch(id++, codeSections[i].StartOffset, codeSections[i].EndOffset));
                } 
                else if (inGeneratedSection &&
                         InGeneratedFinally(codeSections[i + 1].StartOffset))
                {
                    continue;
                } else if (inGeneratedSection &&
                           !InGeneratedFinally(codeSections[i + 1].StartOffset))
                {
                    sections.Add(
                        new Branch(
                            id++, 
                            codeSections[startIndex].StartOffset,
                            codeSections[i + 1].EndOffset));
                    startIndex = -1;
                    i++;
                    inGeneratedSection = false;
                }
            }

            var lastSectionAdded = sections.LastOrDefault();
            var lastSectionInOriginal = codeSections.Last();
            if (lastSectionAdded == null ||
                lastSectionAdded.EndOffset != lastSectionInOriginal.EndOffset)
            {
                sections.Add(
                    new Branch(id, 
                        lastSectionInOriginal.StartOffset, 
                        lastSectionInOriginal.EndOffset));
            }

            return sections.ToArray();
        }

        private Condition[] GetConditions(NodeCache nodeCache)
        {
            var codeSections = nodeCache.GetCodeSections();
            var exitConditions = codeSections.Where(section => section.HasMultipleExits)
                .SelectMany(section => section.GetExitConditions())
                .Where(condition => !ExitsIntoGeneratedFinally(condition.Start)).ToList();

            var enterConditions = codeSections.Where(section => section.HasMultipleEnters)
                .SelectMany(section => section.GetEnterConditions())
                .Where(condition => !EnteredFromGeneratedFinally(condition.Target)).ToList();

            return exitConditions.Concat(enterConditions).Distinct().ToArray();
        }
        
        private InstrumentedSequencePoint[] GetSequencePoints(SequencePoint[] sequencePoints)
        {
            List<InstrumentedSequencePoint> instrumentedSequencePoints = new List<InstrumentedSequencePoint>();
            for (int i = 0; i < sequencePoints.Length; i++)
            {
                int startOffset = sequencePoints[i].Offset;
                int nextStartOffset = i + 1 < sequencePoints.Length ? sequencePoints[i + 1].Offset : int.MaxValue;
                int endOffset = Instructions.Select(instruction => instruction.Offset).SkipWhile(offset => offset < startOffset)
                    .TakeWhile(offset => offset < nextStartOffset)
                    .Last();

                instrumentedSequencePoints.Add(
                    new InstrumentedSequencePoint(
                        i,
                        sequencePoints[i].Document.Url,
                        startOffset,
                        endOffset,
                        sequencePoints[i].StartLine,
                        sequencePoints[i].EndLine,
                        sequencePoints[i].StartColumn,
                        sequencePoints[i].EndColumn
                        ));
            }

            return instrumentedSequencePoints.ToArray();
        }


        private List<Branch> GetGeneratedFinallyHandlers(MethodDefinition method, SequencePoint[] sequencePoints)
        {
            int generatedBranchId = 0; // this is whatever, the offsets are the key
            var generatedFinallyHandlers = method.Body.ExceptionHandlers
                .Where(handler => handler.HandlerType == ExceptionHandlerType.Finally &&
                                  !sequencePoints.Any(sp =>
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

        private bool InGeneratedFinally(int instructionOffset)
        {
            return _generatedFinallyHandlers.Any(handler =>
                instructionOffset >= handler.StartOffset &&
                instructionOffset <= handler.EndOffset);
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

        public static InstrumentedMethodBuilder Build(MethodDefinition method)
        {
            return new InstrumentedMethodBuilder(method);
        }
    }
}