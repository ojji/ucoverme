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
        public string ProjectPath { get; private set; }

        private readonly ConcurrentQueue<MethodExecutionData> _methodsExecuted;
        private readonly ConcurrentQueue<ExecutionEvent> _testCaseEvents;

        private TestExecutionData(string dataCollectorName, Guid testCaseId, string testCaseName)
        {
            DataCollectorName = dataCollectorName;
            TestCaseId = testCaseId;
            TestCaseName = testCaseName;
            _methodsExecuted = new ConcurrentQueue<MethodExecutionData>();
            _testCaseEvents = new ConcurrentQueue<ExecutionEvent>();
            _testCaseEvents.Enqueue(ExecutionEvent.TestCaseStarted(testCaseId, testCaseName));
        }

        public MethodExecutionData MethodEntered(int methodId)
        {
            var methodDataCollector = new MethodExecutionData(methodId);
            _methodsExecuted.Enqueue(methodDataCollector);
            _testCaseEvents.Enqueue(ExecutionEvent.MethodEntered(methodId));
            return methodDataCollector;
        }

        public static TestExecutionData Start(string dataCollectorName, Guid testCaseId, string testCaseName)
        {
            return new TestExecutionData(dataCollectorName, testCaseId, testCaseName);
        }

        public void End(TestOutcome result)
        {
            _testCaseEvents.Enqueue(ExecutionEvent.TestCaseEnded(result));
            TestResult = result;
            WriteSummary();
        }

        public void WriteSummary()
        {
            using (var writer = new StreamWriter(File.Open(GetTestCaseFilename(), FileMode.Create)))
            {
                foreach (var executionEvent in _testCaseEvents)
                {
                    writer.WriteLine($"[{TestCaseId} - {TestCaseName}] - {executionEvent}");
                }

                writer.WriteLine("\n--- Method executions ---");
                foreach (var method in _methodsExecuted)
                {
                    foreach (var executionEvent in method.ExecutionEvents)
                    {
                                writer.WriteLine($"[{method.MethodId}] - {executionEvent}");
                    }
                    writer.Write("\n\n");
                }
            }
        }

        private string GetTestCaseFilename()
        {
            var coverageDirectory = Path.GetDirectoryName(ProjectPath);
            return Path.Combine(coverageDirectory, $"{DataCollectorName}-{TestCaseId.ToString()}.ucovermetest");
        }

        public void SetProjectPath(string projectPath)
        {
            if (string.IsNullOrEmpty(ProjectPath))
            {
                ProjectPath = projectPath;
            }
        }
    }
}