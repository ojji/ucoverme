using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollector.InProcDataCollector;
using UCoverme.DataCollector.Utils;

namespace UCoverme.DataCollector.DataCollectors
{
    public class XUnitDataCollector : IDataCollector
    {
        private static readonly List<TestExecutionData> RunningTestExecutions = new List<TestExecutionData>();
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

                    RunningTestExecutions.Add(current);
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
                foreach (var testExecution in RunningTestExecutions)
                {
                    testExecution.End(TestOutcome.None);
                }
            }
        }

        public void TestCaseStart(TestCaseStartArgs testCaseStartArgs)
        {
        }

        public void TestCaseEnd(TestCaseEndArgs testCaseEndArgs)
        {
            lock (LockObject)
            {
                var testCaseName = testCaseEndArgs.DataCollectionContext.TestCase.FullyQualifiedName;
                var testCasesWithName = RunningTestExecutions.Where(test => test.TestCaseName == testCaseName).ToArray();

                // if there are multiple cases running with the same method name, we cannot
                // tell which one ended in xunit because we lack a testcontext
                // so instead we just return and accept that we cannot set the
                // test outcome in this case
                // as a workaround, we are hooking onto the test session end event
                // and write the remaining test summaries there
                if (testCasesWithName.Length != 1) return;
                RunningTestExecutions.Remove(testCasesWithName[0]);
                testCasesWithName[0].End(testCaseEndArgs.TestOutcome);
            }
        }
    }
}