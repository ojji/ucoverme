using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollector.InProcDataCollector;
using UCoverme.DataCollector.Utils;

namespace UCoverme.DataCollector.DataCollectors
{
    public class NUnitDataCollector : IDataCollector
    {
        private static string _log = Path.Combine(Directory.GetCurrentDirectory(), "nunit-collector.txt");
        private static Type _testContextType;
        private static Type _testAdapterType;
        private static PropertyInfo _currentContextPropertyInfo;
        private static PropertyInfo _currentTestPropertyInfo;
        private static PropertyInfo _currentFullNamePropertyInfo;

        private static readonly object LockObject = new object();
        private static readonly ConcurrentDictionary<string, TestExecutionData> TestExecutions = new ConcurrentDictionary<string, TestExecutionData>();

        public string DataCollectorName => "nunit";

        public NUnitDataCollector()
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
                if (!TestExecutions.TryGetValue(GetTestExecutionId(), out var currentContext))
                {
                    throw new InvalidOperationException("Could not find the test execution context.");
                }

                return currentContext;
            }
        }

        public void TestSessionStart(TestSessionStartArgs testSessionStartArgs)
        {
        }

        public void TestSessionEnd(TestSessionEndArgs testSessionEndArgs)
        {
        }

        public void TestCaseStart(TestCaseStartArgs testCaseStartArgs)
        {
            lock (LockObject)
            {
                if (!TestExecutions.TryAdd(testCaseStartArgs.TestCase.FullyQualifiedName,
                    TestExecutionData.Start(DataCollectorName, testCaseStartArgs.TestCase.Id, testCaseStartArgs.TestCase.FullyQualifiedName)))
                {
                    throw new InvalidOperationException("Could not create the test execution context.");
                }
            }
        }

        public void TestCaseEnd(TestCaseEndArgs testCaseEndArgs)
        {
            lock (LockObject)
            {
                if (!TestExecutions.TryRemove(testCaseEndArgs.DataCollectionContext.TestCase.FullyQualifiedName, out var currentTestExecution))
                {
                    throw new InvalidOperationException("Could not delete the test execution context.");
                }
                currentTestExecution.End(testCaseEndArgs.TestOutcome);
                currentTestExecution.DumpSummary();
            }
        }

        private string GetTestExecutionId()
        {
            if (_currentFullNamePropertyInfo == null)
            {
                SetPropertyGetters();
            }

            var currentContext = _currentContextPropertyInfo.GetValue(null);
            var currentTest = _currentTestPropertyInfo.GetValue(currentContext);
            return _currentFullNamePropertyInfo.GetValue(currentTest) as string;
        }

        private void SetPropertyGetters()
        {
            var nunitAssembly = AppDomain.CurrentDomain
    .GetAssemblies().First(a => a.FullName.StartsWith("nunit.framework"));

            _testContextType = nunitAssembly.GetTypes()
                .First(t => t.AssemblyQualifiedName.StartsWith("NUnit.Framework.TestContext"));

            _currentContextPropertyInfo = _testContextType.GetProperty("CurrentContext");

            _testAdapterType = nunitAssembly.GetTypes()
                .First(t => t.AssemblyQualifiedName.StartsWith("NUnit.Framework.TestContext+TestAdapter"));

            _currentTestPropertyInfo = _testContextType.GetProperty("Test");
            _currentFullNamePropertyInfo = _testAdapterType.GetProperty("FullName");
        }
    }
}