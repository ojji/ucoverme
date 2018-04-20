using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Mono.Collections.Generic;
using UCoverme.DataCollector;
using UCoverme.Model;
using UCoverme.Utils;

namespace UCoverme.Instrumentation
{
    public class Instrumenter
    {
        private enum InstrumentationType
        {
            BranchEnter,
            SequencePointHit,
            BranchExit
        }

        private readonly InstrumentedAssembly _instrumentedAssembly;

        // WARNING: these are the references to the data collector assembly
        // if the method signatures are changing in the data collector, we have change these too!
        private TypeReference _testExecutionDataTypeReference;
        private MethodReference _getDataCollectorMethodReference;
        private MethodReference _branchEnteredMethodReference;
        private MethodReference _branchExitedMethodReference;
        private MethodReference _sequencePointHitMethodReference;

        private readonly Dictionary<int, Dictionary<InstrumentationType, Instruction[]>> _beforeInstructions = new Dictionary<int, Dictionary<InstrumentationType, Instruction[]>>();
        private readonly Dictionary<int, Dictionary<InstrumentationType, Instruction[]>> _afterInstructions = new Dictionary<int, Dictionary<InstrumentationType, Instruction[]>>();

        public Instrumenter(InstrumentedAssembly instrumentedAssembly)
        {
            _instrumentedAssembly = instrumentedAssembly;
            CreateTempCopies(_instrumentedAssembly.AssemblyPaths);
        }

        public void Instrument(string projectPath)
        {
            using (var assemblyDefinition = AssemblyDefinition.ReadAssembly(
                _instrumentedAssembly.AssemblyPaths.TempAssemblyPath,
                new ReaderParameters
                {
                    ReadSymbols = true
                }))
            {
                Console.WriteLine($"Instrumenting assembly {_instrumentedAssembly.FullyQualifiedAssemblyName}...");

                ImportAndSetInstrumentationMethodReferences(assemblyDefinition);

                foreach (var instrumentedClass in _instrumentedAssembly.Classes)
                {
                    if (instrumentedClass.IsSkipped)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.Write($"[DISABLED - {instrumentedClass.SkipReason.ToString()}] ");
                        Console.ResetColor();
                        Console.WriteLine($"{instrumentedClass.Name}");
                    }
                    else
                    {
                        Console.WriteLine($"{instrumentedClass.Name}");
                        foreach (var method in instrumentedClass.Methods)
                        {
                            Console.WriteLine($"\t{method.Name}");
                            InstrumentMethod(assemblyDefinition, method, projectPath);
                        }
                    }
                }

                assemblyDefinition.Write(_instrumentedAssembly.AssemblyPaths.OriginalAssemblyPath, new WriterParameters
                {
                    WriteSymbols = true
                });

                Console.WriteLine($"Done, assembly written to {_instrumentedAssembly.AssemblyPaths.OriginalAssemblyPath}.");
            }
        }

        private void ImportAndSetInstrumentationMethodReferences(AssemblyDefinition assemblyDefinition)
        {
            Type dataCollectorType = typeof(UCovermeDataCollector);
            Type testExecutionDataType = typeof(TestExecutionData);

            assemblyDefinition.MainModule.ImportReference(dataCollectorType);
            _testExecutionDataTypeReference = assemblyDefinition.MainModule.ImportReference(testExecutionDataType);

            var getDataCollectorMethodInfo = dataCollectorType.GetMethod("GetDataCollector", new [] { typeof(string) });
            _getDataCollectorMethodReference =
                assemblyDefinition.MainModule.ImportReference(getDataCollectorMethodInfo);

            // BranchEntered
            var branchEnteredMethodInfo =
                testExecutionDataType.GetMethod("BranchEntered", new[] {typeof(int), typeof(int)});
            _branchEnteredMethodReference = assemblyDefinition.MainModule.ImportReference(branchEnteredMethodInfo);

            // BranchExited
            var branchExitedMethodInfo =
                testExecutionDataType.GetMethod("BranchExited", new[] {typeof(int), typeof(int)});
            _branchExitedMethodReference = assemblyDefinition.MainModule.ImportReference(branchExitedMethodInfo);

            // SequencePointHit
            var sequencePointHitMethodInfo =
                testExecutionDataType.GetMethod("SequencePointHit", new[] {typeof(int), typeof(int)});
            _sequencePointHitMethodReference =
                assemblyDefinition.MainModule.ImportReference(sequencePointHitMethodInfo);
        }

