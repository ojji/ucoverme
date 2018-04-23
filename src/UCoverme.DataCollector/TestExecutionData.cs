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

        public ConcurrentQueue<MethodExecutionData> MethodsExecuted { get; }
        public ConcurrentQueue<ExecutionEvent> TestCaseEvents { get; }

        private TestExecutionData(string dataCollectorName, Guid testCaseId, string testCaseName)
        {
            DataCollectorName = dataCollectorName;
            TestCaseId = testCaseId;
            TestCaseName = testCaseName;
            MethodsExecuted = new ConcurrentQueue<MethodExecutionData>();
            TestCaseEvents = new ConcurrentQueue<ExecutionEvent>();
            TestCaseEvents.Enqueue(ExecutionEvent.TestCaseStarted(testCaseId, testCaseName));
        }

        public MethodExecutionData MethodEntered(int methodId)
        {
            var methodDataCollector = new MethodExecutionData(methodId);
            MethodsExecuted.Enqueue(methodDataCollector);
            TestCaseEvents.Enqueue(ExecutionEvent.MethodEntered(methodId));
            return methodDataCollector;
        }

        public static TestExecutionData Start(string dataCollectorName, Guid testCaseId, string testCaseName)
        {
            return new TestExecutionData(dataCollectorName, testCaseId, testCaseName);
        }

        public void End(TestOutcome result)
        {
            TestCaseEvents.Enqueue(ExecutionEvent.TestCaseEnded(result));
            TestResult = result;
            TestExecutionSummary.WriteToFile(this);
        }

        public string GetTestCaseFilename()
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