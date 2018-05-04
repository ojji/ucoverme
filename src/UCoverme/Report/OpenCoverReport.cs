﻿using System;
using System.Collections.Generic;
using System.Linq;
using UCoverme.DataCollector.Events;
using UCoverme.DataCollector.Summary;
using UCoverme.Model;

namespace UCoverme.Report
{
    public class OpenCoverReport
    {
        public OpenCoverModule[] Modules { get; }

        private readonly UCovermeProject _project;

        public Summary GetSummaryForProject()
        {
            var summaries = _project.Assemblies.Where(module => !module.IsSkipped).Select(GetSummaryForAssembly).ToArray();

            var numSequencePoints = summaries.Sum(summary =>
                summary.NumSequencePoints);
            var visitedSequencePoints = summaries.Sum(summary =>
                summary.VisitedSequencePoints);
            var numBranchPoints = summaries.Sum(summary =>
                summary.NumBranchPoints);
            var visitedBranchPoints = summaries.Sum(summary =>
                summary.VisitedBranchPoints);
            var sequenceCoverage = CalculateCoverage(numSequencePoints, visitedSequencePoints);
            var branchCoverage = CalculateCoverage(numBranchPoints, visitedBranchPoints);
            var numMethods = summaries.Sum(summary => summary.NumMethods);
            var visitedMethods = summaries.Sum(summary => summary.VisitedMethods);
            var numClasses = summaries.Sum(summary => summary.NumClasses);
            var visitedClasses = summaries.Sum(summary => summary.VisitedClasses);

            return new Summary
            {
                NumSequencePoints = numSequencePoints,
                VisitedSequencePoints = visitedSequencePoints,
                NumBranchPoints = numBranchPoints,
                VisitedBranchPoints = visitedBranchPoints,
                SequenceCoverage = sequenceCoverage,
                BranchCoverage = branchCoverage,
                MinCyclomaticComplexity = summaries.Select(s => s.MinCyclomaticComplexity).Min(),
                MaxCyclomaticComplexity = summaries.Select(s => s.MaxCyclomaticComplexity).Max(),
                NumClasses = numClasses,
                VisitedClasses = visitedClasses,
                NumMethods = numMethods,
                VisitedMethods = visitedMethods
            };
        }

        public Summary GetSummaryForAssembly(InstrumentedAssembly module)
        {
            var summaries = module.Classes.Where(c => !c.IsSkipped).Select(GetSummaryForClass).ToArray();

            var numSequencePoints = summaries.Sum(summary =>
                summary.NumSequencePoints);
            var visitedSequencePoints = summaries.Sum(summary =>
                summary.VisitedSequencePoints);
            var numBranchPoints = summaries.Sum(summary =>
                summary.NumBranchPoints);
            var visitedBranchPoints = summaries.Sum(summary =>
                summary.VisitedBranchPoints);
            var sequenceCoverage = CalculateCoverage(numSequencePoints, visitedSequencePoints);
            var branchCoverage = CalculateCoverage(numBranchPoints, visitedBranchPoints);
            var numMethods = summaries.Sum(summary => summary.NumMethods);
            var visitedMethods = summaries.Sum(summary => summary.VisitedMethods);
            var numClasses = summaries.Sum(summary => summary.NumClasses);
            var visitedClasses = summaries.Sum(summary => summary.VisitedClasses);

            return new Summary
            {
                NumSequencePoints = numSequencePoints,
                VisitedSequencePoints = visitedSequencePoints,
                NumBranchPoints = numBranchPoints,
                VisitedBranchPoints = visitedBranchPoints,
                SequenceCoverage = sequenceCoverage,
                BranchCoverage = branchCoverage,
                MinCyclomaticComplexity = summaries.Select(s => s.MinCyclomaticComplexity).Min(),
                MaxCyclomaticComplexity = summaries.Select(s => s.MaxCyclomaticComplexity).Max(),
                NumClasses = numClasses,
                VisitedClasses = visitedClasses,
                NumMethods = numMethods,
                VisitedMethods = visitedMethods
            };
        }

        public Summary GetSummaryForClass(InstrumentedClass instrumentedClass)
        {
            var summaries = instrumentedClass.Methods.Where(method => !method.IsSkipped).Select(GetSummaryForMethod).ToArray();

            var numSequencePoints = summaries.Sum(summary =>
                summary.NumSequencePoints);
            var visitedSequencePoints = summaries.Sum(summary =>
                summary.VisitedSequencePoints);
            var numBranchPoints = summaries.Sum(summary =>
                summary.NumBranchPoints);
            var visitedBranchPoints = summaries.Sum(summary =>
                summary.VisitedBranchPoints);
            var sequenceCoverage = CalculateCoverage(numSequencePoints, visitedSequencePoints);
            var branchCoverage = CalculateCoverage(numBranchPoints, visitedBranchPoints);
            var numMethods = summaries.Sum(summary => summary.NumMethods);
            var visitedMethods = summaries.Sum(summary => summary.VisitedMethods);

            return new Summary
            {
                NumSequencePoints = numSequencePoints,
                VisitedSequencePoints = visitedSequencePoints,
                NumBranchPoints = numBranchPoints,
                VisitedBranchPoints = visitedBranchPoints,
                SequenceCoverage = sequenceCoverage,
                BranchCoverage = branchCoverage,
                MinCyclomaticComplexity = summaries.Select(s => s.MinCyclomaticComplexity).Min(),
                MaxCyclomaticComplexity = summaries.Select(s => s.MaxCyclomaticComplexity).Max(),
                NumClasses = numMethods > 0 ? 1 : 0,
                VisitedClasses = visitedMethods > 0 ? 1 : 0,
                NumMethods = numMethods,
                VisitedMethods = visitedMethods
            };
        }