        private void CreateTempCopies(AssemblyPaths assemblyPaths)
        {
            if (!File.Exists(assemblyPaths.OriginalAssemblyPath))
            {
                throw new ArgumentException($"Cant find the assembly file: {assemblyPaths.OriginalAssemblyPath}.");
            }

            if (!File.Exists(assemblyPaths.OriginalPdbPath))
            {
                throw new ArgumentException($"Cant find the pdb file: {assemblyPaths.OriginalPdbPath}.");
            }

            File.Copy(assemblyPaths.OriginalAssemblyPath, assemblyPaths.TempAssemblyPath, true);
            File.Copy(assemblyPaths.OriginalPdbPath, assemblyPaths.TempPdbPath, true);
        }

        private void InstrumentMethod(AssemblyDefinition assemblyDefinition, InstrumentedMethod method, string projectPath)
        {
            var ilProcessor = GetILProcessorForMethod(assemblyDefinition, method.MethodId);
            ilProcessor.Body.InitLocals = true;
            ilProcessor.Body.SimplifyMacros();

            ChangeTailCallsToNops(ilProcessor);

            var originalInstructions = ilProcessor.Body.Instructions.ToArray();
            var branchSegments = GetInstructionSegments(originalInstructions, method.Branches);
            var sequencePointSegments = GetInstructionSegments(originalInstructions, method.SequencePoints);

            var testExecutionDataVariable = InsertDataCollector(ilProcessor, originalInstructions, projectPath);
            CreateBranchEnters(ilProcessor, method.MethodId, testExecutionDataVariable, branchSegments);
            CreateBranchExits(ilProcessor, method.MethodId, testExecutionDataVariable, branchSegments);
            CreateSequencePointHits(ilProcessor, method.MethodId, testExecutionDataVariable, sequencePointSegments);

            InsertInstrumentationInstructions(ilProcessor, originalInstructions);
            ClearBeforeAndAfterInstructions();

            ilProcessor.Body.OptimizeMacros();
        }

        private ILProcessor GetILProcessorForMethod(AssemblyDefinition assemblyDefinition, int methodId)
        {
            var methodDefinition = assemblyDefinition.MainModule
                .GetTypes()
                .SelectMany(t => t.GetInstrumentableMethods())
                .FirstOrDefault(m =>
                    m.MetadataToken.ToInt32() == methodId);

            if (methodDefinition == null)
            {
                throw new ArgumentException($"Could not find the method with id {methodId}");
            }

            return methodDefinition.Body.GetILProcessor();
        }

        private void ChangeTailCallsToNops(ILProcessor ilProcessor)
        {
            foreach (var tailInstruction in ilProcessor.Body.Instructions.Where(i => i.OpCode == OpCodes.Tail))
            {
                ilProcessor.Replace(tailInstruction, ilProcessor.Create(OpCodes.Nop));
            }
        }

        private List<ArraySegment<Instruction>> GetInstructionSegments(Instruction[] instructions, IReadOnlyList<ICodeSegment> segments)
        {
            var segmentedInstructions = new List<ArraySegment<Instruction>>(segments.Count);

            if (segments.Count == 0)
            {
                return segmentedInstructions;
            }

            int currentSegment = 0;
            int currentStartOffset = 0;
            int idx = 0;
            int currentInstructionCount = 1;
            while (idx < instructions.Length)
            {
                if (instructions[idx].Offset == segments[currentSegment].EndOffset)
                {
                    segmentedInstructions.Add(new ArraySegment<Instruction>(instructions, currentStartOffset,
                        currentInstructionCount));
                    currentSegment++;
                    currentInstructionCount = 1;
                    idx++;
                    currentStartOffset = idx;
                    continue;
                }

                currentInstructionCount++;
                idx++;
            }
            return segmentedInstructions;
        }

