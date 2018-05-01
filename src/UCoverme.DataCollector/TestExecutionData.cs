using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using ProtoBuf;
using UCoverme.DataCollector.Events;
using UCoverme.DataCollector.Summary;

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

        public MethodExecutionData MethodEntered(int assemblyId, int methodId)
        {
            var methodDataCollector = new MethodExecutionData(assemblyId, methodId);
            MethodsExecuted.Enqueue(methodDataCollector);
            TestCaseEvents.Enqueue(ExecutionEvent.MethodEntered(assemblyId, methodId));
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
            WriteSummary();
        }

        private void WriteSummary()
        {
            var summary = new TestExecutionSummary
            {
                FileName = GetTestCaseFilename(),
                TestCaseId = TestCaseId,
                TestCaseName = TestCaseName,
                TestCaseEvents = TestCaseEvents.ToArray(),
                ProjectPath = ProjectPath,
                MethodsExecuted = MethodsExecuted.Select(m => new MethodExecutionSummary
                {
                    AssemblyId = m.AssemblyId,
                    MethodId = m.MethodId,
                    MethodEvents = m.ExecutionEvents.ToArray()
                }).ToArray()
            };

            using (var file = File.Create(summary.FileName))
            {
                Serializer.Serialize(file, summary);
            }
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