        private decimal CalculateCoverage(int numberOfPoints, int visitedPoints)
        {
            if (numberOfPoints > 0)
            {
                return Math.Round(visitedPoints / (decimal)numberOfPoints * 100, 2);
            }
            return 0;
        }

        public Summary GetSummaryForMethod(InstrumentedMethod method)
        {
            var numSequencePoints = method.SequencePoints.Count(sp => !sp.IsHidden);
            var numMethods = numSequencePoints > 0 ? 1 : 0;
            var visitedMethods = numMethods > 0 && method.VisitCount > 0 ? 1 : 0;
            var visitedSequencePoints = method.SequencePoints.Count(sp => !sp.IsHidden && sp.VisitCount > 0);
            var numBranchPoints = method.Branches.Length;
            var visitedBranchPoints = method.Branches.Count(branch => branch.VisitCount > 0);
            var branchCoverage = CalculateCoverage(numBranchPoints, visitedBranchPoints);
            var sequenceCoverage = CalculateCoverage(numSequencePoints, visitedSequencePoints);
            
            var cyclomaticComplexity = Math.Max(method.CalculateCyclomaticComplexity(), 1);

            return new Summary
            {
                NumSequencePoints = numSequencePoints,
                VisitedSequencePoints = visitedSequencePoints,
                NumBranchPoints = numBranchPoints,
                VisitedBranchPoints = visitedBranchPoints,
                SequenceCoverage = sequenceCoverage,
                BranchCoverage = branchCoverage,
                MinCyclomaticComplexity = cyclomaticComplexity,
                MaxCyclomaticComplexity = cyclomaticComplexity,
                NumClasses = 0,
                VisitedClasses = 0,
                NumMethods = numMethods,
                VisitedMethods = numMethods == 0 ? 0 : visitedMethods
            };
        }

        public OpenCoverReport(UCovermeProject project, IReadOnlyList<TestExecutionSummary> testExecutions)
        {
            _project = project;
            GenerateSummaries(testExecutions);
            Modules = project.Assemblies.Select(assembly => new OpenCoverModule(this, assembly)).ToArray();
        }

        private void GenerateSummaries(IReadOnlyList<TestExecutionSummary> testExecutions)
        {
            foreach (var executionEvent in testExecutions.SelectMany(testExecution => testExecution.TestCaseEvents))
            {
                if (executionEvent is MethodEnteredEvent methodEntered)
                {
                    var instrumentedMethod = _project.Assemblies.First(assembly => assembly.AssemblyId == methodEntered.AssemblyId).Classes
                                .SelectMany(c => c.Methods).First(m => m.MethodId == methodEntered.MethodId);
                    instrumentedMethod.Visit();
                }
            }

            foreach (var methodExecution in testExecutions.SelectMany(testExecution => testExecution.MethodsExecuted))
            {
                Stack<ExecutionEvent> branchExecution = new Stack<ExecutionEvent>();
                List<Condition> possibleConditions = new List<Condition>();
                foreach (var methodEvent in methodExecution.MethodEvents)
                {
                    switch (methodEvent.ExecutionEventType)
                    {
                        case ExecutionEventType.BranchEntered:
                        {
                            var branchEnteredEvent = (BranchEnteredEvent) methodEvent;

                            var conditionHit = possibleConditions.FirstOrDefault(condition =>
                                condition.TargetBranch == branchEnteredEvent.BranchId);
                            conditionHit?.Visit();
                            possibleConditions.Clear();

                            branchExecution.Push(methodEvent);
                            break;
                        }
                        case ExecutionEventType.BranchExited:
                        {
                            var branchExitedEvent = (BranchExitedEvent) methodEvent;
                            
                            var branchVisited = _project
                                .Assemblies
                                .First(assembly => 
                                    assembly.AssemblyId == branchExitedEvent.AssemblyId)
                                .Classes
                                .SelectMany(c => c.Methods)
                                .First(m => 
                                    m.MethodId == branchExitedEvent.MethodId)
                                .Branches
                                .First(branch => branch.Id == branchExitedEvent.BranchId);

                            branchVisited.Visit();

                            var conditionsToHit = _project.Assemblies
                                .First(assembly => assembly.AssemblyId == branchExitedEvent.AssemblyId)
                                .Classes.SelectMany(@class => @class.Methods)
                                .First(method => method.MethodId == branchExitedEvent.MethodId)
                                .Conditions.Where(condition => condition.StartBranch == branchExitedEvent.BranchId);
                            possibleConditions.AddRange(conditionsToHit);
                            break;
                        }
                        case ExecutionEventType.SequencePointHit:
                        {
                            var sequencePointHitEvent = (SequencePointHitEvent) methodEvent;
                            var sequencePointHit = _project
                                .Assemblies
                                .First(assembly =>
                                    assembly.AssemblyId == sequencePointHitEvent.AssemblyId)
                                .Classes
                                .SelectMany(c => c.Methods)
                                .First(m => m.MethodId == sequencePointHitEvent.MethodId)
                                .SequencePoints
                                .First(sp => sp.Id == sequencePointHitEvent.SequencePointId);

                            sequencePointHit.Visit();
                            break;
                        }
                        default: throw new ArgumentOutOfRangeException(
                            $"Can't handle this execution event type in the method execution summary.");
                    }
                }
            }
        }
    }
}