        private VariableReference InsertDataCollector(ILProcessor ilProcessor,
            Instruction[] originalInstructions, string projectPath)
        {
            var testExecutionDataVariable = new VariableDefinition(_testExecutionDataTypeReference);
            ilProcessor.Body.Variables.Add(testExecutionDataVariable);

            var setProjectPathInstruction = ilProcessor.Create(OpCodes.Ldstr, projectPath);
            var getDataCollectorInstruction = ilProcessor.Create(OpCodes.Call, _getDataCollectorMethodReference);
            var storeDataCollectorInstruction = ilProcessor.Create(OpCodes.Stloc, testExecutionDataVariable);

            var originalInstruction = originalInstructions[0];
            ilProcessor.InsertAllBefore(originalInstruction,
                setProjectPathInstruction,
                getDataCollectorInstruction,
                storeDataCollectorInstruction);

            UpdateInstructionReference(ilProcessor.Body, 
                originalInstruction,
                setProjectPathInstruction);

            return testExecutionDataVariable;
        }

        private void CreateBranchEnters(ILProcessor ilProcessor,
            int methodId,
            VariableReference testExecutionDataVariable,
            IReadOnlyList<ArraySegment<Instruction>> branches)
        {
            for (int i = 0; i < branches.Count; i++)
            {
                var loadCollectorInstruction = ilProcessor.Create(OpCodes.Ldloc, testExecutionDataVariable.Index);
                var methodIdParameterInstruction = ilProcessor.Create(OpCodes.Ldc_I4, methodId);
                var branchIdParameterInstruction = ilProcessor.Create(OpCodes.Ldc_I4, i);
                var enterBranchInstruction = ilProcessor.Create(OpCodes.Callvirt, _branchEnteredMethodReference);

                CreateEnterInstruction(branches[i], 
                    InstrumentationType.BranchEnter,
                    loadCollectorInstruction,
                    methodIdParameterInstruction,
                    branchIdParameterInstruction,
                    enterBranchInstruction);
            }
        }

        private void CreateBranchExits(ILProcessor ilProcessor,
            int methodId,
            VariableReference testExecutionDataVariable,
            IReadOnlyList<ArraySegment<Instruction>> branches)
        {
            for (int i = 0; i < branches.Count; i++)
            {
                var loadCollectorInstruction = ilProcessor.Create(OpCodes.Ldloc, testExecutionDataVariable.Index);
                var methodIdParameterInstruction = ilProcessor.Create(OpCodes.Ldc_I4, methodId);
                var branchIdParameterInstruction = ilProcessor.Create(OpCodes.Ldc_I4, i);
                var exitBranchInstruction = ilProcessor.Create(OpCodes.Callvirt, _branchExitedMethodReference);

                CreateExitInstruction(branches[i],
                    InstrumentationType.BranchExit,
                    loadCollectorInstruction,
                    methodIdParameterInstruction,
                    branchIdParameterInstruction,
                    exitBranchInstruction);
            }
        }

        private void CreateSequencePointHits(ILProcessor ilProcessor,
            int methodId,
            VariableReference testExecutionDataVariable,
            IReadOnlyList<ArraySegment<Instruction>> sequencePoints)
        {
            for (int i = 0; i < sequencePoints.Count; i++)
            {
                var loadCollectorInstruction = ilProcessor.Create(OpCodes.Ldloc, testExecutionDataVariable.Index);
                var methodIdParameterInstruction = ilProcessor.Create(OpCodes.Ldc_I4, methodId);
                var sequencePointIdParameterInstruction = ilProcessor.Create(OpCodes.Ldc_I4, i);
                var sequencePointHidInstruction = ilProcessor.Create(OpCodes.Callvirt, _sequencePointHitMethodReference);

                CreateExitInstruction(sequencePoints[i],
                    InstrumentationType.SequencePointHit,
                    loadCollectorInstruction,
                    methodIdParameterInstruction,
                    sequencePointIdParameterInstruction,
                    sequencePointHidInstruction);
            }
        }

