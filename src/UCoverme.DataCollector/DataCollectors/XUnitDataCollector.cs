using System;
using System.Collections.Concurrent;
using System.Threading;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollector.InProcDataCollector;
using UCoverme.DataCollector.Utils;

namespace UCoverme.DataCollector.DataCollectors
{
    public class XUnitDataCollector : IDataCollector
    {
        
        private static readonly ConcurrentBag<TestExecutionData> GlobalTestExecutions = new ConcurrentBag<TestExecutionData>();
        private static readonly AsyncLocal<TestExecutionData> CurrentTestExecution = new AsyncLocal<TestExecutionData>();
        private static readonly object LockObject = new object();

        public string DataCollectorName => "xunit";

        public MethodExecutionData GetDataCollector(string projectPath, int methodId)
        {
            lock (LockObject)
            {
                if (CurrentTestExecution.Value == null)
                {
                    var current = TestExecutionData.Start(DataCollectorName, Guid.NewGuid(), TestExecutionUtils.GetCurrentMethodFromStacktrace());
                    current.SetProjectPath(projectPath);

                    GlobalTestExecutions.Add(current);
                    CurrentTestExecution.Value = current;
                }

                return CurrentTestExecution.Value.MethodEntered(methodId);
            }
        }

        public void TestSessionStart(TestSessionStartArgs testSessionStartArgs)
        {
        }

        public void TestSessionEnd(TestSessionEndArgs testSessionEndArgs)
        {
            lock (LockObject)
            {
                foreach (var testExecution in GlobalTestExecutions)
                {
                    testExecution.WriteSummary();
                }
            }
        }

        public void TestCaseStart(TestCaseStartArgs testCaseStartArgs)
        {
        }

        public void TestCaseEnd(TestCaseEndArgs testCaseEndArgs)
        {
        }
    }
}