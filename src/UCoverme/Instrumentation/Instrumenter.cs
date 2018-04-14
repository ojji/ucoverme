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
        private readonly AssemblyModel _assemblyModel;
        private readonly Type _dataCollectorType = typeof(UCovermeDataCollector);

        // these are the references to the data collector assembly, be careful!
        // if the method signatures are changing, we have change these too!
        private TypeReference _testExecutionDataTypeReference;
        private MethodReference _getDataCollectorMethodReference;
        private MethodReference _branchEnteredMethodReference;
        private MethodReference _branchExitedMethodReference;
        private MethodReference _sequencePointHitMethodReference;

        public Instrumenter(AssemblyModel assemblyModel)
        {
            _assemblyModel = assemblyModel;
            CreateTempCopies(_assemblyModel.AssemblyPaths);
            CopyDataCollectorAssembly(_assemblyModel.AssemblyPaths);
        }

        public void Instrument(IProgress<string> progress)
        {
            using (var assemblyDefinition = AssemblyDefinition.ReadAssembly(_assemblyModel.AssemblyPaths.TempAssemblyPath,
                new ReaderParameters
                {
                    ReadSymbols = true
                }))
            {
                progress.Report($"Instrumenting assembly {_assemblyModel.AssemblyName}...");

                ImportAndSetInstrumentationMethodReferences(assemblyDefinition);

                foreach (var method in _assemblyModel.Classes.SelectMany(c => c.Methods))
                {
                    progress.Report($"\t{method.Name}");
                    InstrumentMethod(assemblyDefinition, method);
                }

                assemblyDefinition.Write(_assemblyModel.AssemblyPaths.OriginalAssemblyPath, new WriterParameters
                 {
                     WriteSymbols = true
                 });

                progress.Report($"Done, assembly written to {_assemblyModel.AssemblyPaths.OriginalAssemblyPath}.");
            }
        }

        private void ImportAndSetInstrumentationMethodReferences(AssemblyDefinition assemblyDefinition)
        {
            Type testExecutionDataType = typeof(TestExecutionData);

            assemblyDefinition.MainModule.ImportReference(_dataCollectorType);
            _testExecutionDataTypeReference = assemblyDefinition.MainModule.ImportReference(testExecutionDataType);

            var getDataCollectorMethodInfo = _dataCollectorType.GetMethod("GetDataCollector");
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

        private void CopyDataCollectorAssembly(AssemblyPaths assemblyPaths)
        {
            var dataCollectorAssemblyPath = _dataCollectorType.Assembly.Location;
            var outputDirectory = Path.GetDirectoryName(assemblyPaths.OriginalAssemblyPath);
            File.Copy(dataCollectorAssemblyPath,
                Path.Combine(outputDirectory,
                    Path.GetFileName(dataCollectorAssemblyPath)),
                true);
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

        private void InstrumentMethod(AssemblyDefinition assemblyDefinition, InstrumentedMethod method)
        {
            var ilProcessor = GetILProcessorForMethod(assemblyDefinition, method);
            ilProcessor.Body.InitLocals = true;
            ilProcessor.Body.SimplifyMacros();

            var testExecutionDataVariable = new VariableDefinition(_testExecutionDataTypeReference);
            ilProcessor.Body.Variables.Add(testExecutionDataVariable);

            ChangeTailCallsToNops(ilProcessor);
            
            var originalInstructions = ilProcessor.Body.Instructions.ToArray();
            List<ArraySegment<Instruction>> branchInstructions = GetBranchInstructions(originalInstructions, method);
            InsertDataCollector(ilProcessor, testExecutionDataVariable, originalInstructions);
            InsertBranchStartInstrumentation(ilProcessor, testExecutionDataVariable, branchInstructions, method);
            InsertBranchEndInstrumentation(ilProcessor, testExecutionDataVariable, branchInstructions, method);
            
            ilProcessor.Body.OptimizeMacros();
        }

        private void ChangeTailCallsToNops(ILProcessor ilProcessor)
        {
            foreach (var tailInstruction in ilProcessor.Body.Instructions.Where(i => i.OpCode == OpCodes.Tail))
            {
                ilProcessor.Replace(tailInstruction, ilProcessor.Create(OpCodes.Nop));
            }
        }

        private void InsertDataCollector(ILProcessor ilProcessor, VariableDefinition testExecutionDataVariable,
            Instruction[] originalInstructions)
        {
            var getDataCollectorInstruction = ilProcessor.Create(OpCodes.Call, _getDataCollectorMethodReference);
            var storeDataCollectorInstruction = ilProcessor.Create(OpCodes.Stloc, testExecutionDataVariable);

            var originalInstruction = originalInstructions[0];
            ilProcessor.InsertAllBefore(originalInstruction,
                getDataCollectorInstruction, 
                storeDataCollectorInstruction);

            UpdateInstructionReference(ilProcessor.Body, originalInstruction, getDataCollectorInstruction);
        }

        private void InsertBranchEndInstrumentation(ILProcessor ilProcessor, 
            VariableDefinition testExecutionDataVariable, 
            List<ArraySegment<Instruction>> branches, 
            InstrumentedMethod method)
        {
            for (int i = 0; i < branches.Count; i++)
            {
                // We have to find the last instruction but be aware! 
                // When the previous instruction is a prefix, we cannot insert the instrumentation code inbetween the two.
                var startInstructionOffset = branches[i].Offset;
                var endInstructionOffset = branches[i].Offset + branches[i].Count - 1;
                Instruction endInstruction = branches[i].Array[endInstructionOffset];
                Instruction previousInstruction = endInstructionOffset > startInstructionOffset ? 
                    branches[i].Array[endInstructionOffset - 1] : 
                    null;
                while (previousInstruction != null && previousInstruction.OpCode.OpCodeType == OpCodeType.Prefix)
                {
                    endInstructionOffset--;
                    endInstruction = previousInstruction;
                    previousInstruction = endInstructionOffset > startInstructionOffset ?
                    branches[i].Array[endInstructionOffset - 1] :
                    null;
                }

                var loadCollectorInstruction = ilProcessor.Create(OpCodes.Ldloc, testExecutionDataVariable.Index);
                var methodIdParameterInstruction = ilProcessor.Create(OpCodes.Ldc_I4, method.MethodId);
                var branchIdParameterInstruction = ilProcessor.Create(OpCodes.Ldc_I4, i);
                var exitBranchInstruction = ilProcessor.Create(OpCodes.Callvirt, _branchExitedMethodReference);

                // todo: if the last instruction is a prefix, where should we place the instrumentation code? - for now just place it before
                if (endInstruction.OpCode.FlowControl == FlowControl.Branch ||
                    endInstruction.OpCode.FlowControl == FlowControl.Cond_Branch ||
                    endInstruction.OpCode.FlowControl == FlowControl.Return ||
                    endInstruction.OpCode.FlowControl == FlowControl.Throw ||
                    endInstruction.OpCode.OpCodeType == OpCodeType.Prefix)
                {
                    ilProcessor.InsertAllBefore(endInstruction,
                    loadCollectorInstruction,
                    methodIdParameterInstruction,
                    branchIdParameterInstruction,
                    exitBranchInstruction);
                }
                else
                {
                    ilProcessor.InsertAllAfter(endInstruction,
                    loadCollectorInstruction,
                    methodIdParameterInstruction,
                    branchIdParameterInstruction,
                    exitBranchInstruction);
                }

                UpdateInstructionReference(ilProcessor.Body, endInstruction, loadCollectorInstruction);
            }
        }

        private void InsertBranchStartInstrumentation(ILProcessor ilProcessor,
            VariableDefinition testExecutionDataVariable,
            List<ArraySegment<Instruction>> branches, 
            InstrumentedMethod method)
        {
            for (int i = 0; i < branches.Count; i++)
            {
                var originalInstruction = branches[i].Array[branches[i].Offset];

                var loadCollectorInstruction = ilProcessor.Create(OpCodes.Ldloc, testExecutionDataVariable.Index);
                var methodIdParameterInstruction = ilProcessor.Create(OpCodes.Ldc_I4, method.MethodId);
                var branchIdParameterInstruction = ilProcessor.Create(OpCodes.Ldc_I4, i);
                var enterBranchInstruction = ilProcessor.Create(OpCodes.Callvirt, _branchEnteredMethodReference);

                ilProcessor.InsertAllBefore(originalInstruction,
                    loadCollectorInstruction,
                    methodIdParameterInstruction,
                    branchIdParameterInstruction,
                    enterBranchInstruction);

                UpdateInstructionReference(ilProcessor.Body, originalInstruction, loadCollectorInstruction);
            }
        }

        private List<ArraySegment<Instruction>> GetBranchInstructions(Instruction[] instructions,
            InstrumentedMethod method)
        {
            var branchInstructions = new List<ArraySegment<Instruction>>(method.Branches.Length);
            int currentBranch = 0;
            int currentStartOffset = 0;
            int currentIdx = 0;
            int currentInstructionCount = 1;
            while (currentIdx < instructions.Length)
            {
                if (instructions[currentIdx].Offset == method.Branches[currentBranch].EndOffset)
                {
                    branchInstructions.Add(new ArraySegment<Instruction>(instructions, currentStartOffset,
                        currentInstructionCount));
                    currentBranch++;
                    currentInstructionCount = 1;
                    currentIdx++;
                    currentStartOffset = currentIdx;
                    continue;
                }

                currentInstructionCount++;
                currentIdx++;
            }

            return branchInstructions;
        }

        private List<Instruction> GetBranchStartInstructions(InstrumentedMethod method, ILProcessor ilProcessor)
        {
            return ilProcessor.Body.Instructions.Where(i => method.Branches.Any(m => m.StartOffset == i.Offset))
                .ToList();
        }

        private ILProcessor GetILProcessorForMethod(AssemblyDefinition assemblyDefinition, InstrumentedMethod method)
        {
            var methodDefinition = assemblyDefinition.MainModule
                .GetTypes()
                .SelectMany(t => t.GetInstrumentableMethods())
                .FirstOrDefault(m =>
                    m.MetadataToken.ToInt32() == method.MethodId);

            if (methodDefinition == null)
            {
                throw new ArgumentException($"Could not find the method with id {method.MethodId}");
            }

            return methodDefinition.Body.GetILProcessor();
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