        private void CreateEnterInstruction(ArraySegment<Instruction> segment, InstrumentationType type, params Instruction[] instrumentationInstructions)
        {
            var beginningInstruction = segment.Array[segment.Offset];
            PlaceInstrumentationBefore(beginningInstruction, type, instrumentationInstructions);
        }

        private void CreateExitInstruction(ArraySegment<Instruction> segment, InstrumentationType type, params Instruction[] instrumentationInstructions)
        {
            // if the last instruction is not branching or throwing, the instrumentation code can come after
            var lastInstruction = segment.Array[segment.Offset + segment.Count - 1];
            if ((lastInstruction.OpCode.FlowControl == FlowControl.Next && lastInstruction.OpCode.OpCodeType != OpCodeType.Prefix) ||
                lastInstruction.OpCode.FlowControl == FlowControl.Break ||
                lastInstruction.OpCode.FlowControl == FlowControl.Call)
            {
                PlaceInstrumentationAfter(lastInstruction, type, instrumentationInstructions);
            }
            else
            {
                // we have to find the last instruction that doesn't have a prefix intruction to place the instrumentation code before it
                lastInstruction = GetLastInstrumentableInstructionFromSegment(segment);
                PlaceInstrumentationBefore(lastInstruction, type, instrumentationInstructions);
            }
        }

        private Instruction GetLastInstrumentableInstructionFromSegment(ArraySegment<Instruction> segment)
        {
            // We have to find the last instruction but be aware! 
            // When the previous instruction is a prefix, we cannot insert the instrumentation code inbetween the two.
            var startInstructionOffset = segment.Offset;
            var endInstructionOffset = segment.Offset + segment.Count - 1;
            Instruction endInstruction = segment.Array[endInstructionOffset];
            Instruction previousInstruction = endInstructionOffset > startInstructionOffset
                ? segment.Array[endInstructionOffset - 1]
                : null;
            while (previousInstruction != null && previousInstruction.OpCode.OpCodeType == OpCodeType.Prefix)
            {
                endInstructionOffset--;
                endInstruction = previousInstruction;
                previousInstruction = endInstructionOffset > startInstructionOffset
                    ? segment.Array[endInstructionOffset - 1]
                    : null;
            }

            return endInstruction;
        }

        private void InsertInstrumentationInstructions(ILProcessor ilProcessor, Instruction[] originalInstructions)
        {
            foreach (var instrumentationInstructions in _afterInstructions)
            {
                // after instructions should be in the format of
                // 1. instruction
                // 2. sequencepointhit
                // 3. branchexit
                var originalInstruction = originalInstructions.First(i => i.Offset == instrumentationInstructions.Key);
                var mergedInstructions = new List<Instruction>();
                if (instrumentationInstructions.Value.ContainsKey(InstrumentationType.SequencePointHit))
                {
                    mergedInstructions.AddRange(instrumentationInstructions.Value[InstrumentationType.SequencePointHit]);
                }

                if (instrumentationInstructions.Value.ContainsKey(InstrumentationType.BranchExit))
                {
                    mergedInstructions.AddRange(instrumentationInstructions.Value[InstrumentationType.BranchExit]);
                }
                ilProcessor.InsertAllAfter(originalInstruction, mergedInstructions.ToArray());
            }

            foreach (var instrumentationInstructions in _beforeInstructions)
            {
                // before instructions should be in the format of
                // 1. branchenter
                // 2. sequencepointhit
                // 3. branchexit
                // 4. instruction

                var originalInstruction = originalInstructions.First(i => i.Offset == instrumentationInstructions.Key);
                var mergedInstructions = new List<Instruction>();
                if (instrumentationInstructions.Value.ContainsKey(InstrumentationType.BranchEnter))
                {
                    mergedInstructions.AddRange(instrumentationInstructions.Value[InstrumentationType.BranchEnter]);
                }
                if (instrumentationInstructions.Value.ContainsKey(InstrumentationType.SequencePointHit))
                {
                    mergedInstructions.AddRange(instrumentationInstructions.Value[InstrumentationType.SequencePointHit]);
                }
                if (instrumentationInstructions.Value.ContainsKey(InstrumentationType.BranchExit))
                {
                    mergedInstructions.AddRange(instrumentationInstructions.Value[InstrumentationType.BranchExit]);
                }
                ilProcessor.InsertAllBefore(originalInstruction, mergedInstructions.ToArray());
                // and we have to update the operand references to the first instruction inserted
                UpdateInstructionReference(ilProcessor.Body, originalInstruction, mergedInstructions[0]);
            }
        }

