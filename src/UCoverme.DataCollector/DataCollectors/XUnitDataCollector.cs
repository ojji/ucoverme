using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollector.InProcDataCollector;
using UCoverme.DataCollector.Utils;

namespace UCoverme.DataCollector.DataCollectors
{
    public class XUnitDataCollector : IDataCollector
    {
        private static string _log = Path.Combine(Directory.GetCurrentDirectory(), "xunit-collector.txt");
        
        private static readonly ConcurrentBag<TestExecutionData> GlobalTestExecutions = new ConcurrentBag<TestExecutionData>();
        private static readonly AsyncLocal<TestExecutionData> CurrentTestExecution = new AsyncLocal<TestExecutionData>();
        private static readonly object LockObject = new object();

        public string DataCollectorName => "xunit";

        public XUnitDataCollector()
        {
            lock (LockObject)
            {
                _log.Empty();
            }
        }

        public TestExecutionData GetDataCollector()
        {
            lock (LockObject)
            {
                _log.Log($"GetDataCollector: {TestExecutionUtils.GetCurrentMethodFromStacktrace()}");

                if (CurrentTestExecution.Value == null)
                {
                    var current = TestExecutionData.Start(DataCollectorName, Guid.NewGuid(), TestExecutionUtils.GetCurrentMethodFromStacktrace());

                    GlobalTestExecutions.Add(current);
                    CurrentTestExecution.Value = current;
                }

                return CurrentTestExecution.Value;
            }
        }

        public void TestSessionStart(TestSessionStartArgs testSessionStartArgs)
        {
        }

        public void TestSessionEnd(TestSessionEndArgs testSessionEndArgs)
        {
            lock (LockObject)
            {
                _log.Log("TestSessionEnd: ");
                foreach (var testExecution in GlobalTestExecutions)
                {
                    testExecution.DumpSummary();
                }
            }
        }

        public void TestCaseStart(TestCaseStartArgs testCaseStartArgs)
        {
            lock (LockObject)
            {
                _log.Log($"TestCaseStart: {testCaseStartArgs.TestCase.DisplayName}");
                foreach (var testCaseTrait in testCaseStartArgs.TestCase.Traits)
                {
                    _log.Log($"{testCaseTrait.Name} - {testCaseTrait.Value}");
                }
            }
        }

        public void TestCaseEnd(TestCaseEndArgs testCaseEndArgs)
        {
            lock (LockObject)
            {
                _log.Log($"TestCaseEnd: {testCaseEndArgs.DataCollectionContext.TestCase.DisplayName}");
            }
        }
    }
}