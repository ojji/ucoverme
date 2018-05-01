using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using UCoverme.Model;

namespace UCoverme.ModelBuilder
{
    public class MethodBuilder : MethodBuilderBase
    {
        private readonly List<CodeSection> _generatedFinallyHandlers;

        private MethodBuilder(MethodDefinition method, int? fileId) : base(method, fileId)
        {
            var sequencePoints = method.DebugInformation.SequencePoints.OrderBy(sp => sp.Offset).ToArray();
            SequencePoints = GetSequencePoints(sequencePoints);
            _generatedFinallyHandlers = GetGeneratedFinallyHandlers(method, sequencePoints);
            
            Conditions = GetConditions(NodeCache);
            Branches = MergeGeneratedCodeSections(NodeCache);
            
            var conditionsWithBranches = Conditions.Select(c =>
                new 
                {
                    c.StartOffset,
                    StartBranch = Branches.First(branch => 
                        CodeSection.Intersects(branch, c.StartOffset)),
                    c.EndOffset,
                    EndBranch = Branches.First(branch => 
                        CodeSection.Intersects(branch, c.EndOffset)),
                }).ToArray();

            var branchingPoints = Conditions.Where(condition =>
                {
                    return Conditions.Any(otherCondition =>
                        otherCondition.StartOffset == condition.StartOffset &&
                        otherCondition.EndOffset != condition.EndOffset);
                })
                .Select(c =>
                new
                {
                    c.StartOffset,
                    StartBranch = Branches.First(branch =>
                        CodeSection.Intersects(branch, c.StartOffset)),
                    c.EndOffset,
                    EndBranch = Branches.First(branch =>
                        CodeSection.Intersects(branch, c.EndOffset)),
                }).ToArray();

            var nonBranchingPoints = Conditions.Where(condition =>
                {
                    return !Conditions.Any(otherCondition =>
                        otherCondition.StartOffset == condition.StartOffset &&
                        otherCondition.EndOffset != condition.EndOffset);
                })
                .Select(c =>
                new
                {
                    c.StartOffset,
                    StartBranch = Branches.First(branch =>
                        CodeSection.Intersects(branch, c.StartOffset)),
                    c.EndOffset,
                    EndBranch = Branches.First(branch =>
                        CodeSection.Intersects(branch, c.EndOffset)),
                }).ToArray();
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
            var exitConditions = codeSections.Where(nodeCache.HasMultipleExits)
                .SelectMany(nodeCache.GetExitConditions)
                .Where(condition => !nodeCache.ExitsIntoGeneratedFinally(condition, _generatedFinallyHandlers)).ToList();

            var enterConditions = codeSections.Where(nodeCache.HasMultipleEnters)
                .SelectMany(nodeCache.GetEnterConditions)
                .Where(condition => !nodeCache.EnteredFromGeneratedFinally
                    (condition, _generatedFinallyHandlers)).ToList();

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
                        FileId,
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
        
        private List<CodeSection> GetGeneratedFinallyHandlers(MethodDefinition method, SequencePoint[] sequencePoints)
        {
            var generatedFinallyHandlers = method.Body.ExceptionHandlers
                .Where(handler => handler.HandlerType == ExceptionHandlerType.Finally &&
                                  !sequencePoints.Any(sp =>
                                      sp.Offset >= handler.HandlerStart.Offset &&
                                      sp.Offset < handler.HandlerEnd.Offset &&
                                      !sp.IsHidden))
                .Select(handler => new CodeSection(
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

        public static MethodBuilder Build(MethodDefinition method, int? fileId)
        {
            return new MethodBuilder(method, fileId);
        }
    }
}