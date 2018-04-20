using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollector.InProcDataCollector;

namespace UCoverme.DataCollector.DataCollectors
{
    public interface IDataCollector
    {
        string DataCollectorName { get; }
        TestExecutionData GetDataCollector(string projectPath);
        void TestSessionEnd(TestSessionEndArgs testSessionEndArgs);
        void TestSessionStart(TestSessionStartArgs testSessionStartArgs);
        void TestCaseStart(TestCaseStartArgs testCaseStartArgs);
        void TestCaseEnd(TestCaseEndArgs testCaseEndArgs);
    }
}