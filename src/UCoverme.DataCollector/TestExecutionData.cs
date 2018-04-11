using System;
using System.Collections.Concurrent;
using System.IO;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using UCoverme.DataCollector.Events;

namespace UCoverme.DataCollector
{
    public class TestExecutionData
    {
        public string DataCollectorName { get; }
        public Guid TestCaseId { get; }
        public string TestCaseName { get; }
        public TestOutcome TestResult { get; private set; }

        private readonly ConcurrentQueue<ExecutionEvent> _executionEvents;

        private TestExecutionData(string dataCollectorName, Guid testCaseId, string testCaseName)
        {
            DataCollectorName = dataCollectorName;
            TestCaseId = testCaseId;
            TestCaseName = testCaseName;
            _executionEvents = new ConcurrentQueue<ExecutionEvent>();
            _executionEvents.Enqueue(ExecutionEvent.TestCaseStarted(testCaseId, testCaseName));
        }

        public static TestExecutionData Start(string dataCollectorName, Guid testCaseId, string testCaseName)
        {
            return new TestExecutionData(dataCollectorName, testCaseId, testCaseName);
        }

        public void BranchEntered(int methodId, int branchId)
        {
            _executionEvents.Enqueue(ExecutionEvent.BranchEntered(methodId, branchId));
        }

        public void BranchExited(int methodId, int branchId)
        {
            _executionEvents.Enqueue(ExecutionEvent.BranchExited(methodId, branchId));
        }

        public void SequencePointHit(int methodId, int branchId)
        {
            _executionEvents.Enqueue(ExecutionEvent.SequencePointHit(methodId, branchId));
        }

        public void End(TestOutcome result)
        {
            _executionEvents.Enqueue(ExecutionEvent.TestCaseEnded(result));
            TestResult = result;
        }

        public void DumpSummary()
        {
            using (var writer = new StreamWriter(File.Open(GetTestCaseFilename(), FileMode.Create)))
            {
                foreach (var executionEvent in _executionEvents)
                {
                    writer.WriteLine($"[{TestCaseId} - {TestCaseName}] - {executionEvent}");
                }
            }
        }

        private string GetTestCaseFilename()
        {
            return Path.Combine(Directory.GetCurrentDirectory(), $"{DataCollectorName}-{TestCaseId.ToString()}.ucovermetest");
        }
    }
}