        private void ClearBeforeAndAfterInstructions()
        {
            _beforeInstructions.Clear();
            _afterInstructions.Clear();
        }

        private void PlaceInstrumentationBefore(Instruction target, InstrumentationType type, params Instruction[] instructions)
        {
            if (!_beforeInstructions.ContainsKey(target.Offset))
            {
                _beforeInstructions[target.Offset] = new Dictionary<InstrumentationType, Instruction[]>();
            }
            _beforeInstructions[target.Offset].Add(type, instructions);
        }

        private void PlaceInstrumentationAfter(Instruction target, InstrumentationType type, params Instruction[] instructions)
        {
            if (!_afterInstructions.ContainsKey(target.Offset))
            {
                _afterInstructions[target.Offset] = new Dictionary<InstrumentationType, Instruction[]>();
            }
            _afterInstructions[target.Offset].Add(type, instructions);
        }

        private void UpdateInstructionReference(MethodBody methodBody, Instruction originalInstruction,
            Instruction newInstruction)
        {
            UpdateExceptionHandlers(methodBody.ExceptionHandlers, originalInstruction, newInstruction);
            UpdateOperandsInBody(methodBody.Instructions, originalInstruction, newInstruction);
        }

        private void UpdateOperandsInBody(Collection<Instruction> instructions, Instruction originalInstruction,
            Instruction newInstruction)
        {
            foreach (var instruction in instructions)
            {
                if (instruction.Operand == originalInstruction)
                {
                    instruction.Operand = newInstruction;
                    continue;
                }

                if (!(instruction.Operand is Instruction[]))
                {
                    continue;
                }

                var operands = (Instruction[]) instruction.Operand;
                for (int i = 0; i < operands.Length; i++)
                {
                    if (operands[i] == originalInstruction)
                    {
                        operands[i] = newInstruction;
                    }
                }
            }
        }

        private void UpdateExceptionHandlers(IEnumerable<ExceptionHandler> exceptionHandlers,
            Instruction originalInstruction, Instruction newInstruction)
        {
            foreach (var exceptionHandler in exceptionHandlers)
            {
                if (exceptionHandler.FilterStart == originalInstruction)
                {
                    exceptionHandler.FilterStart = newInstruction;
                }

                if (exceptionHandler.HandlerStart == originalInstruction)
                {
                    exceptionHandler.HandlerStart = newInstruction;
                }

                if (exceptionHandler.HandlerEnd == originalInstruction)
                {
                    exceptionHandler.HandlerEnd = newInstruction;
                }

                if (exceptionHandler.TryStart == originalInstruction)
                {
                    exceptionHandler.TryStart = newInstruction;
                }

                if (exceptionHandler.TryEnd == originalInstruction)
                {
                    exceptionHandler.TryEnd = newInstruction;
                }
            }
        }
    }
}