using System;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollector.InProcDataCollector;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.InProcDataCollector;
using UCoverme.DataCollector.DataCollectors;

namespace UCoverme.DataCollector
{
    [DataCollectorFriendlyName("UCovermeDataCollector")]
    [DataCollectorTypeUri("ucoverme://inprocdatacollector")]
    public class UCovermeDataCollector : InProcDataCollection
    {   
        private static readonly Lazy<IDataCollector> TestExecutionDataCollector = new Lazy<IDataCollector>(DataCollectors.DataCollectors.CreateDataCollector);
        private static readonly object LockObject = new object();

        public static TestExecutionData GetDataCollector(string coverageProjectPath)
        {
            lock (LockObject)
            {
                if (TestExecutionDataCollector.Value == null)
                {
                    throw new InvalidOperationException("Test execution datacollector is null.");
                }

                return TestExecutionDataCollector.Value.GetDataCollector(coverageProjectPath);
            }
        }

        public void Initialize(IDataCollectionSink dataSink)
        {
        }

        public void TestCaseStart(TestCaseStartArgs testCaseStartArgs)
        {
            lock (LockObject)
            {
                if (TestExecutionDataCollector.Value == null)
                {
                    throw new InvalidOperationException("Test execution datacollector is null.");
                }
                TestExecutionDataCollector.Value.TestCaseStart(testCaseStartArgs);
            }
        }

        public void TestCaseEnd(TestCaseEndArgs testCaseEndArgs)
        {
            lock (LockObject)
            {
                if (TestExecutionDataCollector.Value == null)
                {
                    throw new InvalidOperationException("Test execution datacollector is null.");
                }
                TestExecutionDataCollector.Value.TestCaseEnd(testCaseEndArgs);
            }
        }

        public void TestSessionEnd(TestSessionEndArgs testSessionEndArgs)
        {
            lock (LockObject)
            {
                if (TestExecutionDataCollector.Value == null)
                {
                    throw new InvalidOperationException("Test execution datacollector is null.");
                }
                TestExecutionDataCollector.Value.TestSessionEnd(testSessionEndArgs);
            }
        }

        public void TestSessionStart(TestSessionStartArgs testSessionStartArgs)
        {
            lock (LockObject)
            {
                TestExecutionDataCollector.Value.TestSessionStart(testSessionStartArgs);
            }
        }
    }
}